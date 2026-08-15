namespace Recommender.Api.Configuration;

/// <summary>
/// Configurare pentru serviciul de recomandări, legată din secțiunea <c>Recommender</c>.
/// </summary>
public sealed class RecommenderOptions
{
    public const string SectionName = "Recommender";

    public const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=omnichannel;Username=postgres;Password=postgres";

    /// <summary>Șir de conexiune către PostgreSQL-ul comun (schema Store API + extensia pgvector).</summary>
    public string ConnectionString { get; init; } = DefaultConnectionString;

    /// <summary>Numărul implicit de recomandări returnate când <c>limit</c> nu e specificat.</summary>
    public int DefaultLimit { get; init; } = 10;

    /// <summary>Plafonul maxim pentru <c>limit</c>.</summary>
    public int MaxLimit { get; init; } = 50;

    /// <summary>Ponderea componentei content-based în scorul hibrid (0..1).</summary>
    public double ContentWeight { get; init; } = 0.6;

    /// <summary>Similaritatea minimă (cosinus) acceptată pentru candidații content-based.</summary>
    public double MinSimilarity { get; init; } = 0.0;

    /// <summary>Factor de over-fetch folosit pentru a menține un pool suficient de candidați.</summary>
    public int CandidateMultiplier { get; init; } = 3;
}
