package com.omnichannel.store.data.remote.dto

import kotlinx.serialization.Serializable

/** Reflectă `OrderResponse` din apps/store-api. `Status` este string (numele enumului OrderStatus). */
@Serializable
data class OrderDto(
    val id: String,
    val orderNumber: String,
    val customerId: String,
    val status: String,
    val currency: String,
    val notes: String? = null,
    val totalAmount: Double,
    val totalCurrency: String,
    val createdAt: String,
    val lines: List<OrderLineDto> = emptyList()
)

/** Reflectă `OrderLineResponse` din apps/store-api. */
@Serializable
data class OrderLineDto(
    val id: String,
    val productId: String,
    val productName: String,
    val quantity: Int,
    val unitPriceAmount: Double,
    val unitPriceCurrency: String,
    val lineTotalAmount: Double,
    val lineTotalCurrency: String
)
