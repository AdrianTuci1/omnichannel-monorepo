select
    cast("Id" as varchar)            as order_id,
    cast("OrderNumber" as varchar)   as order_number,
    cast("CustomerId" as varchar)    as customer_id,
    cast("Status" as integer)        as status,
    cast("Currency" as varchar)      as currency,
    cast("Notes" as varchar)         as notes,
    cast("CreatedAt" as timestamp)   as created_at,
    cast("UpdatedAt" as timestamp)   as updated_at,
    current_timestamp                as _bronze_loaded_at
from {{ source('raw', 'orders') }}
