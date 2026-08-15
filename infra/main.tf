locals {
  environment = var.environment
  base_name   = lower("${var.name_prefix}-${var.environment}")

  tags = {
    environment = var.environment
    project     = "omnichannel"
    managed_by  = "terraform"
  }
}

# Sufix global unic pentru resursele care au constrângere de unicitate la nivel
# de Azure (Redis, Service Bus, Storage). 4 bytes -> 8 caractere hexa.
resource "random_id" "suffix" {
  byte_length = 4
}

resource "azurerm_resource_group" "this" {
  name     = "rg-${local.base_name}"
  location = var.location
  tags     = local.tags
}

module "network" {
  source = "./modules/network"

  name_prefix            = local.base_name
  resource_group_name    = azurerm_resource_group.this.name
  location               = var.location
  vnet_address_space     = var.vnet_address_space
  aks_subnet_prefix      = var.aks_subnet_prefix
  postgres_subnet_prefix = var.postgres_subnet_prefix
  pe_subnet_prefix       = var.pe_subnet_prefix
  tags                   = local.tags
}

module "aks" {
  source = "./modules/aks"

  name_prefix         = local.base_name
  resource_group_name = azurerm_resource_group.this.name
  location            = var.location
  aks_subnet_id       = module.network.aks_subnet_id
  kubernetes_version  = var.kubernetes_version
  system_node_size    = var.aks_system_node_size
  system_node_count   = var.aks_system_node_count
  system_node_min     = var.aks_system_node_min
  system_node_max     = var.aks_system_node_max
  user_node_size      = var.aks_user_node_size
  user_node_count     = var.aks_user_node_count
  user_node_min       = var.aks_user_node_min
  user_node_max       = var.aks_user_node_max
  tags                = local.tags
}

module "postgres" {
  source = "./modules/postgres"

  name_prefix            = local.base_name
  resource_group_name    = azurerm_resource_group.this.name
  location               = var.location
  subnet_id              = module.network.postgres_subnet_id
  private_dns_zone_id    = module.network.postgres_private_dns_zone_id
  administrator_login    = var.postgres_admin_login
  administrator_password = var.postgres_admin_password
  postgres_version       = var.postgres_version
  sku_name               = var.postgres_sku_name
  storage_mb             = var.postgres_storage_mb
  backup_retention_days  = var.postgres_backup_retention_days
  high_availability      = var.postgres_high_availability
  database_name          = var.postgres_database_name
  tags                   = local.tags
}

module "redis" {
  source = "./modules/redis"

  name_prefix         = local.base_name
  resource_group_name = azurerm_resource_group.this.name
  location            = var.location
  subnet_id           = module.network.pe_subnet_id
  private_dns_zone_id = module.network.redis_private_dns_zone_id
  unique_suffix       = random_id.suffix.hex
  sku_name            = var.redis_sku_name
  capacity            = var.redis_capacity
  redis_version       = var.redis_version
  tags                = local.tags
}

module "service_bus" {
  source = "./modules/service-bus"

  name_prefix         = local.base_name
  resource_group_name = azurerm_resource_group.this.name
  location            = var.location
  subnet_id           = module.network.pe_subnet_id
  private_dns_zone_id = module.network.servicebus_private_dns_zone_id
  unique_suffix       = random_id.suffix.hex
  sku                 = var.service_bus_sku
  queues              = var.service_bus_queues
  tags                = local.tags
}
