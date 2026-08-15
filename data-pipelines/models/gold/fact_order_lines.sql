select
    l.order_line_id,
    l.order_id,
    o.order_number,
    l.product_id,
    p.sku,
    p.name as product_name,
    p.category_name,
    l.product_name as product_name_snapshot,
    o.customer_id,
    o.status_code,
    o.status,
    l.quantity,
    l.unit_price_amount,
    l.unit_price_currency,
    l.line_total_amount,
    l.line_total_currency,
    o.created_at as order_created_at
from {{ ref('silver_order_lines') }} as l
left join {{ ref('silver_orders') }} as o
    on l.order_id = o.order_id
left join {{ ref('dim_products') }} as p
    on l.product_id = p.product_id
