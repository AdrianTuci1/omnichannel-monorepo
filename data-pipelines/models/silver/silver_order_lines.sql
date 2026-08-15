with source as (
    select * from {{ ref('bronze_order_lines') }}
),

deduped as (
    select *
    from source
    qualify row_number() over (partition by order_line_id order by _bronze_loaded_at desc) = 1
),

final as (
    select
        order_line_id,
        order_id,
        product_id,
        product_name,
        quantity,
        unit_price_amount,
        upper(unit_price_currency) as unit_price_currency,
        round(unit_price_amount * quantity, 2) as line_total_amount,
        unit_price_currency as line_total_currency,
        _bronze_loaded_at
    from deduped
)

select * from final
