using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Tellma.AttendanceImporter.Connect
{
    public class ConnectApiClient : IConnectApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly IReadOnlyList<string> _dailyReportEmails;
        private readonly ILogger<ConnectApiClient> _logger;

        public ConnectApiClient(
            HttpClient httpClient,
            IOptions<ConnectApiOptions> options,
            ILogger<ConnectApiClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var optionsValue = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _apiKey = optionsValue.ApiKey ?? throw new ArgumentException("ApiKey is required");

            // Set base address if not already set
            _httpClient.BaseAddress ??= new Uri("https://attend.axc.ae/");

            _dailyReportEmails = (optionsValue.DailyReportEmails ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(email => email.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .ToList()
                .AsReadOnly();
        }

        public async Task<List<ConnectAttendanceRecord>> GetAttendanceRecords(
            string location,
            DateTime? lastSyncTime,
            CancellationToken token)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"Location={Uri.EscapeDataString(location)}"
                };

                if (lastSyncTime.HasValue)
                {
                    // Format with timezone properly
                    var lastSyncString = lastSyncTime.Value.ToString("yyyy-MM-ddTHH:mm:ss");
                    queryParams.Add($"LastSyncTime={Uri.EscapeDataString(lastSyncString)}");
                }

                var queryString = string.Join("&", queryParams);
                var url = $"api/attendance?{queryString}%2B04:00";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-API-Key", _apiKey);

                var response = await _httpClient.SendAsync(request, token);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(token);

                return JsonSerializer.Deserialize<List<ConnectAttendanceRecord>>(
                    content,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }) ?? new List<ConnectAttendanceRecord>();

                //// Filter records after deserialization (more efficient)
                //var filteredResult = lastSyncTime.HasValue
                //    ? result.Where(r => r.Time > lastSyncTime.Value).ToList()
                //    : result;

                //_logger.LogInformation(
                //    "Fetched {TotalCount} records, {FilteredCount} after filtering for location {Location}",
                //    result.Count, filteredResult.Count, location);

                //return filteredResult;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error calling Connect API for location {Location}", location);
                throw new HttpRequestException($"Error calling Connect API for location '{location}': {ex.Message}", ex);
            }
        }

        public List<string> GetDailyReportEmails() => _dailyReportEmails.ToList();
    }
}