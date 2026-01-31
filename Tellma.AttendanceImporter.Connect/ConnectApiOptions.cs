namespace Tellma.AttendanceImporter.Connect
{
    // Configuration classes
    public class ConnectApiOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://attend.axc.ae/";
        public string DailyReportEmails { get; set; } = string.Empty;
    }
}