// Tipuri și constante care reflectă wire-formatul JSON al StoreApi (camelCase).
//
// Wire-shape (conform Contracts.cs / Program.cs din apps/store-api):
//   ProductResponse  { id, sku, name, description, priceAmount, priceCurrency,
//                      categoryId, isActive, createdAt }
//   CustomerResponse { id, email, firstName, lastName, phone, createdAt }
//   OrderResponse    { id, orderNumber, customerId, status, currency, notes,
//                      totalAmount, totalCurrency, createdAt, lines }
//   OrderLineResponse{ id, productId, productName, quantity, unitPriceAmount,
//                      unitPriceCurrency, lineTotalAmount, lineTotalCurrency }
//   OrderStatus      "Draft" | "Pending" | "Paid" | "Shipped" | "Delivered" | "Cancelled"

export const DEFAULT_CURRENCY = "USD";

export const SUPPORTED_CURRENCIES = ["USD", "EUR", "RON"];

export const ORDER_STATUS = {
  Draft: { label: "Draft", tone: "neutral" },
  Pending: { label: "În așteptare", tone: "pending" },
  Paid: { label: "Plătită", tone: "paid" },
  Shipped: { label: "Expediată", tone: "shipped" },
  Delivered: { label: "Livrată", tone: "delivered" },
  Cancelled: { label: "Anulată", tone: "cancelled" },
};
