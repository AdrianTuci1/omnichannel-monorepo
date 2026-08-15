package com.omnichannel.store.util

import java.text.NumberFormat
import java.util.Currency
import java.util.Locale

object MoneyFormatter {

    /**
     * Formatează o sumă + cod de monedă folosind simbolul valutar local.
     * Exemplu: (12.34, "USD") -> "$12.34" (locale en_US) / "12,34 USD" (locale ro_RO).
     * Pentru coduri de monedă necunoscute, cade pe "sumă COD".
     */
    fun format(amount: Double, currency: String): String = try {
        val currencyCode = currency.trim().uppercase(Locale.ROOT)
        val currencyInstance = Currency.getInstance(currencyCode)
        val format = NumberFormat.getCurrencyInstance(Locale.getDefault()).apply {
            this.currency = currencyInstance
            minimumFractionDigits = currencyInstance.defaultFractionDigits
            maximumFractionDigits = currencyInstance.defaultFractionDigits
        }
        format.format(amount)
    } catch (e: Exception) {
        "$amount $currency"
    }
}
