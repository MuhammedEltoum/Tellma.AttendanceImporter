using Tellma.AttendanceImporter.Contract;

namespace Tellma.AttendanceImporter.Tests
{
    public class MockDeviceService : IDeviceService
    {
        public string DeviceType => "Mock";

        public Task<IEnumerable<AttendanceRecord>> LoadFromDevice(DeviceInfo info, CancellationToken token)
        {
            var result = new List<AttendanceRecord>
            {
                new AttendanceRecord(info)
                {
                    Time = new DateTime(2023,6,19,8,56,0),
                    UserId = "19", // Asmaa
                    IsIn = null
                },
                new AttendanceRecord(info)
                {
                    Time = new DateTime(2023,6,19,8,58,0),
                    UserId = "21", // Sara
                    IsIn = null
                }
            };

            return Task.FromResult<IEnumerable<AttendanceRecord>>(result);
        }
    }
}