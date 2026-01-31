using Tellma.AttendanceImporter.Contract;

namespace Tellma.AttendanceImporter.Tests
{
    public class MockDeviceServiceFactory : IDeviceServiceFactory
    {
        public IDeviceService Create(string deviceType)
        {
            return new MockDeviceService(); // not using device type
        }
    }
}