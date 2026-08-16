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

export interface ProductListResponse {
  items: ProductResponse[];
  total: number;
  page: number;
  pageSize: number;
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

export type PaymentMethod = "CashOnDelivery" | "Card" | "BankTransfer";

export type PaymentStatus = "Pending" | "Paid" | "Failed" | "Refunded";

export interface OrderResponse {
  id: string;
  orderNumber: string;
  customerId: string;
  status: OrderStatus;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
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

// ---------- Auth ----------
export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface RegisterResponse {
  userId: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface LogoutRequest {
  refreshToken: string;
}

// ---------- Cart ----------
export interface CartItem {
  productId: string;
  productName: string;
  quantity: number;
  unitPriceAmount: number;
  unitPriceCurrency: string;
}

export interface AddCartItemRequest {
  productId: string;
  quantity: number;
}

export interface UpdateCartItemRequest {
  quantity: number;
}

// ---------- Orders (create) ----------
export interface CreateOrderLineRequest {
  productId: string;
  quantity: number;
}

export interface CreateOrderRequest {
  customerId: string;
  currency?: string;
  notes?: string | null;
  paymentMethod?: PaymentMethod;
  lines?: CreateOrderLineRequest[];
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

export function paymentMethodLabel(method: PaymentMethod): string {
  switch (method) {
    case "CashOnDelivery":
      return "Ramburs";
    case "Card":
      return "Card";
    case "BankTransfer":
      return "Transfer bancar";
    default:
      return method;
  }
}

export function paymentStatusLabel(status: PaymentStatus): string {
  switch (status) {
    case "Pending":
      return "În așteptare";
    case "Paid":
      return "Plătit";
    case "Failed":
      return "Eșuat";
    case "Refunded":
      return "Rambursat";
    default:
      return status;
  }
}
