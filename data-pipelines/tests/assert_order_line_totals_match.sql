select
    order_line_id
from {{ ref('silver_order_lines') }}
where line_total_amount != round(unit_price_amount * quantity, 2)
