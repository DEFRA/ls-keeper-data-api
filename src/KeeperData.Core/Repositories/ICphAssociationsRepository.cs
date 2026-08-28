using KeeperData.Core.DTOs;

namespace KeeperData.Core.Repositories;

/// <summary>
/// Reads a party's current CPH access from the locally cached SAM read model.
/// </summary>
public interface ICphAssociationsRepository
{
    /// <summary>
    /// Every holding the parties matching <paramref name="email"/> hold one of
    /// <paramref name="roles"/> on. Email matching is case-insensitive.
    /// </summary>
    Task<List<CphAssociationSourceDto>> FindByEmailAsync(
        string email,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default);
}