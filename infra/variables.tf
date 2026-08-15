variable "environment" {
  description = "Numele mediului (dev, staging, prod). Folosit în nume și tag-uri."
  type        = string
  default     = "dev"
}

variable "name_prefix" {
  description = "Prefix scurt pentru resurse (folosit în numele resurselor Azure)."
  type        = string
  default     = "omni"
}

variable "location" {
  description = "Regiunea Azure în care se creează resursele."
  type        = string
  default     = "West Europe"
}

variable "subscription_id" {
  description = "ID-ul subscription-ului Azure. Gol => se folosește ARM_SUBSCRIPTION_ID / az login."
  type        = string
  default     = ""
}

variable "tenant_id" {
  description = "ID-ul tenant-ului Azure AD. Gol => se folosește ARM_TENANT_ID / az login."
  type        = string
  default     = ""
}

# ---------- Rețea ----------

variable "vnet_address_space" {
  description = "Spațiul de adrese al VNet-ului."
  type        = list(string)
  default     = ["10.0.0.0/16"]
}

variable "aks_subnet_prefix" {
  description = "Prefixul subnet-ului pentru nodurile AKS."
  type        = string
  default     = "10.0.0.0/20"
}

variable "postgres_subnet_prefix" {
  description = "Prefixul subnet-ului delegat pentru PostgreSQL Flexible Server."
  type        = string
  default     = "10.0.16.0/24"
}

variable "pe_subnet_prefix" {
  description = "Prefixul subnet-ului pentru private endpoints (Redis, Service Bus)."
  type        = string
  default     = "10.0.32.0/24"
}

# ---------- AKS ----------

variable "kubernetes_version" {
  description = "Versiunea Kubernetes a clusterului AKS."
  type        = string
  default     = "1.31"
}

variable "aks_system_node_size" {
  description = "VM size pentru node pool-ul de sistem (addon-uri critice)."
  type        = string
  default     = "Standard_D2ds_v5"
}

variable "aks_system_node_count" {
  description = "Numărul inițial de noduri în pool-ul de sistem."
  type        = number
  default     = 1
}

variable "aks_system_node_min" {
  description = "Numărul minim de noduri în pool-ul de sistem."
  type        = number
  default     = 1
}

variable "aks_system_node_max" {
  description = "Numărul maxim de noduri în pool-ul de sistem."
  type        = number
  default     = 3
}

variable "aks_user_node_size" {
  description = "VM size pentru node pool-ul de aplicație."
  type        = string
  default     = "Standard_D4ds_v5"
}

variable "aks_user_node_count" {
  description = "Numărul inițial de noduri în pool-ul de aplicație."
  type        = number
  default     = 2
}

variable "aks_user_node_min" {
  description = "Numărul minim de noduri în pool-ul de aplicație."
  type        = number
  default     = 2
}

variable "aks_user_node_max" {
  description = "Numărul maxim de noduri în pool-ul de aplicație."
  type        = number
  default     = 6
}

# ---------- PostgreSQL Flexible Server ----------

variable "postgres_admin_login" {
  description = "Login-ul administratorului PostgreSQL."
  type        = string
  default     = "omniadmin"
}

variable "postgres_admin_password" {
  description = "Parola administratorului PostgreSQL. FURNIZATĂ OBLIGATORIU prin tfvars / env (nu există default)."
  type        = string
  sensitive   = true
}

variable "postgres_version" {
  description = "Versiunea PostgreSQL (major)."
  type        = string
  default     = "16"
}

variable "postgres_sku_name" {
  description = "SKU-ul Flexible Server (General Purpose / Memory Optimized pentru HA zonal)."
  type        = string
  default     = "GP_Standard_D2ds_v4"
}

variable "postgres_storage_mb" {
  description = "Dimensiunea storage-ului în MB."
  type        = number
  default     = 32768
}

variable "postgres_backup_retention_days" {
  description = "Retenția backup-urilor în zile."
  type        = number
  default     = 7
}

variable "postgres_high_availability" {
  description = "Activează high availability zone-redundant (necesită SKU General Purpose / Memory Optimized)."
  type        = bool
  default     = true
}

variable "postgres_database_name" {
  description = "Numele bazei de date aplicației."
  type        = string
  default     = "store"
}

# ---------- Redis ----------

variable "redis_sku_name" {
  description = "Tier-ul Azure Cache for Redis (Basic, Standard, Premium)."
  type        = string
  default     = "Standard"
}

variable "redis_capacity" {
  description = "Capacitatea Redis (unități; 0-6 pentru Basic/Standard)."
  type        = number
  default     = 1
}

variable "redis_version" {
  description = "Versiunea Redis."
  type        = string
  default     = "6"
}

# ---------- Service Bus ----------

variable "service_bus_sku" {
  description = "Tier-ul Service Bus (Basic, Standard, Premium)."
  type        = string
  default     = "Standard"
}

variable "service_bus_queues" {
  description = "Cozile provisionate în namespace-ul Service Bus."
  type        = list(string)
  default     = ["orders", "inventory-reserved", "notifications"]
}
