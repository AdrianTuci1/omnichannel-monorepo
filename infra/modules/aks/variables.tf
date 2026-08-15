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

variable "aks_subnet_id" {
  description = "ID-ul subnet-ului pentru nodurile AKS."
  type        = string
}

variable "kubernetes_version" {
  description = "Versiunea Kubernetes."
  type        = string
}

variable "system_node_size" {
  description = "VM size pentru pool-ul de sistem."
  type        = string
}

variable "system_node_count" {
  description = "Numărul inițial de noduri (pool sistem)."
  type        = number
}

variable "system_node_min" {
  description = "Numărul minim de noduri (pool sistem)."
  type        = number
}

variable "system_node_max" {
  description = "Numărul maxim de noduri (pool sistem)."
  type        = number
}

variable "user_node_size" {
  description = "VM size pentru pool-ul de aplicație."
  type        = string
}

variable "user_node_count" {
  description = "Numărul inițial de noduri (pool aplicație)."
  type        = number
}

variable "user_node_min" {
  description = "Numărul minim de noduri (pool aplicație)."
  type        = number
}

variable "user_node_max" {
  description = "Numărul maxim de noduri (pool aplicație)."
  type        = number
}

variable "tags" {
  description = "Tag-uri aplicate resurselor."
  type        = map(string)
  default     = {}
}
