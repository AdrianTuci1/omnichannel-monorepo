// Client fetch pentru StoreApi (apps/store-api). Wire-format: JSON camelCase,
// conform Contracts.cs (milestone m1).

const DEFAULT_BASE_URL = "http://localhost:5000";
const STORAGE_KEY = "pos.apiBaseUrl";

function trimTrailingSlash(url) {
  return url.replace(/\/+$/, "");
}

export function resolveBaseUrl() {
  const runtime = localStorage.getItem(STORAGE_KEY);
  if (runtime && runtime.trim()) return trimTrailingSlash(runtime.trim());

  const env = import.meta.env.VITE_API_BASE_URL;
  if (env && env.trim()) return trimTrailingSlash(env.trim());

  return DEFAULT_BASE_URL;
}

export function setBaseUrl(url) {
  localStorage.setItem(STORAGE_KEY, url);
}

export class ApiError extends Error {
  constructor(status, message) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

export function errorMessage(err) {
  if (err instanceof Error) return err.message;
  return String(err);
}

async function request(path, options = {}) {
  const method = options.method ?? "GET";
  const res = await fetch(`${resolveBaseUrl()}${path}`, {
    headers: { Accept: "application/json", "Content-Type": "application/json" },
    ...options,
  });

  if (!res.ok) {
    let detail = `${method} ${path} → HTTP ${res.status}`;
    const body = await res.text().catch(() => "");
    if (body) detail += `: ${body}`;
    throw new ApiError(res.status, detail);
  }

  if (res.status === 204) return null;

  const text = await res.text();
  return text ? JSON.parse(text) : null;
}

export const api = {
  health: () => request("/health"),
  listProducts: () => request("/products"),
  createProduct: (payload) =>
    request("/products", { method: "POST", body: JSON.stringify(payload) }),
  listCustomers: () => request("/customers"),
  createCustomer: (payload) =>
    request("/customers", { method: "POST", body: JSON.stringify(payload) }),
  listOrders: () => request("/orders"),
  createOrder: (payload) =>
    request("/orders", { method: "POST", body: JSON.stringify(payload) }),
  deleteOrder: (id) => request(`/orders/${id}`, { method: "DELETE" }),
};
