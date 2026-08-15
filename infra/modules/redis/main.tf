resource "azurerm_redis_cache" "this" {
  name                          = "redis-${var.name_prefix}-${var.unique_suffix}"
  resource_group_name           = var.resource_group_name
  location                      = var.location
  capacity                      = var.capacity
  family                        = "C"
  sku_name                      = var.sku_name
  redis_version                 = var.redis_version
  minimum_tls_version           = "1.2"
  non_ssl_port_enabled          = false
  public_network_access_enabled = false

  redis_configuration {
    maxmemory_policy       = "allkeys-lru"
    authentication_enabled = true
  }

  tags = var.tags
}

resource "azurerm_private_endpoint" "this" {
  name                = "pe-${var.name_prefix}-redis"
  resource_group_name = var.resource_group_name
  location            = var.location
  subnet_id           = var.subnet_id

  private_service_connection {
    name                           = "psc-redis-${var.name_prefix}"
    private_connection_resource_id = azurerm_redis_cache.this.id
    is_manual_connection           = false
    subresource_names              = ["redisCache"]
  }

  private_dns_zone_group {
    name                 = "pdzg-redis"
    private_dns_zone_ids = [var.private_dns_zone_id]
  }

  tags = var.tags
}
