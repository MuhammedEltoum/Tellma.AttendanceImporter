using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TimeZoneConverter;
using Tellma.AttendanceImporter;

namespace Tellma.AttendanceImporter.FunctionWorker
{
    public class AttendanceImporterFunction
    {
        private readonly TellmaAttendanceImporter _importer;
        private readonly TimeZoneInfo _gulfTimeZone;

        // Constructor Injection: The Importer is injected directly, no need for IServiceProvider scope creation manually
        public AttendanceImporterFunction(TellmaAttendanceImporter importer)
        {
            _importer = importer;

            // Initialize Gulf Standard Time zone (UAE) matches Worker.cs logic
            try
            {
                _gulfTimeZone = TZConvert.GetTimeZoneInfo("Asia/Dubai");
            }
            catch
            {
                _gulfTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");
            }
        }

        // CRON: 0 */10 6-20 * * * = Every 10 minutes
        // Note: CRON triggers in Azure are UTC by default. 
        // We run it every 10 minutes continuously, but filter execution inside the code based on UAE time.
        [FunctionName("AttendanceImporter")]
        public async Task Run(
            [TimerTrigger("0 */10 6-20 * * *", RunOnStartup = false)] TimerInfo timer,
            ILogger log,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Time Zone Check (Ported from Worker.cs)
                var gulfTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _gulfTimeZone);
                var currentTime = gulfTime.TimeOfDay;

                var startHour = TimeSpan.FromHours(6);
                var endHour = TimeSpan.FromHours(20);

                // 2. Execution Gate
                if (currentTime >= startHour && currentTime <= endHour)
                {
                    log.LogInformation($"[Working Hours] Starting import. Gulf Time: {gulfTime:HH:mm:ss}");

                    // 3. execution
                    // We use the function's cancellationToken directly
                    await _importer.ImportToTellma(cancellationToken);

                    log.LogInformation("Import completed successfully");
                }
                else
                {
                    log.LogInformation($"[Off Hours] Current Gulf Time: {gulfTime:HH:mm:ss}. Skipping execution.");
                }
            }
            catch (OperationCanceledException)
            {
                log.LogWarning("Import operation was cancelled.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error occurred during attendance import");
                // In Azure Functions, throwing here will mark the execution as 'Failed' in the dashboard
                throw;
            }
        }
    }
}