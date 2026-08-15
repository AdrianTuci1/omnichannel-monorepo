using Microsoft.EntityFrameworkCore;
using Recommender.Api.Domain;

namespace Recommender.Api.Persistence;

/// <summary>
/// Magazin în memorie pentru datele importate din Store API (produse, comenzi și linii
/// de comandă), populat de <see cref="StoreDataSynchronizer"/>. Folosește providerul
/// InMemory, în oglindă cu Store API-ul local (m1), care rulează tot pe InMemory.
/// </summary>
public sealed class RecommenderDbContext : DbContext
{
    /// <summary>Statusul <c>Cancelled</c> din enum-ul OrderStatus al Store API (stocat ca int).</summary>
    public const int CancelledOrderStatus = 6;

    public RecommenderDbContext(DbContextOptions<RecommenderDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Sku).IsRequired().HasMaxLength(64);
            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
            e.Property(p => p.Description).HasMaxLength(2000);
            e.Property(p => p.PriceAmount).HasPrecision(18, 2);
            e.Property(p => p.PriceCurrency).HasMaxLength(3);
            e.Property(p => p.CategoryId).IsRequired();
            e.Property(p => p.IsActive).IsRequired();
            e.Property(p => p.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.OrderNumber).IsRequired().HasMaxLength(40);
            e.Property(o => o.CustomerId).IsRequired();
            e.Property(o => o.Status).IsRequired();
            e.Property(o => o.Currency).IsRequired().HasMaxLength(3);
            e.Property(o => o.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<OrderLine>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.OrderId).IsRequired();
            e.Property(l => l.ProductId).IsRequired();
            e.Property(l => l.ProductName).IsRequired().HasMaxLength(200);
            e.Property(l => l.Quantity).IsRequired();

            e.HasIndex(l => l.OrderId);
            e.HasIndex(l => l.ProductId);
        });
    }
}
