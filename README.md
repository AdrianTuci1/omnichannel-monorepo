# Omnichannel E-commerce — Monorepo

Monorepo de producție pentru o platformă de e-commerce omnichannel: store API,
microservicii de recomandări și CDP (Customer Data Platform), clienți web/Android/POS,
integrații (Odoo, Akeneo), pipeline-uri de date (dbt), infra (Terraform) și Helm.

Acest README acoperă pornirea locală a stivei de bază prin Docker Compose.

## Servicii (stiva Docker Compose)

| Serviciu    | Imagine / build             | Port  | Descriere                                                |
|-------------|-----------------------------|-------|----------------------------------------------------------|
| `postgres`  | `pgvector/pgvector:pg16`    | 5432  | PostgreSQL 16 + extensia `vector` (pgvector)             |
| `redis`     | `redis:7-alpine`            | 6379  | Cache / lock distribuit                                   |
| `store-api` | build `apps/store-api`      | 5180  | API principal (.NET 9) — produse, comenzi, clienți, stoc |
| `recommender`| build `integrations/recommender` | 5181 | Recomandări hibride (.NET 9)                        |
| `cdp`       | build `integrations/cdp`    | —     | Worker CDP (.NET 9) — DuckDB + Iceberg, consum evenimente |
| `frontend`  | build `clients/web`         | 3000  | Client web (Next.js 15)                                  |

## Cerințe

- Docker Engine + Docker Compose v2 (`docker compose`).
- `curl` + `jq` pentru scriptul de seed.

## Pornire rapidă

```bash
# 1. Pregătește variabilele de mediu
cp .env.example .env
#    → editează .env și setează POSTGRES_PASSWORD și JWT_SECRET (nu lăsa valorile de exemplu)

# 2. Construiește și pornește toate serviciile
docker compose up -d --build

# 3. Verifică sănătatea
curl http://localhost:5180/health          # → {"status":"ok"}
docker compose ps                          # toate serviciile trebuie să fie "healthy"/"running"
```

Postgres și Redis pornesc cu healthcheck; `store-api` așteaptă să fie sănătoase
(`depends_on` cu `condition: service_healthy`) înainte de pornire.

## Baza de date (InMemory vs PostgreSQL)

`store-api` alege providerul în funcție de `ConnectionStrings__StoreApi`
(`builder.Configuration.GetConnectionString("StoreApi")` în `Program.cs`):

- **Gol / nesetat (default)** → `UseInMemoryDatabase` + `EnsureCreated()`: schema este
  creată automat la pornire. Modul dev, funcțional out-of-the-box — fără migrări de aplicat.
- **Setat** → `UseNpgsql` cu `UseVector()` (pgvector). Tabelele se creează prin migrările
  EF Core (vezi mai jos).

Pentru a activa PostgreSQL, setează în `.env`:

```
CONNECTION_STRINGS__STOREAPI=Host=postgres;Port=5432;Database=store;Username=store;Password=<parola>
```

Apoi generează și aplică migrările (factory-ul design-time `StoreDbContextFactory` este
deja configurat pentru `dotnet ef`; citește aceeași variabilă de mediu):

```bash
cd apps/store-api
export ConnectionStrings__StoreApi='Host=localhost;Port=5432;Database=store;Username=store;Password=<parola>'
dotnet tool install --global dotnet-ef      # o singură dată, dacă nu există
dotnet ef migrations add InitialCreate \
  --project src/StoreApi.Infrastructure \
  --startup-project src/StoreApi.Api
dotnet ef database update \
  --project src/StoreApi.Infrastructure \
  --startup-project src/StoreApi.Api
```

Notă: migrările EF nu sunt încă generate în repo (worker-ul de backend a adăugat factory-ul
design-time; generarea migrației inițiale rămâne de făcut la prima activare PostgreSQL).

## Seed (date de test)

```bash
./scripts/seed.sh
# sau explicit:
API_BASE_URL=http://localhost:5180 ./scripts/seed.sh
```

Scriptul creează 4 categorii și 9 produse prin API (`POST /categories`, `POST /products`).

## Acces

- Frontend web: http://localhost:3000
- Store API:    http://localhost:5180 (Swagger nu e configurat; endpoint-uri minimal API)
- Recommender:  http://localhost:5181/health

## Endpoint-uri principale (store-api)

- `GET  /health` — liveness
- `POST /auth/register`, `/auth/login`, `/auth/refresh`, `/auth/logout` — autentificare JWT
- `GET/POST /cart/items` — coș (necesită autentificare)
- `GET/POST/PUT/DELETE /products`, `/products/{id}`, `/products/{id}/reviews`, `/products/{id}/inventory`, `/products/{id}/related`
- `GET/POST/PUT/DELETE /categories`, `/categories/{id}`
- `GET/POST/PUT/DELETE /customers`, `/customers/{id}`
- `GET/POST/PUT/DELETE /orders`, `/orders/{id}`
- `GET/POST /events` — outbox de evenimente (consumat de CDP)

## Oprire și curățare

```bash
docker compose down            # oprește și șterge containerele (păstrează volumele)
docker compose down -v         # șterge și volumele (postgres_data, cdp_data)
```

## Note / devieri față de contract

- **Providerul DB este selectabil, nu fix**: `store-api` suportă PostgreSQL
  (`ConnectionStrings__StoreApi` + `UseNpgsql` + pgvector) și InMemory (default). Compose-ul
  pornește cu InMemory (funcțional imediat); PostgreSQL se activează setând
  `CONNECTION_STRINGS__STOREAPI` și aplicând migrările EF Core (vezi mai sus).
- **Migrările EF Core nu sunt încă generate** în repo — la prima activare PostgreSQL rulează
  `dotnet ef migrations add InitialCreate`. `Program.cs` nu aplică migrările automat la
  pornire (doar `EnsureCreated` în modul InMemory).
- **`Jwt__Secret`** este folosit de autentificare (`/auth/*`); fallback-ul din cod este o
  cheie de dev, dar Compose-ul o impune prin `${JWT_SECRET:?}`.
- **`Redis__ConnectionString`** este folosit pentru cache distribuit (coș, refresh tokens);
  dacă Redis e indisponibil, `store-api` cade automat pe cache in-memory
  (`AbortOnConnectFail=false`).
- **CDP** pornește fără Azure Service Bus (opțional): cu `SERVICEBUS_CONNECTION_STRING` gol,
  consumă evenimente exclusiv prin poller-ul `GET /events?since=...`.
- `integrations/recommender/Directory.Build.props` este o copie a celui din
  `integrations/` (necesară pentru build-ul Docker, cu context izolat pe
  `integrations/recommender`). Păstrează-l sincronizat dacă îl modifici pe cel părinte.
