"use client";

import { useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";

import { apiPost } from "@/lib/api";
import { saveTokens } from "@/lib/auth";
import type { AuthResponse } from "@/lib/types";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";

const inputClass =
  "w-full rounded-md border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-1 focus:ring-neutral-400";
const labelClass = "text-sm font-medium text-neutral-700";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      const auth = await apiPost<AuthResponse>("/auth/login", {
        email: email.trim(),
        password,
      });
      saveTokens(auth);
      router.push("/");
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Eroare la autentificare."
      );
      setSubmitting(false);
    }
  }

  return (
    <div className="mx-auto flex max-w-md flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold tracking-tight text-neutral-900">
          Autentificare
        </h1>
        <p className="text-sm text-neutral-600">
          Intră în cont pentru a folosi coșul și a plasa comenzi.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Conectează-te</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="flex flex-col gap-1">
              <label htmlFor="email" className={labelClass}>
                Email
              </label>
              <input
                id="email"
                type="email"
                className={inputClass}
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="email"
                placeholder="adresa@exemplu.ro"
              />
            </div>

            <div className="flex flex-col gap-1">
              <label htmlFor="password" className={labelClass}>
                Parolă
              </label>
              <input
                id="password"
                type="password"
                className={inputClass}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="current-password"
                placeholder="••••••••"
              />
            </div>

            {error ? (
              <p className="rounded-md border border-neutral-300 px-3 py-2 text-sm text-neutral-900">
                {error}
              </p>
            ) : null}

            <Button type="submit" disabled={submitting}>
              {submitting ? "Se autentifică…" : "Autentificare"}
            </Button>

            <p className="text-sm text-neutral-600">
              Nu ai cont?{" "}
              <Link
                href="/register"
                className="font-medium text-neutral-900 underline underline-offset-2 hover:text-neutral-700"
              >
                Înregistrează-te
              </Link>
            </p>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
