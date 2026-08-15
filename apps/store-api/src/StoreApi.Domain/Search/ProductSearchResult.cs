using StoreApi.Domain.Entities;

namespace StoreApi.Domain.Search;

/// <summary>
/// Rezultat al unei căutări semantice/vectoriale de produse.
/// Similarity este în intervalul [-1, 1]; 1 = identic.
/// </summary>
public sealed record ProductSearchResult(Product Product, double Similarity);
