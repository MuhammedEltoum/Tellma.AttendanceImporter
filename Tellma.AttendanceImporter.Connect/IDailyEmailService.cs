namespace Tellma.AttendanceImporter.Connect
{
    // Extracted Email Service
    public interface IDailyEmailService
    {
        Task CheckAndSendDailyReportAsync(List<ConnectEmployee> employees, CancellationToken token);
    }
}