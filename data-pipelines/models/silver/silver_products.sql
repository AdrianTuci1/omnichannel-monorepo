with source as (
    select * from {{ ref('bronze_products') }}
),

deduped as (
    select *
    from source
    qualify row_number() over (
        partition by product_id
        order by updated_at desc nulls last, _bronze_loaded_at desc
    ) = 1
),

final as (
    select
        product_id,
        upper(sku) as sku,
        name,
        description,
        is_active,
        category_id,
        price_amount,
        upper(price_currency) as price_currency,
        created_at,
        updated_at,
        _bronze_loaded_at
    from deduped
)

select * from final
