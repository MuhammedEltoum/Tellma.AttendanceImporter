using Microsoft.Extensions.Logging;
using Tellma.AttendanceImporter.Contract;
using Tellma.Utilities.EmailLogger;

namespace Tellma.AttendanceImporter.Connect
{
    public class ConnectApiService : IConnectApiService
    {
        private readonly IConnectApiClient _connectApiClient;
        private readonly ITellmaApiClient _tellmaApiClient;
        private readonly ILogger<ConnectApiService> _logger;
        private readonly IDailyEmailService _dailyEmailService;
        private readonly DateTime _earliestAttendanceDate = new(2026, 01, 02);
        private readonly object _lock = new();

        public string DeviceType => "Connect";

        public ConnectApiService(
            IConnectApiClient connectApiClient,
            ITellmaApiClient tellmaApiClient,
            IDailyEmailService dailyEmailService,
            ILogger<ConnectApiService> logger)
        {
            _connectApiClient = connectApiClient ?? throw new ArgumentNullException(nameof(connectApiClient));
            _tellmaApiClient = tellmaApiClient ?? throw new ArgumentNullException(nameof(tellmaApiClient));
            _dailyEmailService = dailyEmailService ?? throw new ArgumentNullException(nameof(dailyEmailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<AttendanceRecord>> LoadFromDevice(DeviceInfo info, CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(info);
            ArgumentException.ThrowIfNullOrWhiteSpace(info.Name);

            try
            {
                var connectEmployees = await _tellmaApiClient.GetConnectEmployees(info.Name, token);

                // Check and send daily email if needed
                var invalidEmployees = connectEmployees
                    .Where(emp => String.IsNullOrWhiteSpace(emp.BitrixId))
                    .ToList();

                await _dailyEmailService.CheckAndSendDailyReportAsync(invalidEmployees, token);

                // Halt import if an employee exist with no bitrixId.
                if (invalidEmployees.Count > 0)
                {
                    _logger.LogWarning("An employee has no BitrixId. Service will halt importing until amending the employee.");
                    return Enumerable.Empty<AttendanceRecord>();
                }

                var validEmployees = connectEmployees
                    .Where(e => !string.IsNullOrWhiteSpace(e.BitrixId))
                    .ToList();

                _logger.LogDebug("Retrieved {Count} Connect employees", validEmployees.Count);

                var location = info.Name.Replace(" device", "", StringComparison.OrdinalIgnoreCase);
                var connectAttendanceRecords = await _connectApiClient.GetAttendanceRecords(
                    location, info.LastSyncTime, token);

                if (connectAttendanceRecords.Count == 0)
                {
                    _logger.LogWarning(
                        "No attendance records retrieved from Connect API for device {DeviceName}",
                        info.Name);
                    return Enumerable.Empty<AttendanceRecord>();
                }

                _logger.LogDebug(
                    "Retrieved {Count} attendance records from Connect API",
                    connectAttendanceRecords.Count);

                var filteredRecords = FilterAttendanceRecords(connectAttendanceRecords, validEmployees);
                return MapToAttendanceRecords(filteredRecords, info);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex,
                    "Error loading attendance records from Connect device {DeviceName}",
                    info.Name);
                throw;
            }
        }

        private List<ConnectAttendanceRecord> FilterAttendanceRecords(
            List<ConnectAttendanceRecord> attendanceRecords,
            List<ConnectEmployee> connectEmployees)
        {
            var employeeLookup = connectEmployees.ToDictionary(e => e.BitrixId!);

            var invalidUserIds = attendanceRecords
                .Where(ar => !employeeLookup.ContainsKey(ar.UserId))
                .Select(ar => ar.UserId)
                .Distinct()
                .ToList();

            return attendanceRecords
                .Where(ar => employeeLookup.TryGetValue(ar.UserId, out var employee) &&
                           ar.Time.Date >= employee.JoiningDate &&
                           ar.Time.Date >= _earliestAttendanceDate.Date)
                .ToList();
        }

        private static IEnumerable<AttendanceRecord> MapToAttendanceRecords(
            List<ConnectAttendanceRecord> connectRecords,
            DeviceInfo deviceInfo)
        {
            return connectRecords.Select(car => new AttendanceRecord(deviceInfo)
            {
                UserId = car.UserId,
                Time = car.Time,
                IsIn = car.IsIn
            });
        }
    }
}