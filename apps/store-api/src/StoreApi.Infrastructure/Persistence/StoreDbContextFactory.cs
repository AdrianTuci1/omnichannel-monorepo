using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StoreApi.Infrastructure.Persistence;

/// <summary>
/// Fabrica design-time pentru `dotnet ef`. Folosește PostgreSQL (nu InMemory) astfel încât
/// migrările să includă tipurile native (vector, complex types etc.).
/// </summary>
public sealed class StoreDbContextFactory : IDesignTimeDbContextFactory<StoreDbContext>
{
    public StoreDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__StoreApi")
            ?? "Host=localhost;Database=store_api;Username=store;Password=store";

        var options = new DbContextOptionsBuilder<StoreDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseVector())
            .Options;

        return new StoreDbContext(options);
    }
}
