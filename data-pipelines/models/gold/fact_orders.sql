with lines_agg as (
    select
        order_id,
        sum(line_total_amount) as total_amount,
        count(*) as line_count
    from {{ ref('silver_order_lines') }}
    group by order_id
)

select
    o.order_id,
    o.order_number,
    o.customer_id,
    c.email as customer_email,
    c.full_name as customer_name,
    o.status_code,
    o.status,
    o.currency,
    o.notes,
    o.created_at,
    o.updated_at,
    coalesce(l.total_amount, 0) as total_amount,
    coalesce(l.line_count, 0) as line_count
from {{ ref('silver_orders') }} as o
left join {{ ref('dim_customers') }} as c
    on o.customer_id = c.customer_id
left join lines_agg as l
    on o.order_id = l.order_id
