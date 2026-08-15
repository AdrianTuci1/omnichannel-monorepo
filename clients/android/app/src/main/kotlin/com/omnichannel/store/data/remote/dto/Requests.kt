package com.omnichannel.store.data.remote.dto

import kotlinx.serialization.Serializable

/** Corpuri de cerere pentru rutele POST/PUT din apps/store-api. */

@Serializable
data class CreateCategoryRequest(
    val name: String,
    val slug: String? = null,
    val description: String? = null,
    val parentId: String? = null
)

@Serializable
data class CreateCustomerRequest(
    val email: String,
    val firstName: String,
    val lastName: String,
    val phone: String? = null
)

@Serializable
data class CreateProductRequest(
    val sku: String,
    val name: String,
    val priceAmount: Double,
    val priceCurrency: String,
    val description: String? = null,
    val categoryId: String? = null
)

@Serializable
data class UpdateProductRequest(
    val name: String,
    val priceAmount: Double,
    val priceCurrency: String,
    val categoryId: String,
    val description: String? = null
)

@Serializable
data class CreateOrderLineRequest(
    val productId: String,
    val quantity: Int
)

@Serializable
data class CreateOrderRequest(
    val customerId: String,
    val currency: String? = "USD",
    val notes: String? = null,
    val lines: List<CreateOrderLineRequest>? = null
)
