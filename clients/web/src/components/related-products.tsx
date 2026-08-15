"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

import { apiGet } from "@/lib/api";
import type { RelatedProduct } from "@/lib/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

export function RelatedProducts({ productId }: { productId: string }) {
  const [related, setRelated] = useState<RelatedProduct[] | null>(null);

  useEffect(() => {
    let cancelled = false;
    apiGet<RelatedProduct[]>(`/products/${productId}/related`)
      .then((data) => {
        if (!cancelled) setRelated(data);
      })
      .catch(() => {
        if (!cancelled) setRelated([]);
      });
    return () => {
      cancelled = true;
    };
  }, [productId]);

  if (related === null || related.length === 0) return null;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Produse similare</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-2">
        {related.map((item) => (
          <Link key={item.productId} href={`/products/${item.productId}`}>
            <div className="flex items-center justify-between gap-2 rounded-md border border-neutral-200 px-3 py-2 transition-colors hover:border-neutral-400">
              <span className="text-sm text-neutral-900">{item.name}</span>
              <span className="shrink-0 text-xs text-neutral-500">
                scor {item.score.toFixed(2)}
              </span>
            </div>
          </Link>
        ))}
      </CardContent>
    </Card>
  );
}
