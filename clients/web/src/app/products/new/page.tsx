"use client";

import { useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft } from "lucide-react";

import { apiPost } from "@/lib/api";
import type { CreateProductRequest, ProductResponse } from "@/lib/types";
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

export default function NewProductPage() {
  const router = useRouter();
  const [sku, setSku] = useState("");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [priceAmount, setPriceAmount] = useState("");
  const [priceCurrency, setPriceCurrency] = useState("USD");
  const [categoryId, setCategoryId] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    const payload: CreateProductRequest = {
      sku: sku.trim(),
      name: name.trim(),
      description: description.trim() === "" ? null : description.trim(),
      priceAmount: Number(priceAmount),
      priceCurrency:
        priceCurrency.trim() === "" ? "USD" : priceCurrency.trim(),
      categoryId: categoryId.trim() === "" ? null : categoryId.trim(),
    };

    try {
      await apiPost<ProductResponse>("/products", payload);
      router.push("/products");
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Eroare la crearea produsului."
      );
      setSubmitting(false);
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button asChild variant="ghost" size="sm">
          <Link href="/products">
            <ArrowLeft /> Înapoi la produse
          </Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Produs nou</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="flex flex-col gap-1">
                <label htmlFor="sku" className={labelClass}>
                  SKU
                </label>
                <input
                  id="sku"
                  className={inputClass}
                  value={sku}
                  onChange={(e) => setSku(e.target.value)}
                  required
                  placeholder="SKU-123"
                />
              </div>
              <div className="flex flex-col gap-1">
                <label htmlFor="name" className={labelClass}>
                  Nume
                </label>
                <input
                  id="name"
                  className={inputClass}
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  required
                  placeholder="Produs exemplu"
                />
              </div>
            </div>

            <div className="flex flex-col gap-1">
              <label htmlFor="description" className={labelClass}>
                Descriere
              </label>
              <textarea
                id="description"
                className={inputClass}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                placeholder="Descriere opțională"
              />
            </div>

            <div className="grid gap-4 sm:grid-cols-3">
              <div className="flex flex-col gap-1">
                <label htmlFor="priceAmount" className={labelClass}>
                  Preț
                </label>
                <input
                  id="priceAmount"
                  type="number"
                  min="0"
                  step="0.01"
                  className={inputClass}
                  value={priceAmount}
                  onChange={(e) => setPriceAmount(e.target.value)}
                  required
                  placeholder="19.99"
                />
              </div>
              <div className="flex flex-col gap-1">
                <label htmlFor="priceCurrency" className={labelClass}>
                  Monedă
                </label>
                <input
                  id="priceCurrency"
                  className={inputClass}
                  value={priceCurrency}
                  onChange={(e) => setPriceCurrency(e.target.value)}
                  required
                  placeholder="USD"
                />
              </div>
              <div className="flex flex-col gap-1">
                <label htmlFor="categoryId" className={labelClass}>
                  Categorie (GUID)
                </label>
                <input
                  id="categoryId"
                  className={inputClass}
                  value={categoryId}
                  onChange={(e) => setCategoryId(e.target.value)}
                  placeholder="Opțional — folosește categoria implicită"
                />
              </div>
            </div>

            {error ? (
              <p className="rounded-md border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900">
                {error}
              </p>
            ) : null}

            <div className="flex items-center gap-2">
              <Button type="submit" disabled={submitting}>
                {submitting ? "Se salvează…" : "Adaugă produsul"}
              </Button>
              <Button asChild variant="ghost">
                <Link href="/products">Anulează</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
