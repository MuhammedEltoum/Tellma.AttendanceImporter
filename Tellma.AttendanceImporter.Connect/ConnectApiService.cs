using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using Tellma.AttendanceImporter.Contract;

namespace Tellma.AttendanceImporter.Connect
{
    public class ConnectApiService : IDeviceService
    {
        private IHttpClientFactory _clientFactory;
        public string DeviceType => "Connect";
        public ConnectApiService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory; 
        }
        public async Task<IEnumerable<AttendanceRecord>> LoadFromDevice(DeviceInfo info, CancellationToken token)
        {
            // Implementation for Connect devices goes here
            // Call API endpoints, process data, and return attendance records
            // Deserialize JSON response from Connect API into AttendanceRecord objects
            HttpClient client = _clientFactory.CreateClient("ConnectApiClient");

            UriBuilder builder = new("https://api.connectdevice.com/attendance") // placeholder URL
            {
                Query = $"location={UrlEncoder.Default.Encode(info.IpAddress??"")}&lastSyncTime={UrlEncoder.Default.Encode(info.LastSyncTime.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture))}"//format using Gregorian calendar
            };

            var message = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = builder.Uri
            };
            
            var response = await client.SendAsync(message, token); // Placeholder for actual API call
            var connectAttendanceRecords = await response.Content.ReadFromJsonAsync<List<ConnectAttendanceRecord>>(cancellationToken: token);

            return connectAttendanceRecords!.Select(car => new AttendanceRecord (info)
            {
                UserId = car.UserId,
                Time = car.Time,
                IsIn = car.IsIn
            });
        }

    }
}
