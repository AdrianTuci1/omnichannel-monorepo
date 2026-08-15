namespace OdooBridge.Configuration;

public sealed class OdooOptions
{
    public const string Section = "Odoo";

    public string BaseUrl { get; init; } = "https://odoo.example.com";

    public string Database { get; init; } = "omnichannel";

    public string Username { get; init; } = "admin";

    public string ApiKey { get; init; } = string.Empty;

    public string ProductModel { get; init; } = "product.template";

    public string OrderModel { get; init; } = "sale.order";

    public string PartnerModel { get; init; } = "res.partner";

    public int PageSize { get; init; } = 200;

    public Uri JsonRpcEndpoint => new(BaseUrl.TrimEnd('/') + "/jsonrpc");
}

public sealed class StoreApiOptions
{
    public const string Section = "StoreApi";

    public string BaseUrl { get; init; } = "http://localhost:5180";
}

public sealed class SyncOptions
{
    public const string Section = "Sync";

    public int IntervalSeconds { get; init; } = 300;

    public bool ProductsEnabled { get; init; } = true;

    public bool OrdersEnabled { get; init; } = true;

    public bool ReverseOrdersEnabled { get; init; } = true;
}
