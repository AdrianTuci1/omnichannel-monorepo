# Store API

Minimal API .NET 9 (EF Core) pentru domeniul omnichannel e-commerce.

## Stack

- **Provider DB**: InMemory (local/dev, default) sau PostgreSQL (`Npgsql` + `pgvector`).
- **Cache distribuit**: Redis (`StackExchange.Redis`) cu fallback automat in-memory dacă Redis nu este disponibil.
- **Auth**: JWT (HS256) access tokens + refresh tokens opace rotite, stocate în Redis.

## Config (env / appsettings)

| Cheie                      | Descriere                                        | Default                                        |
| -------------------------- | ------------------------------------------------ | ---------------------------------------------- |
| `ConnectionStrings__StoreApi` | Connection string PostgreSQL (activează PostgreSQL) | *(neconfigurat → InMemory)*                    |
| `Jwt:Secret`               | Secret HMAC-SHA256 pentru semnarea JWT           | `dev-only-secret-key-change-me-in-production-0123456789` |
| `Redis:ConnectionString`   | Endpoint Redis                                   | `localhost:6379`                               |

## Autentificare

Endpointele mutaționale (`POST`/`PUT`/`DELETE` pentru products, orders, reviews, categories, customers, inventory) și toate rutele `/cart` necesită `Authorization: Bearer <accessToken>`. `GET`-urile sunt publice.

- `POST /auth/register` `{email, password, firstName, lastName}` → `201 {userId}`
- `POST /auth/login` `{email, password}` → `200 {accessToken, refreshToken, expiresIn}` (sau `401`)
- `POST /auth/refresh` `{refreshToken}` → `200` (rotire; vechiul token devine invalid)
- `POST /auth/logout` `{refreshToken}` → `204`

Access token: TTL 15 min. Refresh token: opac, TTL 7 zile, stocat în Redis sub `refresh:<token>`.

## Coș (`/cart`, per utilizator autentificat)

- `GET /cart` → lista coșului
- `POST /cart/items` `{productId, quantity}`
- `PUT /cart/items/{productId}` `{quantity}`
- `DELETE /cart/items/{productId}`

Cheia Redis: `cart:<userId>`.

## Căutare

- `GET /products/search?q=<term>` — caută în `Name`, `Sku`, `Description` (EF Core `.Contains`).

## Migrări EF Core (PostgreSQL)

Migrarea `InitialCreate` include `CREATE EXTENSION vector` și coloana `product_embeddings.embedding vector(384)`.

Aplicare (din `apps/store-api`):

```bash
export PATH="$PATH:$HOME/.dotnet/tools"          # dacă dotnet-ef nu e pe PATH
export ConnectionStrings__StoreApi="Host=<host>;Database=store_api;Username=<user>;Password=<pass>"

dotnet ef database update \
  --project src/StoreApi.Infrastructure \
  --startup-project src/StoreApi.Api
```

Generare migrare nouă:

```bash
dotnet ef migrations add <Name> \
  --project src/StoreApi.Infrastructure \
  --startup-project src/StoreApi.Api \
  --output-dir Migrations
```

> Notă: extensia PostgreSQL `vector` necesită privilegiul `CREATE` pe baza de date. În bazele administrate (RDS etc.) instalați extensia manual (`CREATE EXTENSION IF NOT EXISTS vector;`) și aplicați migrarea.

## Build & test

```bash
dotnet build store-api.sln --nologo
dotnet test store-api.sln --nologo
```
