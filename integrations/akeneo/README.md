# Akeneo Bridge

Connector .NET 9 care sincronizează bidirecțional produse și atribute între
Akeneo PIM și backend-ul store-api al monorepo-ului Omnichannel.

## Ce face

- Autentificare OAuth2 (password grant) la Akeneo PIM.
- Preluare paginată a produselor (`/api/rest/v1/products`), atributelor
  (`/api/rest/v1/attributes`) și categoriilor (`/api/rest/v1/categories`).
- Mapare produs Akeneo -> produs store-api (SKU, nume, descriere, preț + monedă, categorie).
- Rezolvare categorie după cod/slug; creează categoria în store-api dacă nu există.
- Creare/actualizare produse în store-api (detecție după SKU).
- **Sincronizare inversă** (store-api -> Akeneo): exportă produsele active din
  store-api în Akeneo (PATCH upsert) și asigură existența atributelor configurate
  (nume, descriere, preț).
- Buclă de sincronizare pe interval configurabil (sau rulare unică cu `RunOnce`).

## Flux bidirecțional

### Forward — Akeneo → store-api

Produsele, atributele și categoriile sunt preluate din Akeneo și reconciliate în
store-api (detecție după SKU). A se vedea secțiunea „Mapare produs".

### Reverse — store-api → Akeneo

La fiecare ciclu (dacă `Sync:ReverseProductsEnabled=true`), workerul:

1. Asigură existența atributelor configurate în Akeneo (`NameAttributeCode`,
   `DescriptionAttributeCode`, `PriceAttributeCode`) prin `PATCH /api/rest/v1/attributes/{code}`
   cu tipurile `pim_catalog_text`, `pim_catalog_textarea` și `pim_catalog_price_collection`.
2. Citește produsele active din store-api (`GET /products`) și le exportă în Akeneo
   prin `PATCH /api/rest/v1/products/{identifier}` (upsert după SKU).

Maparea export invers store-api → Akeneo:

| Akeneo (produs) | Sursă store-api |
| --- | --- |
| `identifier` | `sku` |
| `enabled` | `true` (produsele inactive nu apar în `GET /products`) |
| `categories[0]` | `categoryId` rezolvat la slug-ul categoriei (gol dacă nu există) |
| `values[name]` | `name` |
| `values[description]` | `description` (doar dacă e nevidă) |
| `values[price]` | `priceAmount` + `priceCurrency` (format `amount` ca string, `currency`) |

Prețul este serializat ca `price_collection` (`data: [{ "amount": "19.99", "currency": "USD" }]`),
cu `amount` ca string, conform cerințelor API-ului Akeneo.

## Configurare

Prin fișierul `appsettings.json` sau variabile de mediu (separatorul `:` din JSON
devine `__` în numele variabilei):

| Cheie JSON | Variabilă de mediu | Implicit | Descriere |
| --- | --- | --- | --- |
| `Akeneo:BaseUrl` | `Akeneo__BaseUrl` | `https://akeneo.example.com` | URL de bază al instanței Akeneo PIM. |
| `Akeneo:ClientId` | `Akeneo__ClientId` | (gol) | Client id OAuth2. |
| `Akeneo:ClientSecret` | `Akeneo__ClientSecret` | (gol) | Client secret OAuth2. |
| `Akeneo:Username` | `Akeneo__Username` | (gol) | Utilizator API Akeneo. |
| `Akeneo:Password` | `Akeneo__Password` | (gol) | Parola utilizatorului API. |
| `Akeneo:NameAttributeCode` | `Akeneo__NameAttributeCode` | `name` | Cod atribut folosit pentru nume. |
| `Akeneo:DescriptionAttributeCode` | `Akeneo__DescriptionAttributeCode` | `description` | Cod atribut folosit pentru descriere. |
| `Akeneo:PriceAttributeCode` | `Akeneo__PriceAttributeCode` | `price` | Cod atribut folosit pentru preț (price_collection). |
| `Akeneo:DefaultCurrency` | `Akeneo__DefaultCurrency` | `USD` | Monedă fallback când produsul nu are preț. |
| `Akeneo:PageSize` | `Akeneo__PageSize` | `100` | Dimensiune pagină pentru API-ul Akeneo. |
| `StoreApi:BaseUrl` | `StoreApi__BaseUrl` | `http://localhost:5180` | URL-ul store-api. |
| `Sync:IntervalSeconds` | `Sync__IntervalSeconds` | `300` | Interval între ciclurile de sincronizare. |
| `Sync:ProductsEnabled` | `Sync__ProductsEnabled` | `true` | Activează sincronizarea produselor. |
| `Sync:AttributesEnabled` | `Sync__AttributesEnabled` | `true` | Activează preluarea atributelor. |
| `Sync:ReverseProductsEnabled` | `Sync__ReverseProductsEnabled` | `true` | Activează sincronizarea inversă (store-api → Akeneo). |
| `Sync:RunOnce` | `Sync__RunOnce` | `false` | Rulează un singur ciclu, apoi oprește. |

Exemplu de rulare cu variabile de mediu:

```bash
export Akeneo__BaseUrl="https://pim.acme.com"
export Akeneo__ClientId="1_abcd"
export Akeneo__ClientSecret="secret"
export Akeneo__Username="sync_user"
export Akeneo__Password="sync_pass"
export StoreApi__BaseUrl="http://localhost:5180"
export Sync__RunOnce="true"
```

## Build și rulare

```bash
dotnet build integrations/akeneo
dotnet run --project integrations/akeneo
```

## Mapare produs

| Store API | Sursă Akeneo |
| --- | --- |
| `Sku` | `identifier` |
| `Name` | valoarea atributului `NameAttributeCode` (fallback: `identifier`) |
| `Description` | valoarea atributului `DescriptionAttributeCode` |
| `PriceAmount` / `PriceCurrency` | prima intrare din `PriceAttributeCode` (fallback: `0` + `DefaultCurrency`) |
| `CategoryId` | primul cod din `categories`, rezolvat după slug în store-api (creat dacă lipsește) |

Produsele cu `enabled = false` sunt omise (nu sunt create/actualizate în store-api).
