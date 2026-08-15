resource "azurerm_log_analytics_workspace" "this" {
  name                = "log-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = var.tags
}

resource "azurerm_user_assigned_identity" "this" {
  name                = "id-aks-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  tags                = var.tags
}

# Azure CNI necesită Network Contributor pe subnet-ul nodurilor.
resource "azurerm_role_assignment" "network_contributor" {
  scope                = var.aks_subnet_id
  role_definition_name = "Network Contributor"
  principal_id         = azurerm_user_assigned_identity.this.principal_id
}

# Monitorizare (Container Insights / metrics).
resource "azurerm_role_assignment" "monitoring_metrics_publisher" {
  scope                = azurerm_log_analytics_workspace.this.id
  role_definition_name = "Monitoring Metrics Publisher"
  principal_id         = azurerm_user_assigned_identity.this.principal_id
}

resource "azurerm_kubernetes_cluster" "this" {
  name                = "aks-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  dns_prefix          = "aks-${var.name_prefix}"
  kubernetes_version  = var.kubernetes_version
  sku_tier            = "Standard"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.this.id]
  }

  kubelet_identity {
    client_id                 = azurerm_user_assigned_identity.this.client_id
    object_id                 = azurerm_user_assigned_identity.this.principal_id
    user_assigned_identity_id = azurerm_user_assigned_identity.this.id
  }

  default_node_pool {
    name                         = "system"
    vm_size                      = var.system_node_size
    zones                        = [1, 2, 3]
    auto_scaling_enabled         = true
    min_count                    = var.system_node_min
    max_count                    = var.system_node_max
    node_count                   = var.system_node_count
    max_pods                     = 30
    os_disk_size_gb              = 64
    vnet_subnet_id               = var.aks_subnet_id
    only_critical_addons_enabled = true
    tags                         = var.tags
  }

  network_profile {
    network_plugin    = "azure"
    network_policy    = "azure"
    network_mode      = "transparent"
    service_cidr      = "10.240.0.0/16"
    dns_service_ip    = "10.240.0.10"
    outbound_type     = "loadBalancer"
    load_balancer_sku = "standard"
  }

  oms_agent {
    log_analytics_workspace_id      = azurerm_log_analytics_workspace.this.id
    msi_auth_for_monitoring_enabled = true
  }

  # Asigură ordinea: identitatea primește rolurile înainte de crearea clusterului,
  # evitând erorile tranzitorii de permisiuni la Azure CNI / monitoring.
  depends_on = [
    azurerm_role_assignment.network_contributor,
    azurerm_role_assignment.monitoring_metrics_publisher,
  ]

  tags = var.tags
}

resource "azurerm_kubernetes_cluster_node_pool" "user" {
  name                  = "user"
  kubernetes_cluster_id = azurerm_kubernetes_cluster.this.id
  vm_size               = var.user_node_size
  zones                 = [1, 2, 3]
  auto_scaling_enabled  = true
  min_count             = var.user_node_min
  max_count             = var.user_node_max
  node_count            = var.user_node_count
  max_pods              = 30
  os_disk_size_gb       = 64
  vnet_subnet_id        = var.aks_subnet_id
  tags                  = var.tags
}
