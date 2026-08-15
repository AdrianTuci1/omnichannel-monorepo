select
    order_line_id
from {{ ref('silver_order_lines') }}
where quantity <= 0
