package com.omnichannel.store.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey

/** Cache-ul offline al unui produs, mapează 1:1 pe `ProductResponse` din backend. */
@Entity(tableName = "products")
data class ProductEntity(
    @PrimaryKey val id: String,
    val sku: String,
    val name: String,
    val description: String?,
    val priceAmount: Double,
    val priceCurrency: String,
    val categoryId: String,
    val isActive: Boolean,
    val createdAt: String
)
