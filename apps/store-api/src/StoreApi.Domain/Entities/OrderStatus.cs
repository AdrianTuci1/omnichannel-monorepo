namespace StoreApi.Domain.Entities;

public enum OrderStatus
{
    Draft = 1,
    Pending = 2,
    Paid = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
}
