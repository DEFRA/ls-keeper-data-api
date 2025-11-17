using KeeperData.Core.Documents;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class RoleTypeLookupService : IRoleTypeLookupService
{
    /// <summary>
    /// To complete implementation when seeding is completed or to replace.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<RoleDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        return await Task.FromResult(new RoleDocument
        {
            IdentifierId = id,
            Code = "Code",
            Name = "Name"
        });
    }

    /// <summary>
    /// To complete implementation when seeding is completed or to replace.
    /// </summary>
    /// <param name="roleName"></param>
    /// <returns></returns>
    public async Task<(string? roleTypeId, string? roleTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue)) return (null, null);

        var roles = new[]
        {
            new { Id = "b2637b72-2196-4a19-bdf0-85c7ff66cf60", Code = "LIVESTOCKKEEPER", Name = "Livestock Keeper" },
            new { Id = "2de15dc1-19b9-4372-9e81-a9a2f87fd197", Code = "LIVESTOCKOWNER", Name = "Livestock Owner" },
            new { Id = "13738191-9aaf-43e7-ac56-e78eb79871e8", Code = "FACILITYOPERATOR", Name = "Facility Operator" },
            new { Id = "335f6838-9b1d-4037-b9a2-966779b1f0e7", Code = "FACILITYOWNER", Name = "Facility Owner" },
            new { Id = "8184ae3d-c3c4-4904-b1b8-539eeadbf245", Code = "AGENT", Name = "Agent" },
            new { Id = "3a084108-b888-4d89-8f7d-6231533100e1", Code = "CITIZEN", Name = "Citizen" },
            new { Id = "cde2250b-b991-4063-b044-019a6e534b58", Code = "CUSTOMER", Name = "Customer" },
            new { Id = "5053be9f-685a-4779-a663-ce85df6e02e8", Code = "CPHHOLDER", Name = "CPH Holder" },
            new { Id = "63511467-c3c1-4b9d-aee8-098e48f611a4", Code = "REGISTRANT", Name = "Registrant" },
            new { Id = "bbd3e838-488a-4718-ac64-72e6f51ae33d", Code = "LANDOWNER", Name = "Land Owner" },
            new { Id = "bad912bd-c127-4400-b6ae-d045a27ef8d0", Code = "EXPORTER", Name = "Exporter" },
            new { Id = "d99e3bac-8d08-45f7-9039-11af47897588", Code = "ONEOFFEXPORTER", Name = "One Off Exporter" }
        };

        var match = roles.FirstOrDefault(r => r.Code.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));
        return await Task.FromResult(match is null ? (null, null) : (match.Id, match.Name));
    }
}