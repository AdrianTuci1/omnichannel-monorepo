"""Generează fișierele Parquet goale (schema-only) din data/raw/ pentru smoke-test local.

Nu creează date de business — doar schema corectă a tabelelor store-api, astfel încât
`dbt run` / `dbt test` să poată fi executate end-to-end fără o instanță PostgreSQL live.
În producție, fișierele sunt produse de scripts/ingest_from_postgres.sql.

Utilizare:
    .venv/bin/python scripts/create_empty_landing.py
"""

import os

import duckdb

RAW_DIR = os.path.join(os.path.dirname(__file__), "..", "data", "raw")
os.makedirs(RAW_DIR, exist_ok=True)

SCHEMAS = {
    "orders": """
        Id UUID,
        OrderNumber VARCHAR,
        CustomerId UUID,
        Status INTEGER,
        Currency VARCHAR,
        Notes VARCHAR,
        CreatedAt TIMESTAMP,
        UpdatedAt TIMESTAMP
    """,
    "order_lines": """
        Id UUID,
        OrderId UUID,
        ProductId UUID,
        ProductName VARCHAR,
        Quantity INTEGER,
        unit_price_amount DECIMAL(18, 2),
        unit_price_currency VARCHAR
    """,
    "products": """
        Id UUID,
        Sku VARCHAR,
        Name VARCHAR,
        Description VARCHAR,
        IsActive BOOLEAN,
        CategoryId UUID,
        price_amount DECIMAL(18, 2),
        price_currency VARCHAR,
        CreatedAt TIMESTAMP,
        UpdatedAt TIMESTAMP
    """,
    "categories": """
        Id UUID,
        Name VARCHAR,
        Slug VARCHAR,
        Description VARCHAR,
        ParentId UUID
    """,
    "customers": """
        Id UUID,
        Email VARCHAR,
        FirstName VARCHAR,
        LastName VARCHAR,
        Phone VARCHAR,
        CreatedAt TIMESTAMP
    """,
    "inventory": """
        ProductId UUID,
        QuantityOnHand INTEGER,
        Reserved INTEGER,
        ReorderThreshold INTEGER,
        UpdatedAt TIMESTAMP
    """,
}


def main() -> None:
    con = duckdb.connect(":memory:")
    for name, ddl in SCHEMAS.items():
        target = os.path.join(RAW_DIR, f"{name}.parquet")
        con.execute(f"create table {name} ({ddl})")
        con.execute(f"copy {name} to '{target}' (format parquet)")
        print(f"wrote {target}")


if __name__ == "__main__":
    main()
