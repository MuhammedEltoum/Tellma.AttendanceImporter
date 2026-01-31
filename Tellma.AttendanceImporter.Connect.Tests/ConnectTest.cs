using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Tellma.AttendanceImporter.Contract;
using Tellma.Utilities.EmailLogger;

namespace Tellma.AttendanceImporter.Connect.Tests
{
    public class ConnectTest
    {
        private readonly Mock<IConnectApiClient> _mockConnectApiClient;
        private readonly Mock<ITellmaApiClient> _mockTellmaApiClient;
        private readonly Mock<IDailyEmailService> _mockEmailLogger;

        public ConnectTest()
        {
            _mockConnectApiClient = new Mock<IConnectApiClient>();
            _mockTellmaApiClient = new Mock<ITellmaApiClient>();
            _mockEmailLogger = new Mock<IDailyEmailService>();
        }

        [Fact(DisplayName = "Test Connect API Service")]
        public async Task TestConnectApiService()
        {
            // Arrange
            var deviceInfo = new DeviceInfo("Connect")
            {
                Id = 1,
                IpAddress = "Mild Tower",
                LastSyncTime = new DateTime(2025, 12, 14)
            };

            var employees = new List<ConnectEmployee>
            {
                new ConnectEmployee { BitrixId = "1001", JoiningDate = new DateTime(2024, 1, 1) },
                new ConnectEmployee { BitrixId = "1002", JoiningDate = new DateTime(2024, 2, 1) }
            };

            var attendanceRecords = new List<ConnectAttendanceRecord>
            {
                new ConnectAttendanceRecord { UserId = "1001", Time = new DateTime(2026, 1, 3, 8, 30, 0), IsIn = true },
                new ConnectAttendanceRecord { UserId = "1002", Time = new DateTime(2026, 1, 3, 9, 0, 0), IsIn = true }
            };

            _mockTellmaApiClient.Setup(x => x.GetConnectEmployees("Mock Device", It.IsAny<CancellationToken>()))
                .ReturnsAsync(employees);

            _mockConnectApiClient.Setup(x => x.GetAttendanceRecords(
                    It.IsAny<string>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(attendanceRecords);

            var service = new ConnectApiService(
                _mockConnectApiClient.Object,
                _mockTellmaApiClient.Object,
                _mockEmailLogger.Object,
                new NullLogger<ConnectApiService>());

            // Act
            var records = await service.LoadFromDevice(deviceInfo, CancellationToken.None);

            // Assert & Debug
            Console.WriteLine($"Records count: {records.Count()}");

            // Verify what was actually called
            _mockTellmaApiClient.Verify(x => x.GetConnectEmployees("Mock Device", It.IsAny<CancellationToken>()), Times.Once);
            _mockConnectApiClient.Verify(x => x.GetAttendanceRecords(
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()), Times.Once);

            Assert.NotEmpty(records);
            Assert.Equal(2, records.Count());
        }
    }
}