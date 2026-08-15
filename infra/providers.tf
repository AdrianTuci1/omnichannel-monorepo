provider "azurerm" {
  features {}

  # Autentificare: az login (CLI) sau variabilele de mediu ARM_SUBSCRIPTION_ID /
  # ARM_TENANT_ID / ARM_CLIENT_ID / ARM_CLIENT_SECRET. Valorile de mai jos sunt
  # opționale — dacă sunt goale, providerul folosește mediul/CLI-ul.
  subscription_id = var.subscription_id != "" ? var.subscription_id : null
  tenant_id       = var.tenant_id != "" ? var.tenant_id : null
}
