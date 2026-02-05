using Microsoft.Extensions.Options;
using System.Globalization;
using TimeZoneConverter;

namespace Tellma.AttendanceImporter.WinService
{
    public class Worker : BackgroundService
    {
        // DI container
        private readonly IServiceProvider _serviceProvider;
        private readonly ImporterOptions _options;
        private readonly TimeSpan _fixedInterval = TimeSpan.FromMinutes(10); // Fixed 10-minute interval
        private readonly TimeSpan _startHour = TimeSpan.FromHours(6); // 6 AM Gulf Time
        private readonly TimeSpan _endHour = TimeSpan.FromHours(21); // 9 PM Gulf Time (21:00 in 24-hour format)
        private readonly TimeZoneInfo _gulfTimeZone;

        public Worker(IServiceProvider serviceProvider, IOptions<ImporterOptions> options)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;

            // Initialize Gulf Standard Time zone (UAE)
            // For .NET Core/.NET 5+, TZConvert is recommended for cross-platform compatibility
            try
            {
                // Try to get the time zone using TZConvert (more reliable cross-platform)
                _gulfTimeZone = TZConvert.GetTimeZoneInfo("Asia/Dubai");
            }
            catch
            {
                // Fallback to Windows time zone ID if TZConvert fails or isn't available
                _gulfTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Get current time in Gulf Time (UAE)
                    var gulfTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _gulfTimeZone);
                    var currentTime = gulfTime.TimeOfDay;

                    // Check if current time is within working hours (6 AM to 9 PM Gulf Time)
                    if (IsWithinWorkingHours(currentTime))
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Worker>>();
                        logger.LogInformation("Worker running at: {time} (Gulf Time)", gulfTime);
                        logger.LogDebug("UTC Time: {utcTime}", DateTime.UtcNow);

                        try
                        {
                            var importer = scope.ServiceProvider.GetRequiredService<TellmaAttendanceImporter>();
                            await importer.ImportToTellma(stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Unhandled Error");
                            // Don't throw. Instead, wait for period then try again
                        }
                    }
                    else
                    {
                        // Log when we're waiting for next working hour
                        using var scope = _serviceProvider.CreateScope();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Worker>>();
                        logger.LogInformation("Outside working hours. Current Gulf Time: {gulfTime}. Waiting until 6 AM Gulf Time.",
                            gulfTime.ToString("HH:mm:ss"));

                        // If outside working hours, calculate time until next 6 AM Gulf Time
                        var delayUntilNextRun = CalculateDelayUntilNextWorkingHour(gulfTime);
                        await Task.Delay(delayUntilNextRun, stoppingToken);
                        continue; // Skip the fixed interval delay at the end
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    // Log any unexpected errors but continue the service
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Worker>>();
                        logger.LogError(ex, "Error in worker loop");
                    }
                    catch
                    {
                        // If logging fails, continue anyway
                    }

                    // Wait before retrying
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    continue;
                }

                // Wait for the fixed 10-minute interval before next execution
                await Task.Delay(_fixedInterval, stoppingToken);
            }
        }

        private bool IsWithinWorkingHours(TimeSpan currentTime)
        {
            return currentTime >= _startHour && currentTime <= _endHour;
        }

        private TimeSpan CalculateDelayUntilNextWorkingHour(DateTime gulfTime)
        {
            var currentTime = gulfTime.TimeOfDay;

            // If current time is before 6 AM Gulf Time today
            if (currentTime < _startHour)
            {
                var nextRunTime = gulfTime.Date.Add(_startHour);
                return nextRunTime - gulfTime;
            }
            // If current time is after 9 PM Gulf Time today
            else // currentTime > _endHour
            {
                // Next run is at 6 AM Gulf Time tomorrow
                var nextRunTime = gulfTime.Date.AddDays(1).Add(_startHour);
                return nextRunTime - gulfTime;
            }
        }
    }
}