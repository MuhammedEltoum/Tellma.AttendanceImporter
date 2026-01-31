using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections;

namespace Tellma.Utilities.EmailLogger
{
    public class EmailLogger : ILogger
    {
        private readonly EmailOptions _options;
        private readonly IEnumerable<string> _emails;

        public EmailLogger(IOptions<EmailOptions> options)
        {
            _options = options.Value;
            _emails = (_options.EmailAddresses ?? "").Split(",").Select(s => s.Trim()).ToList();
        }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // TODO: check that email addresses are valid
            return (logLevel == LogLevel.Error || logLevel == LogLevel.Critical)
                && _emails.Any();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (exception == null) return;
            if (!IsEnabled(logLevel)) return;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("System Alert", "donotreply@tellma.com"));

                foreach (var email in _emails)
                    message.To.Add(new MailboxAddress(email, email));

                message.Subject = $"{_options.InstallationIdentifier ?? "Unknown"} - {logLevel}: {Truncate(exception.Message, 80, true)}";

                // Create HTML body with modern design
                var bodyBuilder = new BodyBuilder();

                bodyBuilder.HtmlBody = BuildExceptionHtmlEmailBody(
                    exception,
                    formatter(state, exception),
                    logLevel,
                    _options.InstallationIdentifier,
                    eventId
                );

                // Fallback plain text version
                bodyBuilder.TextBody = BuildExceptionPlainTextEmailBody(
                    exception,
                    formatter(state, exception),
                    logLevel,
                    _options.InstallationIdentifier,
                    eventId
                );

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.Timeout = 30000; // 30 seconds

                client.Connect(_options.SmtpHost, _options.SmtpPort ?? 587, _options.SmtpUseSsl);

                // Note: only needed if the SMTP server requires authentication
                if (!string.IsNullOrWhiteSpace(_options.SmtpUsername))
                    client.Authenticate(_options.SmtpUsername, _options.SmtpPassword);


                Console.WriteLine($"Exception email sent to {_emails.Count()} recipients.");
            }
            catch (Exception ex)
            {
                // Log the exception properly
                Console.Error.WriteLine($"Failed to send exception email: {ex.Message}");
                // Consider rethrowing based on your needs
            }
        }

        private string BuildExceptionHtmlEmailBody(Exception exception, string formattedMessage, LogLevel logLevel, string installationIdentifier, EventId eventId)
        {
            var identifier = installationIdentifier ?? "Unknown";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logLevelColor = GetLogLevelColor(logLevel);
            var logLevelText = GetLogLevelText(logLevel);

            // Extract inner exception details if available
            var innerExceptionHtml = exception.InnerException != null ?
                BuildInnerExceptionHtml(exception.InnerException) :
                "<p>No inner exception.</p>";

            // Format stack trace with line breaks
            var stackTraceHtml = FormatStackTrace(exception.StackTrace);

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>System Exception Alert</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 900px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f5f7fa;
        }}
        .container {{
            background: white;
            border-radius: 10px;
            box-shadow: 0 2px 15px rgba(0,0,0,0.1);
            padding: 30px;
            margin: 20px 0;
        }}
        .header {{
            border-bottom: 3px solid {logLevelColor};
            padding-bottom: 20px;
            margin-bottom: 25px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            flex-wrap: wrap;
        }}
        .alert-title {{
            display: flex;
            align-items: center;
            gap: 12px;
        }}
        .alert-icon {{
            font-size: 28px;
        }}
        h1 {{
            color: {logLevelColor};
            margin: 0;
            font-size: 24px;
            font-weight: 700;
        }}
        .log-level-badge {{
            background: {logLevelColor};
            color: white;
            padding: 6px 16px;
            border-radius: 20px;
            font-size: 14px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }}
        .summary-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
            margin: 25px 0;
        }}
        .summary-card {{
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 8px;
            padding: 15px;
        }}
        .summary-label {{
            font-size: 12px;
            color: #64748b;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 5px;
        }}
        .summary-value {{
            font-size: 16px;
            font-weight: 600;
            color: #1e293b;
            word-break: break-all;
        }}
        .exception-section {{
            margin: 30px 0;
        }}
        .section-title {{
            font-size: 18px;
            font-weight: 600;
            color: #1e293b;
            margin-bottom: 15px;
            padding-bottom: 8px;
            border-bottom: 2px solid #e2e8f0;
            display: flex;
            align-items: center;
            gap: 8px;
        }}
        .section-icon {{
            font-size: 20px;
        }}
        .message-box {{
            background: #f1f5f9;
            border-left: 4px solid {logLevelColor};
            padding: 20px;
            border-radius: 6px;
            margin: 15px 0;
            font-family: 'SF Mono', Monaco, 'Cascadia Code', Consolas, monospace;
            white-space: pre-wrap;
            word-break: break-word;
            font-size: 14px;
            line-height: 1.5;
        }}
        .stack-trace-box {{
            background: #1e293b;
            color: #e2e8f0;
            padding: 20px;
            border-radius: 8px;
            margin: 15px 0;
            font-family: 'SF Mono', Monaco, 'Cascadia Code', Consolas, monospace;
            font-size: 13px;
            line-height: 1.6;
            overflow-x: auto;
            max-height: 400px;
            overflow-y: auto;
        }}
        .stack-trace-line {{
            margin-bottom: 2px;
        }}
        .stack-trace-line:hover {{
            background: #334155;
            border-radius: 3px;
        }}
        .inner-exception {{
            background: #fff7ed;
            border: 1px solid #fed7aa;
            border-radius: 8px;
            padding: 20px;
            margin: 20px 0;
        }}
        .footer {{
            margin-top: 40px;
            padding-top: 20px;
            border-top: 1px solid #e2e8f0;
            font-size: 12px;
            color: #64748b;
            text-align: center;
        }}
        .action-buttons {{
            display: flex;
            gap: 10px;
            margin-top: 25px;
            flex-wrap: wrap;
        }}
        .action-button {{
            padding: 8px 16px;
            background: {logLevelColor};
            color: white;
            text-decoration: none;
            border-radius: 6px;
            font-size: 14px;
            font-weight: 500;
            transition: all 0.2s;
        }}
        .action-button:hover {{
            opacity: 0.9;
            transform: translateY(-1px);
        }}
        @media (max-width: 768px) {{
            .container {{
                padding: 20px;
            }}
            .header {{
                flex-direction: column;
                align-items: flex-start;
                gap: 15px;
            }}
            .summary-grid {{
                grid-template-columns: 1fr;
            }}
            .action-buttons {{
                flex-direction: column;
            }}
        }}
        /* Syntax highlighting for stack trace */
        .at {{
            color: #94a3b8;
        }}
        .method {{
            color: #7dd3fc;
        }}
        .file {{
            color: #fbbf24;
        }}
        .line {{
            color: #34d399;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='alert-title'>
                <span class='alert-icon'>🚨</span>
                <h1>System Exception Alert</h1>
            </div>
            <div class='log-level-badge'>{logLevelText}</div>
        </div>
        
        <div class='summary-grid'>
            <div class='summary-card'>
                <div class='summary-label'>Installation</div>
                <div class='summary-value'>{HtmlEncode(identifier)}</div>
            </div>
            <div class='summary-card'>
                <div class='summary-label'>Exception Type</div>
                <div class='summary-value'>{HtmlEncode(exception.GetType().Name)}</div>
            </div>
            <div class='summary-card'>
                <div class='summary-label'>Event ID</div>
                <div class='summary-value'>{eventId.Id}</div>
            </div>
            <div class='summary-card'>
                <div class='summary-label'>Time</div>
                <div class='summary-value'>{timestamp}</div>
            </div>
        </div>
        
        <div class='exception-section'>
            <div class='section-title'>
                <span class='section-icon'>💬</span>
                Exception Message
            </div>
            <div class='message-box'>{HtmlEncode(exception.Message)}</div>
        </div>
        
        <div class='exception-section'>
            <div class='section-title'>
                <span class='section-icon'>📋</span>
                Formatted Message
            </div>
            <div class='message-box'>{HtmlEncode(formattedMessage)}</div>
        </div>
        
        <div class='exception-section'>
            <div class='section-title'>
                <span class='section-icon'>🔍</span>
                Stack Trace
            </div>
            <div class='stack-trace-box'>{stackTraceHtml}</div>
        </div>
        
        <div class='exception-section'>
            <div class='section-title'>
                <span class='section-icon'>↪️</span>
                Inner Exception
            </div>
            <div class='inner-exception'>
                {innerExceptionHtml}
            </div>
        </div>
        
        <div class='action-buttons'>
            <a href='#' class='action-button'>View Full Logs</a>
            <a href='#' class='action-button'>Create Issue</a>
            <a href='#' class='action-button'>Acknowledge Alert</a>
        </div>
        
        <div class='footer'>
            <p>This is an automated alert from the System Monitoring Service.</p>
            <p>Please investigate this exception promptly to prevent system disruption.</p>
            <p>Alert generated at: {timestamp}</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildExceptionPlainTextEmailBody(Exception exception, string formattedMessage, LogLevel logLevel, string installationIdentifier, EventId eventId)
        {
            var identifier = installationIdentifier ?? "Unknown";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logLevelText = GetLogLevelText(logLevel);

            var innerExceptionText = exception.InnerException != null ?
                $"\nInner Exception:\n{exception.InnerException.GetType().Name}: {exception.InnerException.Message}\n{exception.InnerException.StackTrace}" :
                "\nNo inner exception.";

            return $@"🚨 SYSTEM EXCEPTION ALERT
{new string('=', 50)}

Log Level: {logLevelText}
Installation: {identifier}
Exception Type: {exception.GetType().Name}
Event ID: {eventId.Id}
Time: {timestamp}

EXCEPTION MESSAGE:
{exception.Message}

FORMATTED MESSAGE:
{formattedMessage}

STACK TRACE:
{exception.StackTrace}
{innerExceptionText}

ADDITIONAL DATA:
{string.Join("\n", exception.Data.Cast<DictionaryEntry>().Select(e => $"  {e.Key}: {e.Value}"))}

This is an automated alert from the System Monitoring Service.
Please investigate this exception promptly.

Generated at: {timestamp}";
        }

        // Helper methods for exception formatting
        private string GetLogLevelColor(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Critical => "#dc2626",    // Red-600
                LogLevel.Error => "#ea580c",       // Orange-600
                LogLevel.Warning => "#d97706",     // Amber-600
                LogLevel.Information => "#059669", // Emerald-600
                LogLevel.Debug => "#2563eb",       // Blue-600
                LogLevel.Trace => "#7c3aed",       // Violet-600
                _ => "#6b7280"                     // Gray-500
            };
        }

        private string GetLogLevelText(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Critical => "Critical",
                LogLevel.Error => "Error",
                LogLevel.Warning => "Warning",
                LogLevel.Information => "Info",
                LogLevel.Debug => "Debug",
                LogLevel.Trace => "Trace",
                _ => "Unknown"
            };
        }

        private string BuildInnerExceptionHtml(Exception innerException)
        {
            var innerStackTraceHtml = FormatStackTrace(innerException.StackTrace);

            return $@"
<div style='margin-bottom: 15px;'>
    <div style='font-weight: 600; color: #ea580c; margin-bottom: 5px;'>
        {HtmlEncode(innerException.GetType().Name)}
    </div>
    <div style='color: #475569; margin-bottom: 10px;'>
        {HtmlEncode(innerException.Message)}
    </div>
    <div style='background: #1e293b; color: #e2e8f0; padding: 15px; border-radius: 6px; font-family: monospace; font-size: 12px;'>
        {innerStackTraceHtml}
    </div>
</div>";
        }

        private string FormatStackTrace(string? stackTrace)
        {
            if (string.IsNullOrWhiteSpace(stackTrace))
                return "<div style='color: #94a3b8;'>No stack trace available.</div>";

            // Simple formatting for stack trace lines
            var lines = stackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var formattedLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                // Simple syntax highlighting
                var formattedLine = trimmedLine
                    .Replace(" at ", "<span class='at'> at </span>")
                    .Replace(" in ", "<span class='at'> in </span>")
                    .Replace(" line ", "<span class='at'> line </span>");

                formattedLines.Add($"<div class='stack-trace-line'>{formattedLine}</div>");
            }

            return string.Join("", formattedLines);
        }
        /// <summary>
        /// Removes all characters after a certain length.
        /// </summary>
        public static string Truncate(string value, int maxLength, bool appendEllipses = false)
        {
            const string ellipses = "...";

            if (maxLength < 0)
            {
                return value;
            }
            else if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            else if (value.Length <= maxLength)
            {
                return value;
            }
            else
            {
                var truncated = value.Substring(0, maxLength);
                if (appendEllipses)
                {
                    truncated += ellipses;
                }

                return truncated;
            }
        }

        public void SendInvalidUsers(IEnumerable<string> employees, IEnumerable<string> emails)
        {
            if (employees == null || !employees.Any())
                return;

            try
            {
                var invalidUsersList = employees.ToList();
                var message = new MimeMessage();

                message.From.Add(new MailboxAddress("System Alert", "donotreply@tellma.com"));

                foreach (var email in _emails)
                    message.To.Add(new MailboxAddress(email, email));

                message.Subject = $"{_options.InstallationIdentifier ?? "Unknown"} - Invalid Users Detected ({invalidUsersList.Count})";

                // Create HTML body with modern design
                var bodyBuilder = new BodyBuilder();

                bodyBuilder.HtmlBody = BuildHtmlEmailBody(invalidUsersList, _options.InstallationIdentifier);

                // Fallback plain text version
                bodyBuilder.TextBody = BuildPlainTextEmailBody(invalidUsersList, _options.InstallationIdentifier);

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.Timeout = 30000; // 30 seconds

                client.Connect(_options.SmtpHost, _options.SmtpPort ?? 587, _options.SmtpUseSsl);

                // Note: only needed if the SMTP server requires authentication
                if (!string.IsNullOrWhiteSpace(_options.SmtpUsername))
                    client.Authenticate(_options.SmtpUsername, _options.SmtpPassword);

                client.Send(message);
                client.Disconnect(true);

                // Optional: Log successful sending
                Console.WriteLine($"Invalid users email sent to {_emails.Count()} recipients.");
            }
            catch (Exception ex)
            {
                // Log the exception properly - don't swallow it
                Console.Error.WriteLine($"Failed to send invalid users email: {ex.Message}");

                // Depending on your logging framework:
                // _logger.LogError(ex, "Failed to send invalid users email");

                // Consider rethrowing or handling based on your application's needs
                // throw new InvalidOperationException("Failed to send invalid users email", ex);
            }
        }

        private string BuildHtmlEmailBody(List<string> invalidUserIds, string installationIdentifier)
        {
            var identifier = installationIdentifier ?? "Unknown";
            var count = invalidUserIds.Count;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Invalid Users Alert</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f5f7fa;
        }}
        .container {{
            background: white;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            padding: 30px;
            margin: 20px 0;
        }}
        .header {{
            border-bottom: 2px solid #e74c3c;
            padding-bottom: 20px;
            margin-bottom: 25px;
        }}
        .alert-icon {{
            color: #e74c3c;
            font-size: 24px;
            margin-right: 10px;
            vertical-align: middle;
        }}
        h1 {{
            color: #e74c3c;
            display: inline-block;
            vertical-align: middle;
            margin: 0;
            font-size: 24px;
        }}
        .summary-card {{
            background: #fff8f8;
            border-left: 4px solid #e74c3c;
            padding: 15px;
            border-radius: 4px;
            margin: 20px 0;
        }}
        .summary-item {{
            display: flex;
            justify-content: space-between;
            margin: 8px 0;
        }}
        .summary-label {{
            font-weight: 600;
            color: #555;
        }}
        .summary-value {{
            font-weight: 700;
            color: #e74c3c;
        }}
        .users-list {{
            background: #f8f9fa;
            border-radius: 6px;
            padding: 20px;
            margin: 25px 0;
            max-height: 300px;
            overflow-y: auto;
        }}
        .user-item {{
            padding: 10px;
            border-bottom: 1px solid #e9ecef;
            font-family: 'Courier New', monospace;
            font-size: 14px;
        }}
        .user-item:last-child {{
            border-bottom: none;
        }}
        .badge {{
            display: inline-block;
            padding: 4px 12px;
            background: #e74c3c;
            color: white;
            border-radius: 20px;
            font-size: 12px;
            font-weight: 600;
            margin-left: 10px;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #eee;
            font-size: 12px;
            color: #666;
            text-align: center;
        }}
        @media (max-width: 600px) {{
            .container {{
                padding: 15px;
            }}
            h1 {{
                font-size: 20px;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <span class='alert-icon'>⚠️</span>
            <h1>Invalid Users Alert</h1>
            <span class='badge'>{count} Found</span>
        </div>
        
        <div class='summary-card'>
            <div class='summary-item'>
                <span class='summary-label'>Installation:</span>
                <span class='summary-value'>{identifier}</span>
            </div>
            <div class='summary-item'>
                <span class='summary-label'>Invalid Users:</span>
                <span class='summary-value'>{count}</span>
            </div>
            <div class='summary-item'>
                <span class='summary-label'>Detection Time:</span>
                <span class='summary-value'>{timestamp}</span>
            </div>
        </div>
        
        <h3>Employees without User IDs:</h3>
        <div class='users-list'>
            {string.Join("", invalidUserIds.Select(userId =>
                        $"<div class='user-item'>{HtmlEncode(userId)}</div>"))}
        </div>
        
        <div class='footer'>
            <p>This is an automated alert from the User Validation System.</p>
            <p>Please review these user IDs and take appropriate action.</p>
            <p>Generated at {timestamp}</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildPlainTextEmailBody(List<string> invalidUserIds, string installationIdentifier)
        {
            var identifier = installationIdentifier ?? "Unknown";
            var count = invalidUserIds.Count;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            return $@"INVALID USERS ALERT
====================

Installation: {identifier}
Invalid Users Found: {count}
Detection Time: {timestamp}

INVALID USER IDs:
{string.Join("\n", invalidUserIds.Select(userId => $"  • {userId}"))}

This is an automated alert from the User Validation System.
Please review these user IDs and take appropriate action.

Generated at: {timestamp}";
        }

        private string HtmlEncode(string text)
        {
            return System.Net.WebUtility.HtmlEncode(text);
        }
    }
}
