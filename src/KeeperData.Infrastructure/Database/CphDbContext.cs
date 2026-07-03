using KeeperData.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace KeeperData.Infrastructure.Database;

public class CphDbContext : DbContext
{
    public CphDbContext(DbContextOptions<CphDbContext> options) : base(options)
    {
    }

    public DbSet<CphEntity> Cphs => Set<CphEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CphEntity>(entity =>
        {
            entity.ToTable("cphs");
            entity.HasNoKey();
            entity.Property(e => e.Cph).HasColumnName("cph");
        });
    }
}
