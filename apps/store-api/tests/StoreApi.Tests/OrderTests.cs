using StoreApi.Domain.Entities;
using Xunit;

namespace StoreApi.Tests;

public class OrderTests
{
    [Fact]
    public void Total_Is_Sum_Of_Line_Totals()
    {
        var order = new Order(Guid.NewGuid(), "USD");
        order.AddLine(Guid.NewGuid(), "Widget", 10.00m, 2);
        order.AddLine(Guid.NewGuid(), "Gadget", 3.50m, 4);

        Assert.Equal(34.00m, order.Total.Amount);
        Assert.Equal("USD", order.Total.Currency);
    }

    [Fact]
    public void Empty_Order_Has_Zero_Total()
    {
        var order = new Order(Guid.NewGuid(), "EUR");

        Assert.Equal(0m, order.Total.Amount);
        Assert.Equal("EUR", order.Total.Currency);
    }

    [Fact]
    public void Removing_Line_Recomputes_Total()
    {
        var order = new Order(Guid.NewGuid(), "USD");
        var line = order.AddLine(Guid.NewGuid(), "Widget", 5.00m, 3);

        Assert.Equal(15.00m, order.Total.Amount);

        order.RemoveLine(line.Id);

        Assert.Equal(0m, order.Total.Amount);
    }
}
