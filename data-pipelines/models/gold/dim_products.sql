select
    p.product_id,
    p.sku,
    p.name,
    p.description,
    p.is_active,
    p.price_amount,
    p.price_currency,
    p.category_id,
    coalesce(c.name, 'Uncategorized') as category_name,
    c.slug as category_slug,
    p.created_at,
    p.updated_at
from {{ ref('silver_products') }} as p
left join {{ ref('silver_categories') }} as c
    on p.category_id = c.category_id
