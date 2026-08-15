with source as (
    select * from {{ ref('bronze_orders') }}
),

deduped as (
    select *
    from source
    qualify row_number() over (
        partition by order_id
        order by updated_at desc nulls last, _bronze_loaded_at desc
    ) = 1
),

final as (
    select
        order_id,
        order_number,
        customer_id,
        status as status_code,
        case status
            when 1 then 'Draft'
            when 2 then 'Pending'
            when 3 then 'Paid'
            when 4 then 'Shipped'
            when 5 then 'Delivered'
            when 6 then 'Cancelled'
            else 'Unknown'
        end as status,
        upper(currency) as currency,
        notes,
        created_at,
        updated_at,
        _bronze_loaded_at
    from deduped
)

select * from final
