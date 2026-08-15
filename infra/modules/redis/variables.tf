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
  description = "ID-ul zonei DNS private Redis."
  type        = string
}

variable "unique_suffix" {
  description = "Sufix unic global pentru numele Redis (numele trebuie să fie unic la nivel Azure)."
  type        = string
}

variable "sku_name" {
  description = "Tier-ul Azure Cache for Redis."
  type        = string
  default     = "Standard"
}

variable "capacity" {
  description = "Capacitatea Redis."
  type        = number
  default     = 1
}

variable "redis_version" {
  description = "Versiunea Redis."
  type        = string
  default     = "6"
}

variable "tags" {
  description = "Tag-uri aplicate resurselor."
  type        = map(string)
  default     = {}
}
