with source as (
    select * from {{ ref('bronze_categories') }}
),

deduped as (
    select *
    from source
    qualify row_number() over (partition by category_id order by _bronze_loaded_at desc) = 1
),

final as (
    select
        category_id,
        name,
        lower(slug) as slug,
        description,
        parent_id,
        _bronze_loaded_at
    from deduped
)

select * from final
