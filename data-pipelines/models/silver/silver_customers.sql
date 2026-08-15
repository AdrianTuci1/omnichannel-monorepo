with source as (
    select * from {{ ref('bronze_customers') }}
),

deduped as (
    select *
    from source
    qualify row_number() over (
        partition by customer_id
        order by created_at desc, _bronze_loaded_at desc
    ) = 1
),

final as (
    select
        customer_id,
        lower(email) as email,
        split_part(email, '@', 2) as email_domain,
        first_name,
        last_name,
        first_name || ' ' || last_name as full_name,
        phone,
        created_at,
        _bronze_loaded_at
    from deduped
)

select * from final
