package com.omnichannel.store.util

import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter

object DateFormatter {

    private val formatter: DateTimeFormatter = DateTimeFormatter
        .ofPattern("dd MMM yyyy, HH:mm")
        .withZone(ZoneId.systemDefault())

    /** Formatează un timestamp ISO-8601 (ex. "2026-08-15T20:55:00.1234567Z") în fusul local. */
    fun formatIso(iso: String): String = try {
        formatter.format(Instant.parse(iso))
    } catch (e: Exception) {
        iso
    }
}
