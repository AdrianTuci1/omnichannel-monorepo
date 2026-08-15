namespace Recommender.Api.Configuration;

/// <summary>
/// Configurare pentru conexiunea HTTP către Store API, legată din secțiunea <c>StoreApi</c>.
/// </summary>
public sealed class StoreApiOptions
{
    public const string SectionName = "StoreApi";

    public const string DefaultBaseUrl = "http://localhost:5180";

    /// <summary>Base URL al Store API (ex. <c>http://localhost:5180</c>).</summary>
    public string BaseUrl { get; init; } = DefaultBaseUrl;
}
