variable "name_prefix" {
  description = "Prefix pentru numele resurselor."
  type        = string
}

variable "resource_group_name" {
  description = "Numele resource group-ului."
  type        = string
}

variable "location" {
  description = "Regiunea Azure."
  type        = string
}

variable "vnet_address_space" {
  description = "Spațiul de adrese al VNet-ului."
  type        = list(string)
}

variable "aks_subnet_prefix" {
  description = "Prefixul subnet-ului AKS."
  type        = string
}

variable "postgres_subnet_prefix" {
  description = "Prefixul subnet-ului delegat PostgreSQL."
  type        = string
}

variable "pe_subnet_prefix" {
  description = "Prefixul subnet-ului pentru private endpoints."
  type        = string
}

variable "tags" {
  description = "Tag-uri aplicate resurselor."
  type        = map(string)
  default     = {}
}
