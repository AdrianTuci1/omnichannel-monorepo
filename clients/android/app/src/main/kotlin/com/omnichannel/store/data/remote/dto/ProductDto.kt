package com.omnichannel.store.data.remote.dto

import kotlinx.serialization.Serializable

/**
 * Reflectă `ProductResponse` din apps/store-api (Contracts.cs).
 * GUID-urile și DateTime sunt serializate ca string de către backend; decimal -> Double.
 */
@Serializable
data class ProductDto(
    val id: String,
    val sku: String,
    val name: String,
    val description: String? = null,
    val priceAmount: Double,
    val priceCurrency: String,
    val categoryId: String,
    val isActive: Boolean,
    val createdAt: String
)
