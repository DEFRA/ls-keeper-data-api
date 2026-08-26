using KeeperData.Core.Entities.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace KeeperData.Infrastructure.Database;

/// <summary>
/// Read-only access to the normalised SAM read model published by the data bridge as
/// views/krds-db_yyyyMMddHHmmss.sqlite.
/// </summary>
public class ReadModelDbContext : DbContext
{
    public ReadModelDbContext(DbContextOptions<ReadModelDbContext> options) : base(options)
    {
    }

    public DbSet<PartyEntity> Parties => Set<PartyEntity>();

    public DbSet<HoldingEntity> Holdings => Set<HoldingEntity>();

    public DbSet<PartyRoleEntity> PartyRoles => Set<PartyRoleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PartyEntity>(entity =>
        {
            entity.ToTable("Party");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<HoldingEntity>(entity =>
        {
            entity.ToTable("Holding");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<PartyRoleEntity>(entity =>
        {
            entity.ToTable("PartyRole");
            entity.HasKey(e => e.Id);
        });
    }
}