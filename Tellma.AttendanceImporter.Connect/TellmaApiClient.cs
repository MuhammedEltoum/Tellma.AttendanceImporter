using Microsoft.Extensions.Options;
using Tellma.Api.Dto;
using Tellma.Client;
using Tellma.Utilities.EmailLogger;

namespace Tellma.AttendanceImporter.Connect
{
    public class TellmaApiClient : ITellmaApiClient
    {
        private readonly TellmaClient _tellmaClient;
        private readonly int _tenantId;

        public TellmaApiClient(TellmaClient tellmaClient, IOptions<TellmaOptions> options)
        {
            _tellmaClient = tellmaClient ?? throw new ArgumentNullException(nameof(tellmaClient));
            _tenantId = (options.Value.TenantIds ?? "")
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
                           .FirstOrDefault();
        }

        public async Task<List<ConnectEmployee>> GetConnectEmployees(string deviceName, CancellationToken token)
        {
            var tenantClient = _tellmaClient.Application(_tenantId);

            // Get employee definition
            var employeeDefinitionResult = await tenantClient
                .AgentDefinitions
                .GetEntities(new GetArguments
                {
                    Select = "Id",
                    Filter = "Code = 'Employee'"
                }, token);

            if (employeeDefinitionResult.Data.Count == 0)
                return new List<ConnectEmployee>();

            int employeeDefinitionId = employeeDefinitionResult.Data[0].Id;

            // Get connect employees
            string filter = "Lookup8.Code = 'Connect' " +
                                "AND Code <> 'E0317' " + // Exclude CEO from attendance service
                                "AND FromDate <> NULL " +
                                $"AND FromDate <= '{DateTime.Now.ToString("yyyy-MM-ddT00:00:00")}' " +
                                "AND (Agent2.Code = 'HQ1' OR Agent2.Code = 'HQ2' OR Agent2.Code = 'HQ3' OR Agent2.Code = 'HQ4')";

            var connectEmployeesResult = await tenantClient
                .Agents(employeeDefinitionId)
                .GetEntities(new GetArguments
                {
                    Filter = filter,
                    Top = int.MaxValue,
                    //Expand = "Agent2"
                }, token);

            var issue = connectEmployeesResult
                .Data
                .FirstOrDefault(agent => agent.Code == "E0320");

            var result = connectEmployeesResult.Data
                .Where(e => e.FromDate.HasValue)
                .Select(e => new ConnectEmployee
                {
                    BitrixId = e.ExternalReference,
                    JoiningDate = e.FromDate!.Value.Date,
                    Name = e.Name ?? string.Empty,
                    Code = e.Code ?? string.Empty,
                    DutyStation = e.Agent2?.Code ?? string.Empty
                })
                .ToList();

            return result;
        }
    }
}