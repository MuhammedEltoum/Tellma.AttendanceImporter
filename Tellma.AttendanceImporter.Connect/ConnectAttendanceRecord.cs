namespace Tellma.AttendanceImporter.Connect
{
    public class ConnectAttendanceRecord
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public bool? IsIn { get; set; }
    }
}
