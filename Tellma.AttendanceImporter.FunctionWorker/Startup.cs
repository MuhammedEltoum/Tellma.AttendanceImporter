using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Tellma.AttendanceImporter;
using Tellma.AttendanceImporter.Connect;
using Tellma.AttendanceImporter.Contract;
using Tellma.AttendanceImporter.Samsung;
using Tellma.AttendanceImporter.TellmaAPI;
using Tellma.AttendanceImporter.Zkem;
using Tellma.Client;
using Tellma.Utilities.EmailLogger;

[assembly: FunctionsStartup(typeof(Tellma.AttendanceImporter.FunctionWorker.Startup))]

namespace Tellma.AttendanceImporter.FunctionWorker
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var context = builder.GetContext();
            var config = context.Configuration;
            var services = builder.Services;

            // 1. Configuration Sections
            // In Azure, nested settings like "Tellma:ClientId" map automatically to IOptions
            services.Configure<TellmaOptions>(config.GetSection("Tellma"));
            services.Configure<ConnectApiOptions>(config.GetSection("ConnectApi"));
            services.Configure<EmailOptions>(config.GetSection("Email"));

            // 2. HttpClient Registrations
            services.AddHttpClient<IConnectApiClient, ConnectApiClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<ConnectApiOptions>>();
                client.BaseAddress = new Uri(options.Value.BaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            // 3. Tellma Client (Singleton as per original Program.cs)
            services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<TellmaOptions>>();
                return new TellmaClient(
                    baseUrl: "https://web.tellma.com",
                    authorityUrl: "https://web.tellma.com",
                    clientId: options.Value.ClientId,
                    clientSecret: options.Value.ClientSecret);
            });

            // 4. Service Registrations
            // Note: In Azure Functions, "Scoped" services live for the duration of a single function execution.
            services.AddScoped<ITellmaApiClient, TellmaApiClient>();

            // Device Services
            services.AddScoped<ZkemDeviceService>();
            services.AddScoped<SamsungDeviceService>();
            services.AddScoped<ConnectApiService>();

            // Utilities
            services.AddScoped<IDailyEmailService, DailyEmailService>();
            services.AddScoped<EmailLogger>();

            // Core Importer Logic
            services.AddScoped<ITellmaService, TellmaService>();
            services.AddScoped<IDeviceServiceFactory, Tellma.AttendanceImporter.FunctionWorker.DeviceServiceFactory>();
            services.AddScoped<TellmaAttendanceImporter>();

            // Logging
            // Azure Functions provides its own ILogger, but if you need the specific EmailLogger integration:
            // You might need to manually attach your EmailLogger provider if it's a custom ILoggerProvider.
            // For now, we are registering the service as Scoped above.
        }
    }
}