select
    cast("ProductId" as varchar)       as product_id,
    cast("QuantityOnHand" as integer)  as quantity_on_hand,
    cast("Reserved" as integer)        as reserved,
    cast("ReorderThreshold" as integer) as reorder_threshold,
    cast("UpdatedAt" as timestamp)     as updated_at,
    current_timestamp                  as _bronze_loaded_at
from {{ source('raw', 'inventory') }}
