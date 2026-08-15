output "cache_id" {
  description = "ID-ul Redis."
  value       = azurerm_redis_cache.this.id
}

output "cache_name" {
  description = "Numele Redis."
  value       = azurerm_redis_cache.this.name
}

output "hostname" {
  description = "Hostname-ul privat al Redis."
  value       = azurerm_redis_cache.this.hostname
}

output "ssl_port" {
  description = "Portul SSL al Redis."
  value       = azurerm_redis_cache.this.ssl_port
}

output "primary_access_key" {
  description = "Cheia de acces primară."
  value       = azurerm_redis_cache.this.primary_access_key
  sensitive   = true
}

output "connection_string" {
  description = "Connection string-ul primar (SSL)."
  value       = azurerm_redis_cache.this.primary_connection_string
  sensitive   = true
}
