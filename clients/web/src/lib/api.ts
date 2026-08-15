// Client fetch pentru StoreApi. Base URL configurabil prin NEXT_PUBLIC_API_BASE_URL,
// default http://localhost:5000 (așa cum expune apps/store-api local).

const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

export async function apiGet<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE_URL}${path}`, {
    method: "GET",
    headers: { Accept: "application/json" },
    cache: "no-store",
  });

  if (!res.ok) {
    throw new ApiError(res.status, `GET ${path} a răspuns ${res.status}`);
  }

  return (await res.json()) as T;
}

export async function apiDelete(path: string): Promise<void> {
  const res = await fetch(`${API_BASE_URL}${path}`, {
    method: "DELETE",
    cache: "no-store",
  });

  if (!res.ok && res.status !== 204) {
    throw new ApiError(res.status, `DELETE ${path} a răspuns ${res.status}`);
  }
}

export async function apiPost<T>(path: string, body: unknown): Promise<T> {
  const res = await fetch(`${API_BASE_URL}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    cache: "no-store",
    body: JSON.stringify(body),
  });

  if (!res.ok) {
    throw new ApiError(res.status, `POST ${path} a răspuns ${res.status}`);
  }

  return (await res.json()) as T;
}

export async function apiPut<T>(path: string, body: unknown): Promise<T> {
  const res = await fetch(`${API_BASE_URL}${path}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    cache: "no-store",
    body: JSON.stringify(body),
  });

  if (!res.ok) {
    throw new ApiError(res.status, `PUT ${path} a răspuns ${res.status}`);
  }

  return (await res.json()) as T;
}

export function apiBaseUrl(): string {
  return API_BASE_URL;
}
