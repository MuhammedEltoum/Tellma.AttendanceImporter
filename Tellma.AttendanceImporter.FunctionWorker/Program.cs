using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tellma.AttendanceImporter;
using Tellma.AttendanceImporter.Connect;
using Tellma.AttendanceImporter.FunctionWorker;
using Tellma.AttendanceImporter.Samsung;
using Tellma.AttendanceImporter.TellmaAPI;
using Tellma.AttendanceImporter.Zkem;
using Tellma.Client;
using Tellma.Utilities.EmailLogger;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Configure Options
builder.Services.Configure<TellmaOptions>(builder.Configuration.GetSection("Tellma"));
builder.Services.Configure<ConnectApiOptions>(builder.Configuration.GetSection("ConnectApi"));

// Register HttpClient
builder.Services.AddHttpClient();

// Register Services - use appropriate lifetimes
builder.Services.AddScoped<ZkemDeviceService>();
builder.Services.AddScoped<SamsungDeviceService>();
builder.Services.AddScoped<ConnectApiService>();
builder.Services.AddScoped<ITellmaApiClient, TellmaApiClient>();
builder.Services.AddScoped<IConnectApiClient, ConnectApiClient>();
builder.Services.AddScoped<IDailyEmailService, DailyEmailService>();
builder.Services.AddScoped<EmailLogger>();
builder.Services.AddScoped<TellmaAttendanceImporter>();
builder.Services.AddScoped<ITellmaService, TellmaService>();
builder.Services.AddScoped<IDeviceServiceFactory, DeviceServiceFactory>();

// Tellma Client - Singleton is correct
builder.Services.AddSingleton(provider =>
{
    var options = provider.GetRequiredService<IOptions<TellmaOptions>>();
    return new TellmaClient(
        baseUrl: "https://web.tellma.com",
        authorityUrl: "https://web.tellma.com",
        clientId: options.Value.ClientId,
        clientSecret: options.Value.ClientSecret);
});

builder.Build().Run();
