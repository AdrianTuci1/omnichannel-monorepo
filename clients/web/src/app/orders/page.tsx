"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

import { apiGet } from "@/lib/api";
import { formatDate, formatMoney, type OrderResponse } from "@/lib/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { StatusBadge } from "@/components/status-badge";
import { EmptyState, ErrorState, LoadingState } from "@/components/states";

export default function OrdersPage() {
  const [orders, setOrders] = useState<OrderResponse[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    apiGet<OrderResponse[]>("/orders")
      .then((data) => {
        if (!cancelled) setOrders(data);
      })
      .catch((e: Error) => {
        if (!cancelled) setError(e.message);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (error) return <ErrorState message={error} />;
  if (!orders) return <LoadingState label="Se încarcă comenzile…" />;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-baseline justify-between">
        <h1 className="text-2xl font-semibold tracking-tight text-neutral-900">
          Comenzi
        </h1>
        <span className="text-sm text-neutral-500">
          {orders.length} comand{orders.length === 1 ? "ă" : "zi"}
        </span>
      </div>

      {orders.length === 0 ? (
        <EmptyState label="Nu există comenzi. Creează-le prin API (POST /orders)." />
      ) : (
        <div className="flex flex-col gap-2">
          {orders.map((order) => (
            <Link key={order.id} href={`/orders/${order.id}`}>
              <Card className="transition-colors hover:border-neutral-400">
                <CardHeader className="flex-row items-center justify-between gap-2 space-y-0">
                  <CardTitle className="text-base">
                    {order.orderNumber}
                  </CardTitle>
                  <StatusBadge status={order.status} />
                </CardHeader>
                <CardContent className="flex flex-wrap items-center justify-between gap-2">
                  <div className="flex flex-col gap-0.5 text-sm">
                    <span className="text-neutral-600">
                      {order.lines.length} linii · {formatDate(order.createdAt)}
                    </span>
                    <span className="text-neutral-500">
                      Client: {order.customerId}
                    </span>
                  </div>
                  <span className="text-base font-semibold text-neutral-900">
                    {formatMoney(order.totalAmount, order.totalCurrency)}
                  </span>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
