"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { ArrowLeft } from "lucide-react";

import { apiGet } from "@/lib/api";
import {
  formatDate,
  formatMoney,
  type ProductResponse,
} from "@/lib/types";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ErrorState, LoadingState } from "@/components/states";
import { ProductReviews } from "@/components/product-reviews";
import { RelatedProducts } from "@/components/related-products";

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-xs uppercase tracking-wide text-neutral-500">
        {label}
      </span>
      <span className="text-sm text-neutral-900">{value}</span>
    </div>
  );
}

export default function ProductDetailPage() {
  const params = useParams<{ id: string }>();
  const [product, setProduct] = useState<ProductResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    apiGet<ProductResponse>(`/products/${params.id}`)
      .then((data) => {
        if (!cancelled) setProduct(data);
      })
      .catch((e: Error) => {
        if (!cancelled) setError(e.message);
      });
    return () => {
      cancelled = true;
    };
  }, [params.id]);

  if (error) return <ErrorState message={error} />;
  if (!product) return <LoadingState label="Se încarcă produsul…" />;

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
          <CardTitle className="flex items-start justify-between gap-2">
            <span>{product.name}</span>
            <span className="text-lg font-semibold text-neutral-900">
              {formatMoney(product.priceAmount, product.priceCurrency)}
            </span>
          </CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-6">
          <div className="flex items-center gap-2">
            <Badge variant="secondary">{product.sku}</Badge>
            {product.isActive ? (
              <Badge variant="success">activ</Badge>
            ) : (
              <Badge variant="muted">inactiv</Badge>
            )}
          </div>

          <p className="text-sm leading-relaxed text-neutral-700">
            {product.description || "Fără descriere."}
          </p>

          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <Field label="ID" value={product.id} />
            <Field label="SKU" value={product.sku} />
            <Field label="Categorie" value={product.categoryId} />
            <Field label="Preț" value={`${formatMoney(product.priceAmount, product.priceCurrency)}`} />
            <Field label="Monedă" value={product.priceCurrency} />
            <Field label="Creat la" value={formatDate(product.createdAt)} />
          </div>
        </CardContent>
      </Card>

      <RelatedProducts productId={product.id} />
      <ProductReviews productId={product.id} />
    </div>
  );
}
