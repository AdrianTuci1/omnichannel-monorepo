package com.omnichannel.store.ui.detail

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Warning
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.omnichannel.store.R
import com.omnichannel.store.StoreApplication
import com.omnichannel.store.ui.StoreViewModelFactory
import com.omnichannel.store.ui.components.DetailField
import com.omnichannel.store.ui.components.EmptyState
import com.omnichannel.store.ui.components.LoadingState
import com.omnichannel.store.ui.components.OfflineBanner
import com.omnichannel.store.ui.components.OfflineErrorState
import com.omnichannel.store.ui.components.StatusBadge
import com.omnichannel.store.util.DateFormatter
import com.omnichannel.store.util.MoneyFormatter

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ProductDetailScreen(productId: String, onBack: () -> Unit) {
    val app = LocalContext.current.applicationContext as StoreApplication
    val viewModel: ProductDetailViewModel = viewModel(
        factory = StoreViewModelFactory(app.container, productId)
    )
    val state by viewModel.uiState.collectAsStateWithLifecycle()

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(R.string.product_detail_title)) },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(
                            imageVector = Icons.AutoMirrored.Filled.ArrowBack,
                            contentDescription = stringResource(R.string.back)
                        )
                    }
                },
                actions = {
                    if (state.isOffline) {
                        Icon(
                            imageVector = Icons.Filled.Warning,
                            contentDescription = stringResource(R.string.offline_banner),
                            tint = MaterialTheme.colorScheme.error
                        )
                    }
                    IconButton(onClick = viewModel::refresh) {
                        Icon(
                            imageVector = Icons.Filled.Refresh,
                            contentDescription = stringResource(R.string.refresh)
                        )
                    }
                }
            )
        }
    ) { innerPadding ->
        val modifier = Modifier
            .fillMaxSize()
            .padding(innerPadding)

        val product = state.product
        when {
            state.isLoading && product == null -> LoadingState(modifier)
            product != null -> ProductDetailContent(product = product, isOffline = state.isOffline, modifier = modifier)
            state.isOffline -> OfflineErrorState(onRetry = viewModel::refresh, modifier = modifier)
            else -> EmptyState(text = stringResource(R.string.error_product_not_found), modifier = modifier)
        }
    }
}

@Composable
private fun ProductDetailContent(
    product: ProductDetail,
    isOffline: Boolean,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        if (isOffline) {
            OfflineBanner()
        }
        Text(text = product.name, style = MaterialTheme.typography.headlineSmall)
        Text(
            text = MoneyFormatter.format(product.priceAmount, product.priceCurrency),
            style = MaterialTheme.typography.headlineMedium,
            color = MaterialTheme.colorScheme.primary
        )
        Row(
            horizontalArrangement = Arrangement.spacedBy(12.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            StatusBadge(isActive = product.isActive)
            Text(
                text = product.categoryName ?: stringResource(R.string.uncategorized),
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        HorizontalDivider()
        DetailField(stringResource(R.string.label_sku), product.sku)
        DetailField(
            stringResource(R.string.label_category),
            product.categoryName ?: stringResource(R.string.uncategorized)
        )
        DetailField(
            stringResource(R.string.label_description),
            product.description?.takeIf { it.isNotBlank() } ?: stringResource(R.string.no_description)
        )
        DetailField(stringResource(R.string.label_created_at), DateFormatter.formatIso(product.createdAt))
        DetailField(
            stringResource(R.string.label_status),
            if (product.isActive) stringResource(R.string.status_active) else stringResource(R.string.status_inactive)
        )
        DetailField(stringResource(R.string.label_id), product.id)
    }
}
