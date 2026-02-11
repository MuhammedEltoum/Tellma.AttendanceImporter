using Microsoft.Extensions.Options;
using Tellma.AttendanceImporter;
using Tellma.AttendanceImporter.Connect;
using Tellma.AttendanceImporter.Samsung;
using Tellma.AttendanceImporter.TellmaAPI;
using Tellma.AttendanceImporter.WinService;
using Tellma.AttendanceImporter.Zkem;
using Tellma.Client;
using Tellma.Utilities.EmailLogger;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(config =>
    {
        config.ServiceName = "Tellma Attendance Importer";
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Worker>(); // automatically defines Ilogger
        services.Configure<ImporterOptions>(hostContext.Configuration);
        services.Configure<TellmaOptions>(hostContext.Configuration.GetSection("Tellma"));
        services.Configure<ConnectApiOptions>(hostContext.Configuration.GetSection("ConnectApi"));
        services.AddScoped<ZkemDeviceService>();
        services.AddScoped<SamsungDeviceService>();
        
        services.AddScoped<ConnectApiService>();
        services.AddScoped<ITellmaApiClient, TellmaApiClient>();
        services.AddScoped<IConnectApiClient, ConnectApiClient>();
        services.AddScoped<IDailyEmailService, DailyEmailService>();
        services.AddScoped<EmailLogger>();
        // Register ConnectApiService as a typed HttpClient
        services.AddHttpClient();
        services.AddScoped<TellmaAttendanceImporter>(); // with every new scope, a new instance
        services.AddScoped<ITellmaService, TellmaService>();
        // Tellma Client - single instance for the entire application
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<TellmaOptions>>();
            return new TellmaClient(
                baseUrl: "https://web.tellma.com",
                authorityUrl: "https://web.tellma.com",
                clientId: options.Value.ClientId,
                clientSecret: options.Value.ClientSecret);
        });
        services.AddScoped<IDeviceServiceFactory, Tellma.AttendanceImporter.WinService.DeviceServiceFactory>();//DI to return the proper factory
    })
    .ConfigureLogging((hostContext, loggingBuilder) =>
    {
        loggingBuilder.AddDebug();
        loggingBuilder.AddEmail(hostContext.Configuration);
    })
    .Build();

host.Run();