package com.omnichannel.store.data.remote.dto

import kotlinx.serialization.Serializable

/** Răspunsul `GET /health`. */
@Serializable
data class HealthDto(
    val status: String
)
