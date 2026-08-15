package com.omnichannel.store.ui

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewmodel.CreationExtras
import com.omnichannel.store.AppContainer
import com.omnichannel.store.ui.detail.ProductDetailViewModel
import com.omnichannel.store.ui.list.ProductListViewModel

class StoreViewModelFactory(
    private val container: AppContainer,
    private val productId: String? = null
) : ViewModelProvider.Factory {

    override fun <T : ViewModel> create(modelClass: Class<T>): T =
        create(modelClass, CreationExtras.Empty)

    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>, extras: CreationExtras): T = when {
        modelClass.isAssignableFrom(ProductListViewModel::class.java) ->
            ProductListViewModel(container.productRepository, container.categoryRepository) as T

        modelClass.isAssignableFrom(ProductDetailViewModel::class.java) ->
            ProductDetailViewModel(
                container.productRepository,
                container.categoryRepository,
                checkNotNull(productId) { "productId must be provided for ProductDetailViewModel" }
            ) as T

        else -> throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
    }
}
