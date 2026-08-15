package com.omnichannel.store.data.remote

import com.omnichannel.store.data.remote.dto.CategoryDto
import com.omnichannel.store.data.remote.dto.CreateCategoryRequest
import com.omnichannel.store.data.remote.dto.CreateCustomerRequest
import com.omnichannel.store.data.remote.dto.CreateOrderRequest
import com.omnichannel.store.data.remote.dto.CreateProductRequest
import com.omnichannel.store.data.remote.dto.CustomerDto
import com.omnichannel.store.data.remote.dto.HealthDto
import com.omnichannel.store.data.remote.dto.OrderDto
import com.omnichannel.store.data.remote.dto.ProductDto
import com.omnichannel.store.data.remote.dto.UpdateProductRequest
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

/**
 * Client Retrofit care acoperă toate cele 18 rute ale apps/store-api
 * (vezi apps/store-api/src/StoreApi.Api/Program.cs).
 */
interface StoreApi {

    @GET("health")
    suspend fun health(): HealthDto

    // ---------- Categorii ----------
    @GET("categories")
    suspend fun getCategories(): List<CategoryDto>

    @GET("categories/{id}")
    suspend fun getCategory(@Path("id") id: String): CategoryDto

    @POST("categories")
    suspend fun createCategory(@Body request: CreateCategoryRequest): CategoryDto

    @DELETE("categories/{id}")
    suspend fun deleteCategory(@Path("id") id: String): Response<Unit>

    // ---------- Clienți ----------
    @GET("customers")
    suspend fun getCustomers(): List<CustomerDto>

    @GET("customers/{id}")
    suspend fun getCustomer(@Path("id") id: String): CustomerDto

    @POST("customers")
    suspend fun createCustomer(@Body request: CreateCustomerRequest): CustomerDto

    @DELETE("customers/{id}")
    suspend fun deleteCustomer(@Path("id") id: String): Response<Unit>

    // ---------- Produse ----------
    @GET("products")
    suspend fun getProducts(): List<ProductDto>

    @GET("products/{id}")
    suspend fun getProduct(@Path("id") id: String): ProductDto

    @POST("products")
    suspend fun createProduct(@Body request: CreateProductRequest): ProductDto

    @PUT("products/{id}")
    suspend fun updateProduct(@Path("id") id: String, @Body request: UpdateProductRequest): ProductDto

    @DELETE("products/{id}")
    suspend fun deleteProduct(@Path("id") id: String): Response<Unit>

    // ---------- Comenzi ----------
    @GET("orders")
    suspend fun getOrders(): List<OrderDto>

    @GET("orders/{id}")
    suspend fun getOrder(@Path("id") id: String): OrderDto

    @POST("orders")
    suspend fun createOrder(@Body request: CreateOrderRequest): OrderDto

    @DELETE("orders/{id}")
    suspend fun deleteOrder(@Path("id") id: String): Response<Unit>
}
