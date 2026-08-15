output "namespace_id" {
  description = "ID-ul namespace-ului Service Bus."
  value       = azurerm_servicebus_namespace.this.id
}

output "namespace_name" {
  description = "Numele namespace-ului Service Bus."
  value       = azurerm_servicebus_namespace.this.name
}

output "endpoint" {
  description = "Endpoint-ul Service Bus."
  value       = azurerm_servicebus_namespace.this.endpoint
}

output "queue_names" {
  description = "Lista cozilor provisionate."
  value       = keys(azurerm_servicebus_queue.this)
}

output "store_api_primary_connection_string" {
  description = "Connection string-ul regulii `store-api` (Listen + Send)."
  value       = azurerm_servicebus_namespace_authorization_rule.store_api.primary_connection_string
  sensitive   = true
}

output "default_primary_connection_string" {
  description = "Connection string-ul default (RootManageSharedAccessKey)."
  value       = azurerm_servicebus_namespace.this.default_primary_connection_string
  sensitive   = true
}
