with order_metrics as (
    select
        date_trunc('day', created_at) as order_date,
        currency,
        status,
        count(distinct order_id) as order_count,
        sum(total_amount) as gross_revenue,
        sum(line_count) as line_item_count
    from {{ ref('fact_orders') }}
    group by 1, 2, 3
),

line_metrics as (
    select
        date_trunc('day', order_created_at) as order_date,
        line_total_currency as currency,
        status,
        sum(quantity) as units_sold,
        sum(line_total_amount) as line_revenue
    from {{ ref('fact_order_lines') }}
    group by 1, 2, 3
)

select
    coalesce(o.order_date, l.order_date) as order_date,
    coalesce(o.currency, l.currency) as currency,
    coalesce(o.status, l.status) as status,
    coalesce(o.order_count, 0) as order_count,
    coalesce(l.units_sold, 0) as units_sold,
    coalesce(o.gross_revenue, 0) as gross_revenue,
    coalesce(o.line_item_count, 0) as line_item_count
from order_metrics as o
full outer join line_metrics as l
    on o.order_date = l.order_date
    and o.currency = l.currency
    and o.status = l.status
