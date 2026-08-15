select
    cast("Id" as varchar)            as category_id,
    cast("Name" as varchar)          as name,
    cast("Slug" as varchar)          as slug,
    cast("Description" as varchar)   as description,
    cast("ParentId" as varchar)      as parent_id,
    current_timestamp                as _bronze_loaded_at
from {{ source('raw', 'categories') }}
