variable "name_prefix" {
  description = "Prefix pentru numele resurselor."
  type        = string
}

variable "resource_group_name" {
  description = "Numele resource group-ului."
  type        = string
}

variable "location" {
  description = "Regiunea Azure."
  type        = string
}

variable "subnet_id" {
  description = "ID-ul subnet-ului delegat (Microsoft.DBforPostgreSQL/flexibleServers)."
  type        = string
}

variable "private_dns_zone_id" {
  description = "ID-ul zonei DNS private PostgreSQL."
  type        = string
}

variable "administrator_login" {
  description = "Login-ul administratorului PostgreSQL."
  type        = string
}

variable "administrator_password" {
  description = "Parola administratorului PostgreSQL."
  type        = string
  sensitive   = true
}

variable "postgres_version" {
  description = "Versiunea PostgreSQL."
  type        = string
  default     = "16"
}

variable "sku_name" {
  description = "SKU-ul Flexible Server."
  type        = string
  default     = "GP_Standard_D2ds_v4"
}

variable "storage_mb" {
  description = "Dimensiunea storage-ului în MB."
  type        = number
  default     = 32768
}

variable "backup_retention_days" {
  description = "Retenția backup-urilor în zile."
  type        = number
  default     = 7
}

variable "high_availability" {
  description = "Activează HA zone-redundant."
  type        = bool
  default     = true
}

variable "database_name" {
  description = "Numele bazei de date aplicației."
  type        = string
  default     = "store"
}

variable "tags" {
  description = "Tag-uri aplicate resurselor."
  type        = map(string)
  default     = {}
}
