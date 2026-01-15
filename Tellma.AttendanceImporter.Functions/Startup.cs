using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Tellma.AttendanceImporter;
using Tellma.AttendanceImporter.Connect;
using Tellma.AttendanceImporter.Contract;

[assembly: FunctionsStartup(typeof(Tellma.AttendanceImporter.Functions.Startup))]

namespace Tellma.AttendanceImporter.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Register services
            builder.Services.AddHttpClient();
            
            // Register the Connect API service
            builder.Services.AddScoped<ConnectApiService>();
            
            // Register other required services
            builder.Services.AddScoped<IDeviceServiceFactory, DeviceServiceFactory>();
            builder.Services.AddScoped<TellmaAttendanceImporter>();
            
            // Configure options from configuration
            builder.Services.AddOptions<TellmaOptions>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("Tellma").Bind(settings);
                });
        }
    }
}
