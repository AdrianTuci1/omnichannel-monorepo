// Gestionarea tokenilor de autentificare și a identității clientului în localStorage.
// Modul pur de browser: accesul la localStorage este protejat cu `typeof window`
// pentru a rămâne sigur la server-side rendering.

import type { AuthResponse } from "@/lib/types";

export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";

const ACCESS_TOKEN_KEY = "omnichannel.accessToken";
const REFRESH_TOKEN_KEY = "omnichannel.refreshToken";
const CUSTOMER_ID_KEY = "omnichannel.customerId";

function storage(): Storage | null {
  return typeof window === "undefined" ? null : window.localStorage;
}

export function getAccessToken(): string | null {
  return storage()?.getItem(ACCESS_TOKEN_KEY) ?? null;
}

export function getRefreshToken(): string | null {
  return storage()?.getItem(REFRESH_TOKEN_KEY) ?? null;
}

export function getCustomerId(): string | null {
  return storage()?.getItem(CUSTOMER_ID_KEY) ?? null;
}

export function saveCustomerId(customerId: string): void {
  storage()?.setItem(CUSTOMER_ID_KEY, customerId);
}

export function saveTokens(auth: AuthResponse): void {
  const s = storage();
  if (!s) return;
  s.setItem(ACCESS_TOKEN_KEY, auth.accessToken);
  s.setItem(REFRESH_TOKEN_KEY, auth.refreshToken);
}

export function clearTokens(): void {
  storage()?.removeItem(ACCESS_TOKEN_KEY);
  storage()?.removeItem(REFRESH_TOKEN_KEY);
}

export function isAuthenticated(): boolean {
  return getAccessToken() !== null;
}

// Deconectează utilizatorul: șterge tokenii locali și, dacă există un refresh
// token, îl invalidează la server (best-effort, erorile de rețea sunt ignorate).
export async function logout(): Promise<void> {
  const refreshToken = getRefreshToken();
  clearTokens();
  if (!refreshToken) return;

  try {
    await fetch(`${API_BASE_URL}/auth/logout`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
    });
  } catch {
    // Tokenul local a fost deja șters; eșecul de rețea nu blochează logout-ul.
  }
}
