// Tipuri TypeScript care reflectă exact wire-formatul JSON al StoreApi (camelCase),
// conform Contracts.cs din apps/store-api (milestone m1).

export interface CategoryResponse {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  parentId: string | null;
}

export interface CustomerResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phone: string | null;
  createdAt: string;
}

export interface ProductResponse {
  id: string;
  sku: string;
  name: string;
  description: string | null;
  priceAmount: number;
  priceCurrency: string;
  categoryId: string;
  isActive: boolean;
  createdAt: string;
}

export interface OrderLineResponse {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPriceAmount: number;
  unitPriceCurrency: string;
  lineTotalAmount: number;
  lineTotalCurrency: string;
}

export type OrderStatus =
  | "Draft"
  | "Pending"
  | "Paid"
  | "Shipped"
  | "Delivered"
  | "Cancelled";

export interface OrderResponse {
  id: string;
  orderNumber: string;
  customerId: string;
  status: OrderStatus;
  currency: string;
  notes: string | null;
  totalAmount: number;
  totalCurrency: string;
  createdAt: string;
  lines: OrderLineResponse[];
}

export interface HealthResponse {
  status: string;
}

export interface Review {
  id: string;
  productId: string;
  customerId: string;
  rating: number;
  title: string;
  comment: string;
  createdAt: string;
}

export interface RelatedProduct {
  productId: string;
  name: string;
  score: number;
}

export interface CreateProductRequest {
  sku: string;
  name: string;
  description: string | null;
  priceAmount: number;
  priceCurrency: string;
  categoryId: string | null;
}

export interface CreateReviewRequest {
  rating: number;
  title: string;
  comment: string;
  customerId: string;
}

export function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency,
  }).format(amount);
}

export function formatDate(iso: string): string {
  return new Date(iso).toLocaleString("ro-RO", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}
