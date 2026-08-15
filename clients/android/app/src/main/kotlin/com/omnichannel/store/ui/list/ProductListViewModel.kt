package com.omnichannel.store.ui.list

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.omnichannel.store.data.repository.CategoryRepository
import com.omnichannel.store.data.repository.ProductRepository
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class ProductListItem(
    val id: String,
    val sku: String,
    val name: String,
    val description: String?,
    val priceAmount: Double,
    val priceCurrency: String,
    val categoryName: String?,
    val isActive: Boolean
)

data class ProductListUiState(
    val isLoading: Boolean = true,
    val products: List<ProductListItem> = emptyList(),
    val isOffline: Boolean = false
)

class ProductListViewModel(
    private val productRepository: ProductRepository,
    private val categoryRepository: CategoryRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(ProductListUiState())
    val uiState: StateFlow<ProductListUiState> = _uiState.asStateFlow()

    init {
        observeData()
        refresh()
    }

    private fun observeData() {
        viewModelScope.launch {
            combine(
                productRepository.observeProducts(),
                categoryRepository.observeCategories()
            ) { products, categories ->
                val categoryNames = categories.associate { it.id to it.name }
                products.map { product ->
                    ProductListItem(
                        id = product.id,
                        sku = product.sku,
                        name = product.name,
                        description = product.description,
                        priceAmount = product.priceAmount,
                        priceCurrency = product.priceCurrency,
                        categoryName = categoryNames[product.categoryId],
                        isActive = product.isActive
                    )
                }
            }.collect { items ->
                _uiState.update { it.copy(products = items) }
            }
        }
    }

    fun refresh() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = it.products.isEmpty(), isOffline = false) }
            try {
                coroutineScope {
                    launch { categoryRepository.refreshCategories() }
                    launch { productRepository.refreshProducts() }
                }
                _uiState.update { it.copy(isLoading = false) }
            } catch (e: Exception) {
                _uiState.update { it.copy(isLoading = false, isOffline = true) }
            }
        }
    }
}
