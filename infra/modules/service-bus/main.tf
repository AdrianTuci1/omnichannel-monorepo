resource "azurerm_servicebus_namespace" "this" {
  name                = "sb-${var.name_prefix}-${var.unique_suffix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = var.sku
  minimum_tls_version = "1.2"

  public_network_access_enabled = false
  local_auth_enabled            = true

  tags = var.tags
}

resource "azurerm_servicebus_queue" "this" {
  for_each = toset(var.queues)

  name         = each.key
  namespace_id = azurerm_servicebus_namespace.this.id

  partitioning_enabled                 = true
  max_delivery_count                   = 10
  default_message_ttl                  = "P14D"
  lock_duration                        = "PT1M"
  dead_lettering_on_message_expiration = true
}

# Regulă de acces dedicată pentru store-api (Listen + Send, fără Manage).
resource "azurerm_servicebus_namespace_authorization_rule" "store_api" {
  name         = "store-api"
  namespace_id = azurerm_servicebus_namespace.this.id

  listen = true
  send   = true
  manage = false
}

resource "azurerm_private_endpoint" "this" {
  name                = "pe-${var.name_prefix}-servicebus"
  resource_group_name = var.resource_group_name
  location            = var.location
  subnet_id           = var.subnet_id

  private_service_connection {
    name                           = "psc-servicebus-${var.name_prefix}"
    private_connection_resource_id = azurerm_servicebus_namespace.this.id
    is_manual_connection           = false
    subresource_names              = ["namespace"]
  }

  private_dns_zone_group {
    name                 = "pdzg-servicebus"
    private_dns_zone_ids = [var.private_dns_zone_id]
  }

  tags = var.tags
}
