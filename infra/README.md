# Infrastructura Omnichannel — Azure (Terraform)

Infrastructura de producție a monorepo-ului Omnichannel, definită 100% ca cod
(Terraform + provider `hashicorp/azurerm`). Nu există placeholder-e — toate
resursele au configurație reală.

## Ce provisionează

| Serviciu | Modul | Resurse |
|----------|-------|---------|
| Rețea | `modules/network` | VNet + 3 subnet-uri (AKS, PostgreSQL delegat, private endpoints) + 3 private DNS zones (`privatelink.postgres.database.azure.com`, `privatelink.redis.cache.windows.net`, `privatelink.servicebus.windows.net`) + link-uri VNet |
| AKS multi-AZ | `modules/aks` | Cluster AKS (UserAssigned identity, Azure CNI + Network Policy, oms_agent/Container Insights), 2 node pool-uri răspândite pe zonele 1–3 (sistem + aplicație), Log Analytics workspace, role assignments |
| PostgreSQL | `modules/postgres` | PostgreSQL Flexible Server (VNet injection privat, HA zone-redundant zonele 1+2), bază de date `store`, extensii `vector`, `pg_trgm`, `btree_gin` |
| Redis | `modules/redis` | Azure Cache for Redis (Standard, TLS 1.2, acces privat prin private endpoint) |
| Service Bus | `modules/service-bus` | Namespace Service Bus (Standard, privat) + cozile `orders`, `inventory-reserved`, `notifications` + regulă SAS `store-api` (Listen+Send) |

## Aliniere cu backend-ul (apps/store-api)

Configurația de mai jos acoperă direct dependențele din `StoreApi.Infrastructure.csproj`:

- `Npgsql.EntityFrameworkCore.PostgreSQL` → PostgreSQL Flexible Server + baza `store`; extensia `vector` este activată prin `azure.extensions` pentru `Pgvector.EntityFrameworkCore` (tipul `vector(384)` din `StoreDbContext`).
- `RedLock.net` → Azure Cache for Redis (acces SSL, privat).
- `Azure.Messaging.ServiceBus` → Namespace Service Bus + cozi; connection string-ul regulii `store-api` este expus ca output.

## Structură

```
infra/
├── main.tf                    # resource group + invocarea modulelor
├── variables.tf               # input-uri cu default-uri reale
├── outputs.tf                 # endpoint-uri / connection strings (sensitive)
├── versions.tf                # provider + backend (azurerm)
├── providers.tf               # configurarea providerului azurerm
├── terraform.tfvars.example   # șablon de valori
└── modules/
    ├── network/               # VNet, subnet-uri, private DNS
    ├── aks/                   # cluster AKS multi-AZ
    ├── postgres/              # PostgreSQL Flexible Server
    ├── redis/                 # Azure Cache for Redis
    └── service-bus/           # Service Bus namespace + cozi
```

## Cerințe

- Terraform `>= 1.8.0` (testat cu 1.9.2).
- Azure CLI (`az login`) sau variabilele de mediu `ARM_*` pentru autentificare.

## Utilizare

```bash
cd infra

# 1. Configurare valori
cp terraform.tfvars.example terraform.tfvars
#    -> setați obligatoriu postgres_admin_password (și opțional subscription_id/tenant_id)

# 2. Initializare
terraform init

# 3. Validare
terraform validate

# 4. Plan / apply
terraform plan
terraform apply
```

### State remote (backend)

Backend-ul este `azurerm` (Azure Storage). La primul `init`, furnizați
configurația backend-ului:

```bash
terraform init \
  -backend-config=resource_group_name=<rg> \
  -backend-config=storage_account_name=<sa> \
  -backend-config=container_name=tfstate \
  -backend-config=key=omnichannel-<environment>.tfstate
```

Pentru validare locală fără backend: `terraform init -backend=false`.

### Outputs relevante pentru Helm / CI

După `apply`, Terraform expune ca output-uri (cele sensitive nu se afișează în
clar, dar sunt disponibile programatic):

- `aks_kube_config_raw` — kubeconfig pentru `kubectl`/Helm.
- `postgres_connection_string` — connection string Npgsql (Host/FQDN privat).
- `redis_connection_string` — connection string Redis (SSL).
- `service_bus_connection_string` — connection string regula `store-api`.

## Alegeri de arhitectură

- **Networking privat integral**: PostgreSQL folosește VNet injection (subnet
  delegat), iar Redis + Service Bus folosesc private endpoints — fără expunere
  publică (`public_network_access_enabled = false`).
- **Multi-AZ**: node pool-urile AKS răspândite pe zonele 1–3; PostgreSQL cu
  high availability zone-redundant (primar zona 1, standby zona 2).
- **Unicitate globală**: numele Redis și Service Bus primesc un sufix unic
  (`random_id`), deoarece trebuie să fie unice la nivel Azure.
- **Lock file commit-uit**: `.terraform.lock.hcl` este intenționat păstrat în
  git pentru build-uri reproductibile (pinning al versiunilor de provider).
