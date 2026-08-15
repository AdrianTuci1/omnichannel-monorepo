namespace Cdp.Worker.Events;

/// <summary>
/// Tipurile de entități din domeniul store-api (m1) care pot emite evenimente.
/// Valori folosite în câmpul <c>entityType</c> al envelope-ului de eveniment.
/// </summary>
public static class EntityTypes
{
    public const string Product = "product";
    public const string Category = "category";
    public const string Customer = "customer";
    public const string Order = "order";
    public const string OrderLine = "order_line";
    public const string Inventory = "inventory";
}

/// <summary>
/// Catalogul de tipuri de evenimente de domeniu emise de store-api.
/// Valori folosite în câmpul <c>eventType</c> al envelope-ului de eveniment.
/// </summary>
public static class EventTypes
{
    public const string ProductCreated = "product.created";
    public const string ProductUpdated = "product.updated";
    public const string ProductActivated = "product.activated";
    public const string ProductDeactivated = "product.deactivated";

    public const string CategoryCreated = "category.created";
    public const string CategoryUpdated = "category.updated";

    public const string CustomerCreated = "customer.created";
    public const string CustomerUpdated = "customer.updated";

    public const string OrderCreated = "order.created";
    public const string OrderSubmitted = "order.submitted";
    public const string OrderPaid = "order.paid";
    public const string OrderShipped = "order.shipped";
    public const string OrderDelivered = "order.delivered";
    public const string OrderCancelled = "order.cancelled";

    public const string InventoryUpdated = "inventory.updated";
}
