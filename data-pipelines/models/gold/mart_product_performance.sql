with lines as (
    select
        product_id,
        sku,
        product_name,
        category_name,
        quantity,
        line_total_amount,
        status
    from {{ ref('fact_order_lines') }}
)

select
    product_id,
    sku,
    product_name,
    category_name,
    count(*) as order_line_count,
    sum(quantity) as units_sold,
    sum(line_total_amount) as revenue,
    sum(case when status = 'Cancelled' then quantity else 0 end) as units_cancelled
from lines
group by 1, 2, 3, 4
