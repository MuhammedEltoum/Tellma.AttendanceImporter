using Tellma.AttendanceImporter.Contract;
using Tellma.AttendanceImporter.TellmaAPI;

namespace Tellma.AttendanceImporter.Tests
{
    internal class MockTellmaSerivce : ITellmaService
    {
        public Task<IEnumerable<DeviceInfo>> GetDeviceInfos(int tenantId, CancellationToken token)
        {
            return Task.FromResult<IEnumerable<DeviceInfo>>(new List<DeviceInfo>
            {
                new DeviceInfo("MockType") // Provide required 'deviceType' argument
                {
                    Id = 1,
                    Name = "Mock Device A",
                    DutyStationId = 101,
                    IpAddress = "10.10.10.10",
                    LastSyncTime = DateTime.UtcNow,
                    Port = 8080,
                }
            });
        }

        public Task Import(int tenantId, IEnumerable<AttendanceRecord> records, CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}