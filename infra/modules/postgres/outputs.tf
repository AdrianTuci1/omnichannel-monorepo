output "server_id" {
  description = "ID-ul serverului PostgreSQL Flexible."
  value       = azurerm_postgresql_flexible_server.this.id
}

output "server_name" {
  description = "Numele serverului PostgreSQL Flexible."
  value       = azurerm_postgresql_flexible_server.this.name
}

output "fqdn" {
  description = "FQDN-ul privat al serverului PostgreSQL."
  value       = azurerm_postgresql_flexible_server.this.fqdn
}

output "database_name" {
  description = "Numele bazei de date aplicației."
  value       = azurerm_postgresql_flexible_server_database.this.name
}

output "administrator_login" {
  description = "Login-ul administratorului PostgreSQL."
  value       = azurerm_postgresql_flexible_server.this.administrator_login
}

output "administrator_password" {
  description = "Parola administratorului PostgreSQL."
  value       = var.administrator_password
  sensitive   = true
}

output "connection_string" {
  description = "Connection string Npgsql pentru store-api."
  value       = local.connection_string
  sensitive   = true
}
