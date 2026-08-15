with orders as (
    select
        customer_id,
        customer_email,
        customer_name,
        order_id,
        total_amount,
        created_at
    from {{ ref('fact_orders') }}
)

select
    customer_id,
    customer_email,
    customer_name,
    count(order_id) as order_count,
    sum(total_amount) as lifetime_value,
    min(created_at) as first_order_at,
    max(created_at) as last_order_at
from orders
group by 1, 2, 3
