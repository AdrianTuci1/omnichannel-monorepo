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

variable "subnet_id" {
  description = "ID-ul subnet-ului pentru private endpoint."
  type        = string
}

variable "private_dns_zone_id" {
  description = "ID-ul zonei DNS private Service Bus."
  type        = string
}

variable "unique_suffix" {
  description = "Sufix unic global pentru numele namespace-ului."
  type        = string
}

variable "sku" {
  description = "Tier-ul Service Bus (Basic, Standard, Premium)."
  type        = string
  default     = "Standard"
}

variable "queues" {
  description = "Cozile provisionate."
  type        = list(string)
  default     = []
}

variable "tags" {
  description = "Tag-uri aplicate resurselor."
  type        = map(string)
  default     = {}
}
