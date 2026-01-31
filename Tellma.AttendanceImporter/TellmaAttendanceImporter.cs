using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tellma.AttendanceImporter.Contract;
using Tellma.AttendanceImporter.TellmaAPI;

namespace Tellma.AttendanceImporter
{
    public class TellmaAttendanceImporter
    {
        private readonly IDeviceServiceFactory _deviceServiceFactory;
        private readonly ILogger<TellmaAttendanceImporter> _logger;
        private readonly ITellmaService _tellmaService;
        private readonly IEnumerable<int> _tenantIds;

        public TellmaAttendanceImporter(IDeviceServiceFactory deviceServiceFactory, ILogger<TellmaAttendanceImporter> logger, ITellmaService tellmaService, IOptions<TellmaOptions> options)
        {
            _deviceServiceFactory = deviceServiceFactory;
            _logger = logger;
            _tellmaService = tellmaService;

            _tenantIds = (options.Value.TenantIds ?? "")
                           .Split(",")
                           .Select(s =>
                           {
                               if (int.TryParse(s, out int result))
                                   return result;
                               else if (string.IsNullOrWhiteSpace(s))
                                   throw new ArgumentException($"Error parsing TenantIds config value, the TenantIds list is empty or the service account is unable to see the secrets file..");
                               else
                                   throw new ArgumentException($"Error parsing TenantIds config value, {s} is not a valid integer.");
                           })
                           .ToList(); // materialize for performance. Errors are thrown here.
        }
        public async Task ImportToTellma(CancellationToken token)
        {
            //Stopwatch sw = Stopwatch.StartNew();

            foreach (int tenantId in _tenantIds)
            {
                IEnumerable<DeviceInfo> deviceInfos;
                try
                {
                    deviceInfos = await _tellmaService.GetDeviceInfos(tenantId, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occurred while getting the list of device infos from tenant {tenantId}");
                    continue;
                }
                foreach (var deviceInfosOfType in deviceInfos.GroupBy(e => e.DeviceType))
                {
                    string deviceType = deviceInfosOfType.Key;
                    IDeviceService deviceService = _deviceServiceFactory.Create(deviceType);
                    foreach (DeviceInfo deviceInfo in deviceInfosOfType)
                    {
                        try
                        {
                            IEnumerable<AttendanceRecord> attendanceRecords = await deviceService.LoadFromDevice(deviceInfo, token);
                            await _tellmaService.Import(tenantId, attendanceRecords, token);
                            _logger.LogInformation($"Imported {attendanceRecords.Count()} records to Tenant {tenantId} from ({deviceInfo})");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"An error occurred while loading from device ({deviceInfo}) and uploading to tenant {tenantId}");
                            continue; // in case a new line was added later:)
                        }
                    }
                }
            }
        }
    }
}