select
    cast("Id" as varchar)            as customer_id,
    cast("Email" as varchar)         as email,
    cast("FirstName" as varchar)     as first_name,
    cast("LastName" as varchar)      as last_name,
    cast("Phone" as varchar)         as phone,
    cast("CreatedAt" as timestamp)   as created_at,
    current_timestamp                as _bronze_loaded_at
from {{ source('raw', 'customers') }}
