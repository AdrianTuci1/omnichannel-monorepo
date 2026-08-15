-- Ingest store-api (PostgreSQL) -> landing zone Parquet (data/raw/).
-- Rulează cu duckdb CLI, cu variabilele de mediu PG* setate:
--   export PGHOST=... PGPORT=5432 PGDATABASE=store PGPUSER=... PGPASSWORD=...
--   duckdb < scripts/ingest_from_postgres.sql
--
-- Produce fișierele pe care le citesc sursele din models/sources.yml.

INSTALL postgres;
LOAD postgres;

ATTACH
    'host=' || getenv('PGHOST') ||
    ' port=' || getenv('PGPORT') ||
    ' dbname=' || getenv('PGDATABASE') ||
    ' user=' || getenv('PGUSER') ||
    ' password=' || getenv('PGPASSWORD')
AS store (TYPE postgres, READ_ONLY);

COPY (
    select
        "Id", "OrderNumber", "CustomerId", "Status", "Currency",
        "Notes", "CreatedAt", "UpdatedAt"
    from store.public.orders
) TO 'data/raw/orders.parquet' (FORMAT parquet);

COPY (
    select
        "Id", "OrderId", "ProductId", "ProductName", "Quantity",
        "unit_price_amount", "unit_price_currency"
    from store.public.order_lines
) TO 'data/raw/order_lines.parquet' (FORMAT parquet);

COPY (
    select
        "Id", "Sku", "Name", "Description", "IsActive", "CategoryId",
        "price_amount", "price_currency", "CreatedAt", "UpdatedAt"
    from store.public.products
) TO 'data/raw/products.parquet' (FORMAT parquet);

COPY (
    select
        "Id", "Name", "Slug", "Description", "ParentId"
    from store.public.categories
) TO 'data/raw/categories.parquet' (FORMAT parquet);

COPY (
    select
        "Id", "Email", "FirstName", "LastName", "Phone", "CreatedAt"
    from store.public.customers
) TO 'data/raw/customers.parquet' (FORMAT parquet);

COPY (
    select
        "ProductId", "QuantityOnHand", "Reserved", "ReorderThreshold", "UpdatedAt"
    from store.public.inventory
) TO 'data/raw/inventory.parquet' (FORMAT parquet);
