package com.omnichannel.store.data.local

import com.omnichannel.store.data.remote.dto.CategoryDto
import com.omnichannel.store.data.remote.dto.ProductDto

fun ProductDto.toEntity(): ProductEntity = ProductEntity(
    id = id,
    sku = sku,
    name = name,
    description = description,
    priceAmount = priceAmount,
    priceCurrency = priceCurrency,
    categoryId = categoryId,
    isActive = isActive,
    createdAt = createdAt
)

fun CategoryDto.toEntity(): CategoryEntity = CategoryEntity(
    id = id,
    name = name,
    slug = slug,
    description = description,
    parentId = parentId
)
