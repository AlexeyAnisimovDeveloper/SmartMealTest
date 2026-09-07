using Microsoft.EntityFrameworkCore;
using SmartMealOrderConsoleApp.Entities;

namespace SmartMealOrderConsoleApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<DishEntity> Dishes => Set<DishEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DishEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.HasIndex(e => e.Code).IsUnique();

            entity.Property(e => e.ExternalId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("numeric(10,2)");
            entity.Property(e => e.IsWeighted).IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}