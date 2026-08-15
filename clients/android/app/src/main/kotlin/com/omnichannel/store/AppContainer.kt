package com.omnichannel.store

import android.content.Context
import com.omnichannel.store.data.local.StoreDatabase
import com.omnichannel.store.data.remote.ApiClient
import com.omnichannel.store.data.repository.CategoryRepository
import com.omnichannel.store.data.repository.ProductRepository

/**
 * Container manual de dependențe (fără framework de DI): construiește baza de date,
 * clientul API și repository-urile, expuse ca singleton-uri pe durata procesului.
 */
class AppContainer(context: Context) {

    private val database: StoreDatabase = StoreDatabase.getInstance(context)

    val productRepository: ProductRepository =
        ProductRepository(ApiClient.api, database.productDao())

    val categoryRepository: CategoryRepository =
        CategoryRepository(ApiClient.api, database.categoryDao())
}
