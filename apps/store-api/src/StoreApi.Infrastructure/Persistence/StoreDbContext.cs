using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Pgvector;
using StoreApi.Domain.Entities;

namespace StoreApi.Infrastructure.Persistence;

public sealed class StoreDbContext : DbContext
{
    private bool _isInMemory;

    public StoreDbContext(DbContextOptions<StoreDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Inventory> Inventories => Set<Inventory>();

    public DbSet<ProductEmbedding> ProductEmbeddings => Set<ProductEmbedding>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<User> Users => Set<User>();

    public DbSet<EventOutbox> EventOutbox => Set<EventOutbox>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<WarehouseInventory> WarehouseInventories => Set<WarehouseInventory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _isInMemory = string.Equals(Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);

        ConfigureCategory(modelBuilder);
        ConfigureProduct(modelBuilder);
        ConfigureCustomer(modelBuilder);
        ConfigureOrder(modelBuilder);
        ConfigureOrderLine(modelBuilder);
        ConfigureInventory(modelBuilder);
        ConfigureProductEmbedding(modelBuilder);
        ConfigureReview(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureEventOutbox(modelBuilder);
        ConfigureWarehouse(modelBuilder);
        ConfigureWarehouseInventory(modelBuilder);
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.Property(c => c.Slug).IsRequired().HasMaxLength(200);
            e.Property(c => c.Description).HasMaxLength(1000);

            e.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(c => c.Slug).IsUnique();
        });
    }

    private void ConfigureProduct(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(p => p.Id);
            e.Property(p => p.Sku).IsRequired().HasMaxLength(64);
            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
            e.Property(p => p.Description).HasMaxLength(2000);
            e.Property(p => p.IsActive).IsRequired();

            if (_isInMemory)
            {
                // InMemory nu suportă complex types la materializare; owned types funcționează.
                e.OwnsOne(p => p.Price, money =>
                {
                    money.Property(m => m.Amount).HasColumnName("price_amount").HasPrecision(18, 2);
                    money.Property(m => m.Currency).HasColumnName("price_currency").HasMaxLength(3);
                });
            }
            else
            {
                e.ComplexProperty(p => p.Price, money =>
                {
                    money.Property(m => m.Amount).HasColumnName("price_amount").HasPrecision(18, 2);
                    money.Property(m => m.Currency).HasColumnName("price_currency").HasMaxLength(3);
                });
            }

            e.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(p => p.Sku).IsUnique();
            e.HasIndex(p => p.CategoryId);
        });
    }

    private static void ConfigureCustomer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("customers");
            e.HasKey(c => c.Id);
            e.Property(c => c.Email).IsRequired().HasMaxLength(320);
            e.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
            e.Property(c => c.LastName).IsRequired().HasMaxLength(100);
            e.Property(c => c.Phone).HasMaxLength(40);

            e.HasIndex(c => c.Email).IsUnique();
        });
    }

    private static void ConfigureOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(o => o.Id);
            e.Property(o => o.OrderNumber).IsRequired().HasMaxLength(40);
            e.Property(o => o.Currency).IsRequired().HasMaxLength(3);
            e.Property(o => o.Status).HasConversion<int>();
            e.Property(o => o.PaymentMethod).HasConversion<int>();
            e.Property(o => o.PaymentStatus).HasConversion<int>();
            e.Property(o => o.Notes).HasMaxLength(1000);

            e.Ignore(o => o.Total);

            e.HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(o => o.Lines)
                .WithOne(l => l.Order)
                .HasForeignKey(l => l.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Navigation(o => o.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);

            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.HasIndex(o => o.CustomerId);
        });
    }

    private void ConfigureOrderLine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderLine>(e =>
        {
            e.ToTable("order_lines");
            e.HasKey(l => l.Id);
            e.Property(l => l.ProductName).IsRequired().HasMaxLength(200);
            e.Property(l => l.Quantity).IsRequired();

            if (_isInMemory)
            {
                e.OwnsOne(l => l.UnitPrice, money =>
                {
                    money.Property(m => m.Amount).HasColumnName("unit_price_amount").HasPrecision(18, 2);
                    money.Property(m => m.Currency).HasColumnName("unit_price_currency").HasMaxLength(3);
                });
            }
            else
            {
                e.ComplexProperty(l => l.UnitPrice, money =>
                {
                    money.Property(m => m.Amount).HasColumnName("unit_price_amount").HasPrecision(18, 2);
                    money.Property(m => m.Currency).HasColumnName("unit_price_currency").HasMaxLength(3);
                });
            }

            e.HasIndex(l => l.OrderId);
            e.HasIndex(l => l.ProductId);
        });
    }

    private static void ConfigureInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inventory>(e =>
        {
            e.ToTable("inventory");
            e.HasKey(i => i.ProductId);
            e.Property(i => i.QuantityOnHand).IsRequired();
            e.Property(i => i.Reserved).IsRequired();
            e.Property(i => i.ReorderThreshold).IsRequired();

            e.HasOne(i => i.Product)
                .WithOne()
                .HasForeignKey<Inventory>(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureProductEmbedding(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductEmbedding>(e =>
        {
            e.ToTable("product_embeddings");
            e.HasKey(pe => pe.ProductId);
            e.Property(pe => pe.ModelVersion).IsRequired();

            e.HasOne(pe => pe.Product)
                .WithOne()
                .HasForeignKey<ProductEmbedding>(pe => pe.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        if (_isInMemory)
        {
            // InMemory nu are tipul nativ `vector`: stocăm embedding-ul ca string.
            modelBuilder.Entity<ProductEmbedding>()
                .Property(pe => pe.Embedding)
                .HasConversion(v => v.ToString(), s => new Vector(s));
        }
        else
        {
            modelBuilder.HasPostgresExtension("vector");
            modelBuilder.Entity<ProductEmbedding>()
                .Property(pe => pe.Embedding)
                .HasColumnType("vector(384)")
                .IsRequired();
        }
    }

    private static void ConfigureReview(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(e =>
        {
            e.ToTable("reviews");
            e.HasKey(r => r.Id);
            e.Property(r => r.Title).IsRequired().HasMaxLength(200);
            e.Property(r => r.Comment).HasMaxLength(2000);
            e.Property(r => r.Rating).IsRequired();

            e.HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(r => r.ProductId);
            e.HasIndex(r => r.CustomerId);
        });
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(320);
            e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(100);
            e.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            e.Property(u => u.LastName).IsRequired().HasMaxLength(100);

            e.HasIndex(u => u.Email).IsUnique();
        });
    }

    private static void ConfigureEventOutbox(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventOutbox>(e =>
        {
            e.ToTable("event_outbox");
            e.HasKey(ev => ev.Id);
            e.Property(ev => ev.Type).IsRequired().HasMaxLength(100);
            e.Property(ev => ev.Payload).IsRequired();

            e.HasIndex(ev => ev.CreatedAt);
        });
    }

    private static void ConfigureWarehouse(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Warehouse>(e =>
        {
            e.ToTable("warehouses");
            e.HasKey(w => w.Id);
            e.Property(w => w.Name).IsRequired().HasMaxLength(200);
            e.Property(w => w.Code).IsRequired().HasMaxLength(64);
            e.Property(w => w.IsActive).IsRequired();

            e.HasIndex(w => w.Code).IsUnique();
        });
    }

    private static void ConfigureWarehouseInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WarehouseInventory>(e =>
        {
            e.ToTable("warehouse_inventory");
            e.HasKey(wi => new { wi.WarehouseId, wi.ProductId });
            e.Property(wi => wi.QuantityOnHand).IsRequired();
            e.Property(wi => wi.Reserved).IsRequired();

            e.HasOne(wi => wi.Warehouse)
                .WithMany(w => w.Inventory)
                .HasForeignKey(wi => wi.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(wi => wi.Product)
                .WithMany()
                .HasForeignKey(wi => wi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(wi => wi.ProductId);
        });
    }
}
