package com.omnichannel.store.ui.detail

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

data class ProductDetail(
    val id: String,
    val sku: String,
    val name: String,
    val description: String?,
    val priceAmount: Double,
    val priceCurrency: String,
    val categoryName: String?,
    val isActive: Boolean,
    val createdAt: String
)

data class ProductDetailUiState(
    val isLoading: Boolean = true,
    val product: ProductDetail? = null,
    val isOffline: Boolean = false
)

class ProductDetailViewModel(
    private val productRepository: ProductRepository,
    private val categoryRepository: CategoryRepository,
    private val productId: String
) : ViewModel() {

    private val _uiState = MutableStateFlow(ProductDetailUiState())
    val uiState: StateFlow<ProductDetailUiState> = _uiState.asStateFlow()

    init {
        observeData()
        refresh()
    }

    private fun observeData() {
        viewModelScope.launch {
            combine(
                productRepository.observeProduct(productId),
                categoryRepository.observeCategories()
            ) { product, categories ->
                if (product == null) {
                    null
                } else {
                    val categoryName = categories.firstOrNull { it.id == product.categoryId }?.name
                    ProductDetail(
                        id = product.id,
                        sku = product.sku,
                        name = product.name,
                        description = product.description,
                        priceAmount = product.priceAmount,
                        priceCurrency = product.priceCurrency,
                        categoryName = categoryName,
                        isActive = product.isActive,
                        createdAt = product.createdAt
                    )
                }
            }.collect { detail ->
                _uiState.update { it.copy(product = detail) }
            }
        }
    }

    fun refresh() {
        viewModelScope.launch {
            _uiState.update { it.copy(isOffline = false) }
            try {
                coroutineScope {
                    launch { categoryRepository.refreshCategories() }
                    launch { productRepository.refreshProduct(productId) }
                }
                _uiState.update { it.copy(isLoading = false) }
            } catch (e: Exception) {
                _uiState.update { it.copy(isLoading = false, isOffline = true) }
            }
        }
    }
}
