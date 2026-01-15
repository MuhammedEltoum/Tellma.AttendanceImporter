using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Tellma.AttendanceImporter;

namespace Tellma.AttendanceImporter.Functions
{
    public class ConnectApiFunction
    {
        private readonly TellmaAttendanceImporter _importer;
        private readonly ILogger<ConnectApiFunction> _logger;

        public ConnectApiFunction(TellmaAttendanceImporter importer, ILogger<ConnectApiFunction> logger)
        {
            _importer = importer ?? throw new ArgumentNullException(nameof(importer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [FunctionName("ProcessConnectApiData")]
        public async Task Run(
            [TimerTrigger("0 */5 * * * *")] TimerInfo myTimer) // Runs every 5 minutes
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            if (myTimer.IsPastDue)
            {
                _logger.LogInformation("Timer is running late. The function will still run but may be part of a backlog.");
            }

            try
            {
                _logger.LogInformation("Starting Connect API data import...");
                await _importer.ImportToTellma(System.Threading.CancellationToken.None);
                _logger.LogInformation("Connect API data import completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while importing data from Connect API");
                throw; // This will mark the function as failed in Azure
            }
        }
    }
}
