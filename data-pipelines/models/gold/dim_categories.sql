select
    c.category_id,
    c.name,
    c.slug,
    c.description,
    c.parent_id,
    p.name as parent_name,
    p.slug as parent_slug
from {{ ref('silver_categories') }} as c
left join {{ ref('silver_categories') }} as p
    on c.parent_id = p.category_id
