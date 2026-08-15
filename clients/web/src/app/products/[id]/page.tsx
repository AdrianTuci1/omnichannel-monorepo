"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { ArrowLeft, ShoppingCart } from "lucide-react";

import { apiGet, apiPost } from "@/lib/api";
import { isAuthenticated } from "@/lib/auth";
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
  const router = useRouter();
  const [product, setProduct] = useState<ProductResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [adding, setAdding] = useState(false);
  const [cartMessage, setCartMessage] = useState<string | null>(null);

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

  async function addToCart() {
    if (!isAuthenticated()) {
      router.push("/login");
      return;
    }

    setAdding(true);
    setCartMessage(null);
    try {
      await apiPost("/cart/items", { productId: product!.id, quantity });
      setCartMessage(
        `Adăugat ${quantity} × ${product!.name} în coș.`
      );
    } catch (e) {
      setCartMessage(
        e instanceof Error ? e.message : "Eroare la adăugarea în coș."
      );
    } finally {
      setAdding(false);
    }
  }

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
            <Field
              label="Preț"
              value={`${formatMoney(product.priceAmount, product.priceCurrency)}`}
            />
            <Field label="Monedă" value={product.priceCurrency} />
            <Field label="Creat la" value={formatDate(product.createdAt)} />
          </div>

          <div className="flex flex-wrap items-center gap-3 rounded-md border border-neutral-200 p-4">
            <label
              htmlFor="quantity"
              className="text-sm font-medium text-neutral-700"
            >
              Cantitate
            </label>
            <input
              id="quantity"
              type="number"
              min={1}
              value={quantity}
              onChange={(e) =>
                setQuantity(Math.max(1, Number(e.target.value) || 1))
              }
              className="w-20 rounded-md border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 focus:outline-none focus:ring-1 focus:ring-neutral-400"
            />
            <Button type="button" onClick={addToCart} disabled={adding}>
              <ShoppingCart />
              {adding ? "Se adaugă…" : "Adaugă în coș"}
            </Button>
            {cartMessage ? (
              <span className="text-sm text-neutral-700">{cartMessage}</span>
            ) : null}
          </div>
        </CardContent>
      </Card>

      <RelatedProducts productId={product.id} />
      <ProductReviews productId={product.id} />
    </div>
  );
}
