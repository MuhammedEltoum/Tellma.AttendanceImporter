using Microsoft.Extensions.DependencyInjection;
using Tellma.AttendanceImporter.Contract;

namespace Tellma.AttendanceImporter.Connect.Tests
{
    public class ConnectTest
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ConnectTest()
        {
            var services = new ServiceCollection();

            services.AddHttpClient(); // registers IHttpClientFactory

            var provider = services.BuildServiceProvider();
            _httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        }
        // Add another test
        [Fact(DisplayName = "Test Connect API Service")]
        public async Task TestConnectApiService()
        {
            // Arrange
            var connectApiService = new ConnectApiService(_httpClientFactory);
            var deviceInfo = new DeviceInfo("Connect")
            {
                // put real data below
                Id = 1,
                IpAddress = "Mild Tower",// pass location intead
                //Port = 4370,
                //Name = "Test Connect API",
                //DutyStationId = 8,
                LastSyncTime = new DateTime(2025, 12, 14)
            };
            // Act
            var records = await connectApiService.LoadFromDevice(deviceInfo, CancellationToken.None);

            // Assert
            Assert.NotEmpty(records);
        }

    }
}