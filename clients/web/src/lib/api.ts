// Client fetch pentru StoreApi. Base URL configurabil prin NEXT_PUBLIC_API_BASE_URL,
// default http://localhost:5000 (așa cum expune apps/store-api local).
//
// Atașează `Authorization: Bearer <accessToken>` pe toate requesturile când tokenul
// există și, la un răspuns 401 pe un endpoint protejat, încearcă o singură dată
// refresh-ul tokenului înainte de a redirecționa la /login.

import {
  API_BASE_URL,
  clearTokens,
  getAccessToken,
  getRefreshToken,
  saveTokens,
} from "@/lib/auth";
import type { AuthResponse } from "@/lib/types";

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

type HttpMethod = "GET" | "POST" | "PUT" | "DELETE";

function redirectToLogin(): void {
  if (typeof window === "undefined") return;
  if (window.location.pathname === "/login") return;
  window.location.assign("/login");
}

function isAuthPath(path: string): boolean {
  return path.startsWith("/auth/");
}

async function refreshAccessToken(): Promise<boolean> {
  const refreshToken = getRefreshToken();
  if (!refreshToken) return false;

  try {
    const res = await fetch(`${API_BASE_URL}/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify({ refreshToken }),
    });
    if (!res.ok) return false;
    saveTokens((await res.json()) as AuthResponse);
    return true;
  } catch {
    return false;
  }
}

async function request<T>(
  method: HttpMethod,
  path: string,
  body?: unknown
): Promise<T> {
  const doFetch = (): Promise<Response> => {
    const headers: Record<string, string> = { Accept: "application/json" };
    if (body !== undefined) headers["Content-Type"] = "application/json";

    const token = getAccessToken();
    if (token) headers["Authorization"] = `Bearer ${token}`;

    return fetch(`${API_BASE_URL}${path}`, {
      method,
      headers,
      cache: "no-store",
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  };

  let res = await doFetch();

  if (res.status === 401 && !isAuthPath(path)) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      res = await doFetch();
    } else {
      clearTokens();
      redirectToLogin();
      throw new ApiError(401, `${method} ${path} a răspuns 401`);
    }
  }

  if (res.status === 204) {
    return undefined as T;
  }

  if (!res.ok) {
    throw new ApiError(res.status, `${method} ${path} a răspuns ${res.status}`);
  }

  return (await res.json()) as T;
}

export function apiGet<T>(path: string): Promise<T> {
  return request<T>("GET", path);
}

export function apiPost<T>(path: string, body: unknown): Promise<T> {
  return request<T>("POST", path, body);
}

export function apiPut<T>(path: string, body: unknown): Promise<T> {
  return request<T>("PUT", path, body);
}

export function apiDelete(path: string): Promise<void> {
  return request<void>("DELETE", path);
}

export function apiBaseUrl(): string {
  return API_BASE_URL;
}
