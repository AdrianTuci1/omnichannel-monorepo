output "cluster_id" {
  description = "ID-ul clusterului AKS."
  value       = azurerm_kubernetes_cluster.this.id
}

output "cluster_name" {
  description = "Numele clusterului AKS."
  value       = azurerm_kubernetes_cluster.this.name
}

output "node_resource_group" {
  description = "Resource group-ul auto-generat pentru resursele nodurilor."
  value       = azurerm_kubernetes_cluster.this.node_resource_group
}

output "kube_config_raw" {
  description = "kubeconfig brut."
  value       = azurerm_kubernetes_cluster.this.kube_config_raw
  sensitive   = true
}

output "cluster_identity_principal_id" {
  description = "Principal ID-ul identității managed."
  value       = azurerm_user_assigned_identity.this.principal_id
}

output "cluster_identity_client_id" {
  description = "Client ID-ul identității managed."
  value       = azurerm_user_assigned_identity.this.client_id
}

output "log_analytics_workspace_id" {
  description = "ID-ul workspace-ului Log Analytics."
  value       = azurerm_log_analytics_workspace.this.id
}
