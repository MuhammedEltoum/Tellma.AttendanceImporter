namespace Tellma.AttendanceImporter.Connect
{
    public interface IConnectApiClient
    {
        public Task<List<ConnectAttendanceRecord>> GetAttendanceRecords(string location, DateTime? lastSyncTime, CancellationToken token);
        List<string> GetDailyReportEmails();
    }
}