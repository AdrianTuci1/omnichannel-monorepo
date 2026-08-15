"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { ArrowLeft } from "lucide-react";

import { apiGet } from "@/lib/api";
import {
  formatDate,
  formatMoney,
  type OrderResponse,
} from "@/lib/types";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { StatusBadge } from "@/components/status-badge";
import { ErrorState, LoadingState } from "@/components/states";

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

export default function OrderDetailPage() {
  const params = useParams<{ id: string }>();
  const [order, setOrder] = useState<OrderResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    apiGet<OrderResponse>(`/orders/${params.id}`)
      .then((data) => {
        if (!cancelled) setOrder(data);
      })
      .catch((e: Error) => {
        if (!cancelled) setError(e.message);
      });
    return () => {
      cancelled = true;
    };
  }, [params.id]);

  if (error) return <ErrorState message={error} />;
  if (!order) return <LoadingState label="Se încarcă comanda…" />;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button asChild variant="ghost" size="sm">
          <Link href="/orders">
            <ArrowLeft /> Înapoi la comenzi
          </Link>
        </Button>
      </div>

      <Card>
        <CardHeader className="flex-row items-center justify-between gap-2 space-y-0">
          <CardTitle>{order.orderNumber}</CardTitle>
          <StatusBadge status={order.status} />
        </CardHeader>
        <CardContent className="flex flex-col gap-6">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <Field label="ID" value={order.id} />
            <Field label="Client" value={order.customerId} />
            <Field label="Status" value={order.status} />
            <Field label="Monedă" value={order.currency} />
            <Field label="Creat la" value={formatDate(order.createdAt)} />
            <Field
              label="Total"
              value={formatMoney(order.totalAmount, order.totalCurrency)}
            />
          </div>

          {order.notes ? (
            <p className="text-sm leading-relaxed text-neutral-700">
              <span className="text-xs uppercase tracking-wide text-neutral-500">
                Note:{" "}
              </span>
              {order.notes}
            </p>
          ) : null}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Linii comandă</CardTitle>
        </CardHeader>
        <CardContent>
          {order.lines.length === 0 ? (
            <p className="text-sm text-neutral-500">Comanda nu are linii.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-neutral-200 text-left text-xs uppercase tracking-wide text-neutral-500">
                    <th className="py-2 pr-4 font-medium">Produs</th>
                    <th className="py-2 pr-4 font-medium">Cantitate</th>
                    <th className="py-2 pr-4 font-medium">Preț unitar</th>
                    <th className="py-2 font-medium text-right">Total linie</th>
                  </tr>
                </thead>
                <tbody>
                  {order.lines.map((line) => (
                    <tr
                      key={line.id}
                      className="border-b border-neutral-100 last:border-0"
                    >
                      <td className="py-2 pr-4">
                        <div className="flex flex-col">
                          <span className="text-neutral-900">
                            {line.productName}
                          </span>
                          <span className="text-xs text-neutral-500">
                            {line.productId}
                          </span>
                        </div>
                      </td>
                      <td className="py-2 pr-4 text-neutral-700">
                        {line.quantity}
                      </td>
                      <td className="py-2 pr-4 text-neutral-700">
                        {formatMoney(
                          line.unitPriceAmount,
                          line.unitPriceCurrency
                        )}
                      </td>
                      <td className="py-2 text-right font-medium text-neutral-900">
                        {formatMoney(
                          line.lineTotalAmount,
                          line.lineTotalCurrency
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
