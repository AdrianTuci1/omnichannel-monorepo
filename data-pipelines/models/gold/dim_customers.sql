select
    customer_id,
    email,
    email_domain,
    first_name,
    last_name,
    full_name,
    phone,
    created_at
from {{ ref('silver_customers') }}
