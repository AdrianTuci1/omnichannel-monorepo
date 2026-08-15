output "resource_group_name" {
  description = "Numele resource group-ului."
  value       = azurerm_resource_group.this.name
}

# ---------- AKS ----------

output "aks_cluster_name" {
  description = "Numele clusterului AKS."
  value       = module.aks.cluster_name
}

output "aks_cluster_id" {
  description = "ID-ul clusterului AKS."
  value       = module.aks.cluster_id
}

output "aks_node_resource_group" {
  description = "Resource group-ul auto-generat pentru resursele nodurilor AKS."
  value       = module.aks.node_resource_group
}

output "aks_kube_config_raw" {
  description = "kubeconfig brut (folosit pentru `kubectl` / integrare Helm)."
  value       = module.aks.kube_config_raw
  sensitive   = true
}

output "aks_cluster_identity_principal_id" {
  description = "Principal ID-ul identității managed a clusterului (pentru role assignments, ACR pull etc.)."
  value       = module.aks.cluster_identity_principal_id
}

output "aks_cluster_identity_client_id" {
  description = "Client ID-ul identității managed a clusterului."
  value       = module.aks.cluster_identity_client_id
}

# ---------- PostgreSQL ----------

output "postgres_server_name" {
  description = "Numele serverului PostgreSQL Flexible."
  value       = module.postgres.server_name
}

output "postgres_fqdn" {
  description = "FQDN-ul privat al serverului PostgreSQL."
  value       = module.postgres.fqdn
}

output "postgres_database_name" {
  description = "Numele bazei de date a aplicației."
  value       = module.postgres.database_name
}

output "postgres_admin_login" {
  description = "Login-ul administratorului PostgreSQL."
  value       = module.postgres.administrator_login
}

output "postgres_connection_string" {
  description = "Connection string (Npgsql) pentru store-api."
  value       = module.postgres.connection_string
  sensitive   = true
}

# ---------- Redis ----------

output "redis_hostname" {
  description = "Hostname-ul privat al Redis."
  value       = module.redis.hostname
}

output "redis_ssl_port" {
  description = "Portul SSL al Redis."
  value       = module.redis.ssl_port
}

output "redis_connection_string" {
  description = "Connection string-ul primar al Redis (SSL)."
  value       = module.redis.connection_string
  sensitive   = true
}

# ---------- Service Bus ----------

output "service_bus_namespace" {
  description = "Numele namespace-ului Service Bus."
  value       = module.service_bus.namespace_name
}

output "service_bus_endpoint" {
  description = "Endpoint-ul Service Bus."
  value       = module.service_bus.endpoint
}

output "service_bus_queue_names" {
  description = "Lista cozilor provisionate."
  value       = module.service_bus.queue_names
}

output "service_bus_connection_string" {
  description = "Connection string-ul regulii `store-api` (Listen + Send)."
  value       = module.service_bus.store_api_primary_connection_string
  sensitive   = true
}
