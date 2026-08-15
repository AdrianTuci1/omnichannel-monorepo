"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Trash2 } from "lucide-react";

import { apiDelete, apiGet } from "@/lib/api";
import {
  formatDate,
  formatMoney,
  type OrderResponse,
  type ProductResponse,
  type Review,
} from "@/lib/types";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { StatusBadge } from "@/components/status-badge";
import { EmptyState, ErrorState, LoadingState } from "@/components/states";

type ReviewWithProduct = Review & { productName: string };

export default function AdminPage() {
  const [products, setProducts] = useState<ProductResponse[] | null>(null);
  const [orders, setOrders] = useState<OrderResponse[] | null>(null);
  const [reviews, setReviews] = useState<ReviewWithProduct[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    (async () => {
      try {
        const [prods, ords] = await Promise.all([
          apiGet<ProductResponse[]>("/products"),
          apiGet<OrderResponse[]>("/orders"),
        ]);
        if (cancelled) return;
        setProducts(prods);
        setOrders(ords);

        const reviewsPerProduct = await Promise.all(
          prods.map(async (p): Promise<ReviewWithProduct[]> => {
            try {
              const revs = await apiGet<Review[]>(
                `/products/${p.id}/reviews`
              );
              return revs.map((r) => ({ ...r, productName: p.name }));
            } catch {
              return [];
            }
          })
        );
        if (!cancelled) setReviews(reviewsPerProduct.flat());
      } catch (e) {
        if (!cancelled) {
          setError(
            e instanceof Error ? e.message : "Eroare la încărcarea datelor."
          );
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  async function deleteProduct(id: string) {
    setActionError(null);
    try {
      await apiDelete(`/products/${id}`);
      setProducts((prev) => prev?.filter((p) => p.id !== id) ?? prev);
    } catch (e) {
      setActionError(
        e instanceof Error ? e.message : "Eroare la ștergerea produsului."
      );
    }
  }

  async function deleteOrder(id: string) {
    setActionError(null);
    try {
      await apiDelete(`/orders/${id}`);
      setOrders((prev) => prev?.filter((o) => o.id !== id) ?? prev);
    } catch (e) {
      setActionError(
        e instanceof Error ? e.message : "Eroare la ștergerea comenzii."
      );
    }
  }

  async function deleteReview(id: string) {
    setActionError(null);
    try {
      await apiDelete(`/reviews/${id}`);
      setReviews((prev) => prev?.filter((r) => r.id !== id) ?? prev);
    } catch (e) {
      setActionError(
        e instanceof Error ? e.message : "Eroare la ștergerea recenziei."
      );
    }
  }

  if (error) return <ErrorState message={error} />;
  if (!products || !orders || !reviews) {
    return <LoadingState label="Se încarcă datele de administrare…" />;
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold tracking-tight text-neutral-900">
          Administrare
        </h1>
        <p className="text-sm text-neutral-600">
          Șterge produse, comenzi și recenzii. Operațiile de ștergere necesită
          autentificare.
        </p>
      </div>

      {actionError ? (
        <p className="rounded-md border border-neutral-300 px-3 py-2 text-sm text-neutral-900">
          {actionError}
        </p>
      ) : null}

      <section className="flex flex-col gap-3">
        <h2 className="text-lg font-semibold tracking-tight text-neutral-900">
          Produse ({products.length})
        </h2>
        {products.length === 0 ? (
          <EmptyState label="Nu există produse active." />
        ) : (
          <div className="flex flex-col gap-2">
            {products.map((product) => (
              <Card key={product.id}>
                <CardContent className="flex items-center justify-between gap-4 p-4">
                  <div className="flex min-w-0 flex-col gap-0.5">
                    <Link
                      href={`/products/${product.id}`}
                      className="truncate text-sm font-medium text-neutral-900 hover:underline"
                    >
                      {product.name}
                    </Link>
                    <span className="text-xs text-neutral-500">
                      {product.sku} · {formatMoney(product.priceAmount, product.priceCurrency)}
                    </span>
                  </div>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={() => deleteProduct(product.id)}
                    aria-label={`Șterge produsul ${product.name}`}
                  >
                    <Trash2 />
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="text-lg font-semibold tracking-tight text-neutral-900">
          Comenzi ({orders.length})
        </h2>
        {orders.length === 0 ? (
          <EmptyState label="Nu există comenzi." />
        ) : (
          <div className="flex flex-col gap-2">
            {orders.map((order) => (
              <Card key={order.id}>
                <CardContent className="flex items-center justify-between gap-4 p-4">
                  <div className="flex min-w-0 flex-col gap-0.5">
                    <Link
                      href={`/orders/${order.id}`}
                      className="truncate text-sm font-medium text-neutral-900 hover:underline"
                    >
                      {order.orderNumber}
                    </Link>
                    <span className="text-xs text-neutral-500">
                      {formatDate(order.createdAt)} ·{" "}
                      {formatMoney(order.totalAmount, order.totalCurrency)}
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <StatusBadge status={order.status} />
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => deleteOrder(order.id)}
                      aria-label={`Șterge comanda ${order.orderNumber}`}
                    >
                      <Trash2 />
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="text-lg font-semibold tracking-tight text-neutral-900">
          Recenzii ({reviews.length})
        </h2>
        {reviews.length === 0 ? (
          <EmptyState label="Nu există recenzii." />
        ) : (
          <div className="flex flex-col gap-2">
            {reviews.map((review) => (
              <Card key={review.id}>
                <CardContent className="flex items-center justify-between gap-4 p-4">
                  <div className="flex min-w-0 flex-col gap-0.5">
                    <span className="truncate text-sm font-medium text-neutral-900">
                      {review.title}
                    </span>
                    <span className="text-xs text-neutral-500">
                      {review.productName} · {review.rating}/5 ·{" "}
                      {formatDate(review.createdAt)}
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge variant="secondary">{review.customerId.slice(0, 8)}</Badge>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => deleteReview(review.id)}
                      aria-label={`Șterge recenzia ${review.title}`}
                    >
                      <Trash2 />
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
