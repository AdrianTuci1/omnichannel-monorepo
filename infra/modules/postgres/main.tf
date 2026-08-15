resource "azurerm_postgresql_flexible_server" "this" {
  name                = "psql-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location

  version               = var.postgres_version
  sku_name              = var.sku_name
  storage_mb            = var.storage_mb
  auto_grow_enabled     = true
  backup_retention_days = var.backup_retention_days

  administrator_login    = var.administrator_login
  administrator_password = var.administrator_password

  # Zona primară fixă; HA zone-redundant adaugă un standby în zona 2.
  zone = "1"

  dynamic "high_availability" {
    for_each = var.high_availability ? [1] : []
    content {
      mode                      = "ZoneRedundant"
      standby_availability_zone = "2"
    }
  }

  # Acces exclusiv privat prin VNet injection.
  delegated_subnet_id = var.subnet_id
  private_dns_zone_id = var.private_dns_zone_id

  public_network_access_enabled = false

  tags = var.tags
}

resource "azurerm_postgresql_flexible_server_database" "this" {
  name      = var.database_name
  server_id = azurerm_postgresql_flexible_server.this.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}

# Activează extensiile necesare backend-ului store-api:
#   - vector  -> pgvector (ProductEmbedding.Embedding, tip vector(384))
#   - pg_trgm -> căutare text (trigram), btrieve_gin pentru indexare.
resource "azurerm_postgresql_flexible_server_configuration" "extensions" {
  name      = "azure.extensions"
  server_id = azurerm_postgresql_flexible_server.this.id
  value     = "VECTOR,PG_TRGM,BTREE_GIN"
}

locals {
  connection_string = format(
    "Host=%s;Port=5432;Database=%s;Username=%s;Password=%s;SslMode=Require;",
    azurerm_postgresql_flexible_server.this.fqdn,
    azurerm_postgresql_flexible_server_database.this.name,
    var.administrator_login,
    var.administrator_password,
  )
}
