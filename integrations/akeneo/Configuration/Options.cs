namespace AkeneoBridge.Configuration;

/// <summary>Configurație pentru conexiunea la Akeneo PIM.</summary>
public sealed class AkeneoOptions
{
    public const string Section = "Akeneo";

    public string BaseUrl { get; init; } = "https://akeneo.example.com";

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string NameAttributeCode { get; init; } = "name";

    public string DescriptionAttributeCode { get; init; } = "description";

    public string PriceAttributeCode { get; init; } = "price";

    public string DefaultCurrency { get; init; } = "USD";

    public int PageSize { get; init; } = 100;

    public Uri OAuthTokenEndpoint => new(BaseUrl.TrimEnd('/') + "/api/oauth/v1/token");
}

/// <summary>Configurație pentru backend-ul store-api (destinația de sincronizare).</summary>
public sealed class StoreApiOptions
{
    public const string Section = "StoreApi";

    public string BaseUrl { get; init; } = "http://localhost:5180";
}

/// <summary>Configurație pentru bucla de sincronizare.</summary>
public sealed class SyncOptions
{
    public const string Section = "Sync";

    public int IntervalSeconds { get; init; } = 300;

    public bool ProductsEnabled { get; init; } = true;

    public bool AttributesEnabled { get; init; } = true;

    public bool ReverseProductsEnabled { get; init; } = true;

    public bool RunOnce { get; init; }
}
