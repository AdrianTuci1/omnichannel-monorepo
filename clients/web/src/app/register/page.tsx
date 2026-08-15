"use client";

import { useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";

import { apiPost } from "@/lib/api";
import type { RegisterRequest, RegisterResponse } from "@/lib/types";
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

export default function RegisterPage() {
  const router = useRouter();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    const payload: RegisterRequest = {
      email: email.trim(),
      password,
      firstName: firstName.trim(),
      lastName: lastName.trim(),
    };

    try {
      await apiPost<RegisterResponse>("/auth/register", payload);
      router.push("/login");
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Eroare la înregistrare."
      );
      setSubmitting(false);
    }
  }

  return (
    <div className="mx-auto flex max-w-md flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold tracking-tight text-neutral-900">
          Înregistrare
        </h1>
        <p className="text-sm text-neutral-600">
          Creează un cont pentru autentificare și coșul de cumpărături.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Cont nou</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="flex flex-col gap-1">
                <label htmlFor="firstName" className={labelClass}>
                  Prenume
                </label>
                <input
                  id="firstName"
                  className={inputClass}
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  required
                  autoComplete="given-name"
                  placeholder="Ion"
                />
              </div>
              <div className="flex flex-col gap-1">
                <label htmlFor="lastName" className={labelClass}>
                  Nume
                </label>
                <input
                  id="lastName"
                  className={inputClass}
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  required
                  autoComplete="family-name"
                  placeholder="Popescu"
                />
              </div>
            </div>

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
                minLength={6}
                autoComplete="new-password"
                placeholder="Minim 6 caractere"
              />
            </div>

            {error ? (
              <p className="rounded-md border border-neutral-300 px-3 py-2 text-sm text-neutral-900">
                {error}
              </p>
            ) : null}

            <Button type="submit" disabled={submitting}>
              {submitting ? "Se înregistrează…" : "Creează contul"}
            </Button>

            <p className="text-sm text-neutral-600">
              Ai deja cont?{" "}
              <Link
                href="/login"
                className="font-medium text-neutral-900 underline underline-offset-2 hover:text-neutral-700"
              >
                Autentifică-te
              </Link>
            </p>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
