package com.omnichannel.store.data.remote.dto

import kotlinx.serialization.Serializable

/** Reflectă `CustomerResponse` din apps/store-api. */
@Serializable
data class CustomerDto(
    val id: String,
    val email: String,
    val firstName: String,
    val lastName: String,
    val phone: String? = null,
    val createdAt: String
)
