with source as (
    select * from {{ ref('bronze_inventory') }}
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
        quantity_on_hand,
        reserved,
        reorder_threshold,
        quantity_on_hand - reserved as available,
        case
            when quantity_on_hand - reserved < 0 then 'overcommitted'
            when quantity_on_hand - reserved = 0 then 'out_of_stock'
            when quantity_on_hand - reserved <= reorder_threshold then 'low_stock'
            else 'in_stock'
        end as stock_status,
        updated_at,
        _bronze_loaded_at
    from deduped
)

select * from final
