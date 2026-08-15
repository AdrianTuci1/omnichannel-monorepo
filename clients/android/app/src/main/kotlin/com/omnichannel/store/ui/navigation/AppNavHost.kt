package com.omnichannel.store.ui.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.omnichannel.store.ui.detail.ProductDetailScreen
import com.omnichannel.store.ui.list.ProductListScreen

object Routes {
    const val PRODUCTS = "products"
    const val PRODUCT_DETAIL = "product/{productId}"

    fun productDetail(productId: String) = "product/$productId"
}

@Composable
fun AppNavHost() {
    val navController = rememberNavController()

    NavHost(navController = navController, startDestination = Routes.PRODUCTS) {
        composable(Routes.PRODUCTS) {
            ProductListScreen(
                onProductClick = { productId -> navController.navigate(Routes.productDetail(productId)) }
            )
        }
        composable(
            route = Routes.PRODUCT_DETAIL,
            arguments = listOf(navArgument("productId") { type = NavType.StringType })
        ) { backStackEntry ->
            val productId = backStackEntry.arguments?.getString("productId").orEmpty()
            ProductDetailScreen(
                productId = productId,
                onBack = { navController.popBackStack() }
            )
        }
    }
}
