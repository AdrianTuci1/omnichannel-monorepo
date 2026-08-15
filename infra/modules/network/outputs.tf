output "vnet_id" {
  description = "ID-ul VNet-ului."
  value       = azurerm_virtual_network.this.id
}

output "aks_subnet_id" {
  description = "ID-ul subnet-ului AKS."
  value       = azurerm_subnet.aks.id
}

output "postgres_subnet_id" {
  description = "ID-ul subnet-ului delegat PostgreSQL."
  value       = azurerm_subnet.postgres.id
}

output "pe_subnet_id" {
  description = "ID-ul subnet-ului pentru private endpoints."
  value       = azurerm_subnet.private_endpoints.id
}

output "postgres_private_dns_zone_id" {
  description = "ID-ul zonei DNS private PostgreSQL."
  value       = azurerm_private_dns_zone.postgres.id
}

output "redis_private_dns_zone_id" {
  description = "ID-ul zonei DNS private Redis."
  value       = azurerm_private_dns_zone.redis.id
}

output "servicebus_private_dns_zone_id" {
  description = "ID-ul zonei DNS private Service Bus."
  value       = azurerm_private_dns_zone.servicebus.id
}
