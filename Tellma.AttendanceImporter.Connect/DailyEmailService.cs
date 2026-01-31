using Microsoft.Extensions.Logging;
using Tellma.Utilities.EmailLogger;

namespace Tellma.AttendanceImporter.Connect
{
    public class DailyEmailService : IDailyEmailService
    {
        private readonly IConnectApiClient _connectApiClient;
        private readonly EmailLogger _emailLogger;
        private readonly ILogger<DailyEmailService> _logger;
        private DateTime? _lastEmailSentDate;
        private readonly object _lock = new();

        public DailyEmailService(
            IConnectApiClient connectApiClient,
            EmailLogger emailLogger,
            ILogger<DailyEmailService> logger)
        {
            _connectApiClient = connectApiClient;
            _emailLogger = emailLogger;
            _logger = logger;
        }

        public async Task CheckAndSendDailyReportAsync(List<ConnectEmployee> invalidEmployees, CancellationToken token)
        {
            var now = DateTime.UtcNow;

            // Only check between 2:00-2:05 PM UTC (6:00-6:05 PM Dubai time)
            if (now.Hour != 14 || now.Minute > 5)
                return;

            lock (_lock)
            {
                // Check if we already sent email today
                if (_lastEmailSentDate?.Date.ToString("yyyy-MM-dd") == now.Date.ToString("yyyy-MM-dd"))
                    return;

                _lastEmailSentDate = now;
            }

            try
            {
                if (invalidEmployees.Count == 0)
                    return;

                    var emailRecipients = _connectApiClient.GetDailyReportEmails();
                    if (emailRecipients.Count > 0)
                    {
                        var employeeDetails = invalidEmployees
                        .Select(emp => $"{emp.Code}: {emp.Name}")
                        .Distinct();

                        _emailLogger.SendInvalidUsers(employeeDetails, emailRecipients);
                        _logger.LogInformation("Daily email sent to {Count} recipients", emailRecipients.Count);
                    }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send daily email");
                // Don't rethrow - this shouldn't break the main attendance flow
            }
        }
    }
}