select
    cast("Id" as varchar)                    as order_line_id,
    cast("OrderId" as varchar)               as order_id,
    cast("ProductId" as varchar)             as product_id,
    cast("ProductName" as varchar)           as product_name,
    cast("Quantity" as integer)              as quantity,
    cast("unit_price_amount" as decimal(18, 2))   as unit_price_amount,
    cast("unit_price_currency" as varchar)         as unit_price_currency,
    current_timestamp                        as _bronze_loaded_at
from {{ source('raw', 'order_lines') }}
