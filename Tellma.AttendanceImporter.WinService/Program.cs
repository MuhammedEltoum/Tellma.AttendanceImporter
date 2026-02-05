using Microsoft.Extensions.Options;
using Tellma.AttendanceImporter;
using Tellma.AttendanceImporter.Connect;
using Tellma.AttendanceImporter.Contract;
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
        // Configuration
        services.Configure<ImporterOptions>(hostContext.Configuration);
        services.Configure<TellmaOptions>(hostContext.Configuration.GetSection("Tellma"));
        services.Configure<ConnectApiOptions>(hostContext.Configuration.GetSection("ConnectApi"));
        services.Configure<EmailOptions>(hostContext.Configuration.GetSection("Email"));

        // HttpClient registrations
        services.AddHttpClient<IConnectApiClient, ConnectApiClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ConnectApiOptions>>();
            client.BaseAddress = new Uri(options.Value.BaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

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

        // Service registrations - FIXED
        services.AddScoped<ITellmaApiClient, TellmaApiClient>();
        services.AddScoped<ZkemDeviceService>();
        services.AddScoped<SamsungDeviceService>();
        services.AddScoped<ConnectApiService>();
        services.AddScoped<IDailyEmailService, DailyEmailService>();

        services.AddScoped<ITellmaApiClient, TellmaApiClient>();
        services.AddScoped<TellmaAttendanceImporter>();
        services.AddScoped<ITellmaService, TellmaService>();
        services.AddScoped<IDeviceServiceFactory, DeviceServiceFactory>();

        // Email services
        services.AddScoped<EmailLogger>();  // Changed from AddSingleton to AddScoped if used in scoped services

        services.AddHostedService<Worker>();
    })
    .ConfigureLogging((hostContext, loggingBuilder) =>
    {
        loggingBuilder.AddDebug();
        loggingBuilder.AddEmail(hostContext.Configuration);
    })
    .Build();

host.Run();