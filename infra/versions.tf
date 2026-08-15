terraform {
  required_version = ">= 1.8.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # Backend remote (Azure Storage). Configurarea se furnizează la `terraform init`
  # prin -backend-config (vezi README). Pentru validare locală: terraform init -backend=false.
  backend "azurerm" {}
}
