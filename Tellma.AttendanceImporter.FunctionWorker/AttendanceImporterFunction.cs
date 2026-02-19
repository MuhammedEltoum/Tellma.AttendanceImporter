using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using Tellma.AttendanceImporter;

namespace Tellma.AttendanceImporter.FunctionWorker;

public class AttendanceImporterFunction
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AttendanceImporterFunction> _logger;

    public AttendanceImporterFunction(
        IServiceProvider serviceProvider, 
        ILogger<AttendanceImporterFunction> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [Function("AttendanceImporter")]
    public async Task Run(
        [TimerTrigger("*/10 * * * *")] TimerInfo myTimer, 
        FunctionContext context)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {time}", DateTime.Now);
        
        using var scope = _serviceProvider.CreateScope();

        try
        {
            var importer = scope.ServiceProvider.GetRequiredService<TellmaAttendanceImporter>();
            await importer.ImportToTellma(context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled Error in AttendanceImporter");
        }

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {next}", myTimer.ScheduleStatus.Next);
        }
    }
}