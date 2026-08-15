"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

import { apiGet } from "@/lib/api";
import { formatMoney, type ProductResponse } from "@/lib/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { EmptyState, ErrorState, LoadingState } from "@/components/states";

export default function ProductsPage() {
  const [products, setProducts] = useState<ProductResponse[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    apiGet<ProductResponse[]>("/products")
      .then((data) => {
        if (!cancelled) setProducts(data);
      })
      .catch((e: Error) => {
        if (!cancelled) setError(e.message);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (error) return <ErrorState message={error} />;
  if (!products) return <LoadingState label="Se încarcă produsele…" />;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-baseline justify-between">
        <h1 className="text-2xl font-semibold tracking-tight text-neutral-900">
          Produse
        </h1>
        <span className="text-sm text-neutral-500">
          {products.length} produs{products.length === 1 ? "" : "e"}
        </span>
      </div>

      {products.length === 0 ? (
        <EmptyState label="Nu există produse. Creează-le prin API (POST /products)." />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {products.map((product) => (
            <Link key={product.id} href={`/products/${product.id}`}>
              <Card className="h-full transition-colors hover:border-neutral-400">
                <CardHeader>
                  <CardTitle className="flex items-start justify-between gap-2">
                    <span className="line-clamp-2">{product.name}</span>
                    <span className="shrink-0 text-sm font-medium text-neutral-900">
                      {formatMoney(product.priceAmount, product.priceCurrency)}
                    </span>
                  </CardTitle>
                </CardHeader>
                <CardContent className="flex flex-col gap-3">
                  <p className="line-clamp-2 text-sm text-neutral-600">
                    {product.description || "Fără descriere."}
                  </p>
                  <div className="flex items-center gap-2">
                    <Badge variant="secondary">{product.sku}</Badge>
                    {product.isActive ? (
                      <Badge variant="success">activ</Badge>
                    ) : (
                      <Badge variant="muted">inactiv</Badge>
                    )}
                  </div>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
