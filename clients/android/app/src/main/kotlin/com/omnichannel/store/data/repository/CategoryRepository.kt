package com.omnichannel.store.data.repository

import com.omnichannel.store.data.local.CategoryDao
import com.omnichannel.store.data.local.CategoryEntity
import com.omnichannel.store.data.local.toEntity
import com.omnichannel.store.data.remote.StoreApi
import kotlinx.coroutines.flow.Flow

class CategoryRepository(
    private val api: StoreApi,
    private val categoryDao: CategoryDao
) {

    fun observeCategories(): Flow<List<CategoryEntity>> = categoryDao.observeAll()

    suspend fun refreshCategories() {
        val categories = api.getCategories()
        categoryDao.upsertAll(categories.map { it.toEntity() })
    }
}
