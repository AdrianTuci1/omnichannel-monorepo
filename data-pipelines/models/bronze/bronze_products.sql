select
    cast("Id" as varchar)            as product_id,
    cast("Sku" as varchar)           as sku,
    cast("Name" as varchar)          as name,
    cast("Description" as varchar)   as description,
    cast("IsActive" as boolean)      as is_active,
    cast("CategoryId" as varchar)    as category_id,
    cast("price_amount" as decimal(18, 2))   as price_amount,
    cast("price_currency" as varchar)        as price_currency,
    cast("CreatedAt" as timestamp)   as created_at,
    cast("UpdatedAt" as timestamp)   as updated_at,
    current_timestamp                as _bronze_loaded_at
from {{ source('raw', 'products') }}
