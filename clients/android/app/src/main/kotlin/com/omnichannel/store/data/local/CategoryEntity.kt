package com.omnichannel.store.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey

/** Cache-ul offline al unei categorii, mapează 1:1 pe `CategoryResponse` din backend. */
@Entity(tableName = "categories")
data class CategoryEntity(
    @PrimaryKey val id: String,
    val name: String,
    val slug: String,
    val description: String?,
    val parentId: String?
)
