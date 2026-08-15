package com.omnichannel.store.data.repository

import com.omnichannel.store.data.local.ProductDao
import com.omnichannel.store.data.local.ProductEntity
import com.omnichannel.store.data.local.toEntity
import com.omnichannel.store.data.remote.StoreApi
import kotlinx.coroutines.flow.Flow

/**
 * Sursă unică de adevăr pentru produse: citește din Room (cache offline),
 * iar `refresh*` sincronizează cache-ul cu backend-ul.
 */
class ProductRepository(
    private val api: StoreApi,
    private val productDao: ProductDao
) {

    fun observeProducts(): Flow<List<ProductEntity>> = productDao.observeAll()

    fun observeProduct(id: String): Flow<ProductEntity?> = productDao.observeById(id)

    suspend fun refreshProducts() {
        val products = api.getProducts()
        productDao.upsertAll(products.map { it.toEntity() })
    }

    suspend fun refreshProduct(id: String) {
        val product = api.getProduct(id)
        productDao.upsert(product.toEntity())
    }
}
