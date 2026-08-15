# data-pipelines — dbt + DuckDB (medallion Bronze/Silver/Gold)

Pipeline-uri de date pentru monorepo-ul Omnichannel E-commerce. Sursa este backend-ul
`apps/store-api` (m1), a cărui schemă fizică este descrisă în
`.agents/bus/contracts.json` și implementată în `StoreDbContext.cs`.

## Arhitectura medallion

```
store-api (PostgreSQL, prod)          apps/store-api
        │  scripts/ingest_from_postgres.sql  (DuckDB postgres attach → COPY TO parquet)
        ▼
data/raw/*.parquet  ── SURSE dbt ({{ source('raw', ...) }})
        ▼
BRONZE  (models/bronze)   oglindă raw + _bronze_loaded_at, coloane tipizate
        ▼
SILVER  (models/silver)   dedup pe cheie naturală, status integer→etichetă,
                          monedă uppercase, email lowercase, totaluri calculate
        ▼
GOLD    (models/gold)     dim_customers, dim_products, dim_categories,
                          fact_orders, fact_order_lines, marts (CLV, daily sales,
                          product performance)
```

## Structura

```
dbt_project.yml            configurație proiect (bronze/silver/gold ca scheme)
profiles.yml               profil DuckDB (target/omnichannel.duckdb)
macros/generate_schema_name.sql   scheme curate (bronze/silver/gold), fără prefix
models/sources.yml         sursele externe (Parquet landing zone)
models/bronze/             oglindă raw a tabelelor store-api
models/silver/             curățare + normalizare
models/gold/               dimensiuni, fapte, marts
tests/                     teste singular (data quality)
scripts/ingest_from_postgres.sql  extracția Postgres → Parquet
```

## Maparea entităților (contract → model)

| Entitate store-api | Bronze | Silver | Gold |
|---|---|---|---|
| orders | bronze_orders | silver_orders | fact_orders |
| order_lines | bronze_order_lines | silver_order_lines | fact_order_lines |
| products | bronze_products | silver_products | dim_products |
| categories | bronze_categories | silver_categories | dim_categories |
| customers | bronze_customers | silver_customers | dim_customers / mart_customer_lifetime_value |
| inventory | bronze_inventory | silver_inventory | (disponibil în dim_products via join) |

`product_embeddings` (tip `vector`) este exclus intenționat din pipeline-ul analitic:
este consumat de integrările de recomandare, nu de raportare.

## Convenții de coloane (derivate din StoreDbContext.cs)

EF Core folosește numele proprietăților ca nume de coloane (PascalCase) pentru scalari,
iar tipul `Money` este mapat explicit ca pereche snake_case:

- `products`: `Id`, `Sku`, `Name`, `Description`, `IsActive`, `CategoryId`, `CreatedAt`,
  `UpdatedAt`, `price_amount`, `price_currency`
- `orders`: `Id`, `OrderNumber`, `CustomerId`, `Status` (int, enum 1..6), `Currency`,
  `Notes`, `CreatedAt`, `UpdatedAt`
- `order_lines`: `Id`, `OrderId`, `ProductId`, `ProductName`, `Quantity`,
  `unit_price_amount`, `unit_price_currency`
- `customers`: `Id`, `Email`, `FirstName`, `LastName`, `Phone`, `CreatedAt`
- `categories`: `Id`, `Name`, `Slug`, `Description`, `ParentId`
- `inventory`: `ProductId`, `QuantityOnHand`, `Reserved`, `ReorderThreshold`, `UpdatedAt`

`OrderStatus` (enum): Draft=1, Pending=2, Paid=3, Shipped=4, Delivered=5, Cancelled=6.
Silver păstrează `status_code` (int) și adaugă `status` (etichetă).

## Instalare

```bash
cd /root/omnichannel-monorepo/data-pipelines
python3 -m venv .venv
.venv/bin/pip install dbt-duckdb
```

## Rulare

```bash
cd /root/omnichannel-monorepo/data-pipelines
export DBT_PROFILES_DIR=$PWD          # folosește profiles.yml local

# 1) Extrage datele din Postgres în landing zone (o singură dată / la fiecare sync)
export PGHOST=... PGPORT=5432 PGDATABASE=store PGPASSWORD=***   # PGPASSWORD exportat separat
.venv/bin/dbt deps                    # (fără pachete externe; doar sanity)
.venv/bin/dbt compile                 # validează SQL + YAML
.venv/bin/dbt run                     # construiește bronze → silver → gold
.venv/bin/dbt test                    # rulează testele (unique/not_null/acceptate/singular)
.venv/bin/dbt docs generate && .venv/bin/dbt docs serve
```

Notă: `dbt compile` nu citește fișierele Parquet (nu execută query-urile); validează
doar sintaxa SQL și rezoluția `ref`/`source`. `dbt run`/`dbt test` au nevoie de
fișierele din `data/raw/` populate prin pasul de ingest.

## Teste de calitate a datelor

- `not_null` / `unique` pe cheile naturale (Id, OrderNumber, Sku, Email, ProductId).
- `accepted_values` pe `silver_orders.status` (cele 6 etichete valide).
- Teste singular în `tests/`:
  - `assert_order_line_totals_match`: `line_total_amount` = `unit_price_amount * quantity`.
  - `assert_no_negative_quantities`: nicio linie cu `quantity <= 0`.

## Export Iceberg (opțional, DuckDB ≥ 1.3)

Pentru a publica layer-ele Silver/Gold ca tabele Iceberg:

```sql
INSTALL iceberg; LOAD iceberg;
COPY (select * from silver.orders) TO 'lake/iceberg/silver_orders' (FORMAT iceberg);
```

Iceberg este ținta de storage pentru lake; în dezvoltare modelele sunt materializate ca
tabele DuckDB (scheme `bronze`, `silver`, `gold`). Exportul se face în pasul de
orchestrare/deploy, nu în fiecare run de dev.
