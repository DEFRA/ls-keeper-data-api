using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace KeeperData.Infrastructure.Database.Repositories;

/// <summary>
/// Resolves CPH access out of the cached read model by walking Party.Email to PartyRole to
/// Holding.Cph. The read model masters this data, so nothing here is derived from Mongo.
/// </summary>
public class CphAssociationsRepository(IReadModelSqliteCacheService cacheService) : ICphAssociationsRepository
{
    private readonly IReadModelSqliteCacheService _cacheService = cacheService;

    public async Task<List<CphAssociationSourceDto>> FindByEmailAsync(
        string email,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || roles.Count == 0)
            return [];

        var dbPath = _cacheService.GetCurrentDbPath()
            ?? throw new InvalidOperationException(
                "The SAM read model is not cached locally, so CPH associations cannot be resolved.");

        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseSqlite($"Data Source={dbPath};Mode=ReadOnly")
            .Options;

        await using var dbContext = new ReadModelDbContext(options);

        // The read model stores emails as SAM supplies them and indexes them case-sensitively, so
        // both sides are lowered here. Roles are stored lower case.
        var normalisedEmail = email.Trim().ToLowerInvariant();
        var normalisedRoles = roles.Select(role => role.Trim().ToLowerInvariant()).ToArray();

        var query =
            from role in dbContext.PartyRoles.AsNoTracking()
            join party in dbContext.Parties.AsNoTracking() on role.PartyId equals party.Id
            join holding in dbContext.Holdings.AsNoTracking() on role.HoldingId equals holding.Id
            where party.Email != null
                && party.Email.ToLower() == normalisedEmail
                && normalisedRoles.Contains(role.Role)
            select new CphAssociationSourceDto
            {
                PartyRoleId = role.Id,
                CphNumber = holding.Cph,
                Role = role.Role,
                PartyId = party.SourcePartyId,
                HoldingId = holding.Id,
                HoldingName = holding.FeatureName
            };

        var associations = await query.ToListAsync(cancellationToken);

        // A party role is held per herd, so the same CPH and role can arrive more than once.
        return associations
            .GroupBy(association => (association.CphNumber, association.Role), StringTupleComparer.Instance)
            .Select(group => group.First())
            .OrderBy(association => association.CphNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(association => association.Role, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class StringTupleComparer : IEqualityComparer<(string Cph, string Role)>
    {
        public static readonly StringTupleComparer Instance = new();

        public bool Equals((string Cph, string Role) left, (string Cph, string Role) right) =>
            StringComparer.OrdinalIgnoreCase.Equals(left.Cph, right.Cph)
            && StringComparer.OrdinalIgnoreCase.Equals(left.Role, right.Role);

        public int GetHashCode((string Cph, string Role) value) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(value.Cph),
                StringComparer.OrdinalIgnoreCase.GetHashCode(value.Role));
    }
}