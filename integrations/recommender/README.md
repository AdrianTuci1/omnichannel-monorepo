# Recommender — Microserviciu hibrid de recomandări

Serviciu .NET 9 (ASP.NET Core minimal API) care oferă recomandări hibride pentru catalogul
Omnichannel, combinând:

- **Content-based** — similaritate cosinus peste embedding-uri vectoriale (feature hashing,
  384 dimensiuni), calculate local la încărcarea catalogului.
- **Collaborative (de bază)** — co-ocurență item-item (produse cumpărate împreună) și
  agregare user-based (după istoricul de comenzi al clientului).
- **Hibrid** — îmbinarea celor două scoruri, normalizate min-max și ponderate prin
  `contentWeight`.

Datele nu mai provin dintr-un PostgreSQL comun: serviciul importă catalogul și comenzile
**reale din Store API prin HTTP**, la pornire (și leneș, la prima cerere, dacă Store API-ul
nu era încă disponibil). Stocarea este **InMemory** (în oglindă cu Store API-ul local, care
rulează pe InMemory în mediul de dezvoltare).

## Cerințe

- .NET SDK 9.0
- Store API (`apps/store-api`) pornit și accesibil (default `http://localhost:5180`)

## Configurare

Secțiunea `StoreApi` din `appsettings.json` (sau variabile de mediu `StoreApi__*`):

| Cheie       | Default                 | Descriere                        |
|-------------|-------------------------|----------------------------------|
| `BaseUrl`   | `http://localhost:5180` | Base URL al Store API            |

Secțiunea `Recommender` (variabile de mediu `Recommender__*`):

| Cheie                 | Default | Descriere                                                    |
|-----------------------|---------|--------------------------------------------------------------|
| `DefaultLimit`        | `10`    | Număr implicit de recomandări                                |
| `MaxLimit`            | `50`    | Plafon pentru `limit`                                        |
| `ContentWeight`       | `0.6`   | Ponderea componentei content-based în scorul hibrid (0..1)   |
| `MinSimilarity`       | `0.0`   | Similaritate cosinus minimă acceptată (content-based)        |
| `CandidateMultiplier` | `3`     | Factor de over-fetch al pool-ului de candidați               |

## Build

```bash
dotnet build
```

## Rulare

```bash
dotnet run --project integrations/recommender
```

## Endpoint-uri

### `GET /health`
Liveness — nu atinge datele.

### `GET /health/ready`
Readiness — returnează 503 dacă catalogul nu a fost încă încărcat din Store API.

### `GET /recommendations/{productId}`
Recomandări pentru un produs seed. Parametri opționali de query:

- `limit` — număr de rezultate (default `DefaultLimit`, maxim `MaxLimit`).
- `strategy` — `hybrid` (default), `content` (content-based), `collaborative` (item-item).
- `contentWeight` — ponderea content-based pentru strategia hibridă (default `ContentWeight`).

Răspunsul este un array simplu, gata de consumat de Store API la `GET /products/{id}/related`:

```json
[
  { "productId": "3fa85f64-...", "name": "Nume produs", "score": 0.812 }
]
```

### `GET /products/{productId}/related`
Alias local al lui `GET /recommendations/{productId}` (același răspuns și aceiași parametri),
pentru testare directă fără a traversa Store API-ul.

### `GET /recommendations/customer/{customerId}`
Recomandări pentru un client, pe baza produselor cumpărate anterior (exclude produsele
deja achiziționate). Parametri: `limit`, `strategy` (`hybrid` default sau `collaborative`)
și `contentWeight`.

### `GET /recommendations/search?text=...`
Căutare content-based după text liber — se construiește un embedding cu același algoritm
de feature hashing folosit la încărcarea catalogului și se caută vecinii cei mai apropiați.
Parametri: `text` (obligatoriu), `limit`.

## Arhitectură

```
integrations/recommender/
├── Program.cs                       # wiring DI + endpoint-uri minimal API
├── Contracts.cs                     # contractele de răspuns HTTP
├── Configuration/
│   ├── RecommenderOptions.cs        # opțiuni de recomandare
│   └── StoreApiOptions.cs           # opțiuni conexiune Store API
├── Clients/
│   └── StoreApiClient.cs            # client HTTP (GET /products, GET /orders)
├── Domain/
│   ├── Product.cs
│   ├── Order.cs
│   ├── OrderLine.cs
│   ├── RecommendationItem.cs
│   └── RecommendationStrategy.cs
├── Persistence/
│   ├── RecommenderDbContext.cs      # magazin InMemory (produse, comenzi, linii)
│   └── StoreDataSynchronizer.cs     # import din Store API (IHostedService + lazy)
├── Embeddings/
│   ├── EmbeddingService.cs          # feature hashing (384 dims)
│   ├── EmbeddingStore.cs            # produs → vector (magazin în memorie)
│   └── VectorMath.cs                # cosine similarity
└── Recommendations/
    ├── ContentBasedRecommender.cs   # similaritate cosinus în memorie
    ├── CollaborativeRecommender.cs  # co-ocurență item-item + user-based
    ├── HybridRecommender.cs         # îmbinare ponderată a scorurilor
    └── ScoreNormalizer.cs           # normalizare min-max
```

## Algoritmi

**Content-based.** Se încarcă embedding-ul produsului seed și se calculează similaritatea
cosinus față de toate celelalte produse active; scorul este similaritatea brută.

**Collaborative item-item.** Pentru produsul seed se colectează comenzile care îl conțin,
apoi se numără co-ocurențele cu celelalte produse din aceleași comenzi. Scorul este
fracția comenzilor seed care conțin și candidatul.

**Collaborative user-based.** Se agregă produsele cumpărate de client (excluzând comenzile
anulate), iar candidații sunt produsele co-cumpărate în aceleași comenzi, fără produsele
deja achiziționate.

**Hibrid.** Ambele liste se normalizează min-max pe `[0,1]`, apoi scorul final este
`contentWeight * content + (1 - contentWeight) * collaborative`; rezultatele se unesc și
se sortează descrescător.
