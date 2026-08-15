using Microsoft.EntityFrameworkCore;
using StoreApi.Domain.Common;
using StoreApi.Domain.Entities;
using StoreApi.Infrastructure.Persistence;
using Xunit;

namespace StoreApi.Tests;

public class ReproTests
{
    [Fact]
    public void Repro_Read_Complex_Type_From_InMemory()
    {
        var opts = new DbContextOptionsBuilder<StoreDbContext>()
            .UseInMemoryDatabase("repro-db")
            .Options;

        using var db = new StoreDbContext(opts);
        db.Database.EnsureCreated();

        var cat = new Category("General", "general", "default");
        db.Categories.Add(cat);
        db.SaveChanges();

        var product = new Product("SKU1", "Name", new Money(10m, "USD"), cat.Id);
        db.Products.Add(product);
        db.SaveChanges();

        try
        {
            var loaded = db.Products.AsNoTracking().First(p => p.Id == product.Id);
            Assert.Equal(10m, loaded.Price.Amount);
        }
        catch (Exception ex)
        {
            Assert.Fail($"EXCEPTION: {ex}");
        }
    }

    [Fact]
    public void Repro_Save_Order_With_Lines_InMemory()
    {
        var opts = new DbContextOptionsBuilder<StoreDbContext>()
            .UseInMemoryDatabase("repro-order-db")
            .Options;

        using var db = new StoreDbContext(opts);
        db.Database.EnsureCreated();

        var cat = new Category("General", "general", "default");
        var customer = new Customer("a@b.com", "A", "B");
        db.Categories.Add(cat);
        db.Customers.Add(customer);
        db.SaveChanges();

        var product = new Product("SKU2", "Name", new Money(10m, "USD"), cat.Id);
        db.Products.Add(product);
        db.SaveChanges();

        var order = new Order(customer.Id, "USD");
        order.AddLine(product.Id, product.Name, product.Price.Amount, 2);
        db.Orders.Add(order);

        try
        {
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            Assert.Fail($"EXCEPTION on save: {ex}");
        }

        try
        {
            var loaded = db.Orders.AsNoTracking().Include(o => o.Lines).First(o => o.Id == order.Id);
            Assert.Equal(20m, loaded.Total.Amount);
        }
        catch (Exception ex)
        {
            Assert.Fail($"EXCEPTION on read: {ex}");
        }
    }
}
