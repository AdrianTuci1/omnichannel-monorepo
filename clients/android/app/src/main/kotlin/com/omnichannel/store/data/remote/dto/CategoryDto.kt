package com.omnichannel.store.data.remote.dto

import kotlinx.serialization.Serializable

/** Reflectă `CategoryResponse` din apps/store-api. */
@Serializable
data class CategoryDto(
    val id: String,
    val name: String,
    val slug: String,
    val description: String? = null,
    val parentId: String? = null
)
