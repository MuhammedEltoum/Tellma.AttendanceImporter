namespace Tellma.AttendanceImporter.Connect
{
    public interface ITellmaApiClient
    {
        public Task<List<ConnectEmployee>> GetConnectEmployees(string deviceName, CancellationToken token);
    }
}