"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import Link from "next/link";
import { Minus, Plus, Trash2 } from "lucide-react";

import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api";
import { getCustomerId, isAuthenticated, saveCustomerId } from "@/lib/auth";
import {
  formatMoney,
  paymentMethodLabel,
  paymentStatusLabel,
  type CartItem,
  type CreateOrderRequest,
  type OrderResponse,
  type PaymentMethod,
} from "@/lib/types";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { EmptyState, LoadingState } from "@/components/states";

const inputClass =
  "w-full rounded-md border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-1 focus:ring-neutral-400";
const labelClass = "text-sm font-medium text-neutral-700";

const PAYMENT_METHODS: { value: PaymentMethod; label: string; hint: string }[] =
  [
    { value: "CashOnDelivery", label: "Ramburs", hint: "Plătești la livrare" },
    { value: "Card", label: "Card", hint: "Plată cu cardul" },
    { value: "BankTransfer", label: "Transfer bancar", hint: "Plată prin transfer bancar" },
  ];

export default function CartPage() {
  const [authed, setAuthed] = useState(false);
  const [items, setItems] = useState<CartItem[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [customerId, setCustomerId] = useState("");
  const [placing, setPlacing] = useState(false);
  const [placedOrder, setPlacedOrder] = useState<OrderResponse | null>(null);
  const [paymentMethod, setPaymentMethod] =
    useState<PaymentMethod>("CashOnDelivery");

  const loadCart = useCallback(() => {
    setItems(null);
    setError(null);
    apiGet<CartItem[]>("/cart")
      .then(setItems)
      .catch((e: Error) => setError(e.message));
  }, []);

  useEffect(() => {
    const loggedIn = isAuthenticated();
    setAuthed(loggedIn);
    setCustomerId(getCustomerId() ?? "");
    if (loggedIn) loadCart();
  }, [loadCart]);

  async function updateQuantity(item: CartItem, quantity: number) {
    setError(null);
    try {
      if (quantity <= 0) {
        await apiDelete(`/cart/items/${item.productId}`);
      } else {
        await apiPut(`/cart/items/${item.productId}`, { quantity });
      }
      loadCart();
    } catch (e) {
      setError(
        e instanceof Error ? e.message : "Eroare la actualizarea coșului."
      );
    }
  }

  async function removeItem(item: CartItem) {
    setError(null);
    try {
      await apiDelete(`/cart/items/${item.productId}`);
      loadCart();
    } catch (e) {
      setError(
        e instanceof Error ? e.message : "Eroare la ștergerea produsului."
      );
    }
  }

  async function placeOrder(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!items || items.length === 0) return;
    setPlacing(true);
    setError(null);
    setPlacedOrder(null);

    const payload: CreateOrderRequest = {
      customerId: customerId.trim(),
      paymentMethod,
      lines: items.map((i) => ({
        productId: i.productId,
        quantity: i.quantity,
      })),
    };

    try {
      const order = await apiPost<OrderResponse>("/orders", payload);
      saveCustomerId(customerId.trim());
      await Promise.all(
        items.map((i) => apiDelete(`/cart/items/${i.productId}`))
      );
      setPlacedOrder(order);
      loadCart();
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Eroare la plasarea comenzii."
      );
    } finally {
      setPlacing(false);
    }
  }

  if (!authed) {
    return (
      <div className="flex flex-col gap-4">
        <h1 className="text-2xl font-semibold tracking-tight text-neutral-900">
          Coș
        </h1>
        <EmptyState label="Autentifică-te pentru a vedea coșul de cumpărături." />
        <div>
          <Button asChild>
            <Link href="/login">Autentifică-te</Link>
          </Button>
        </div>
      </div>
    );
  }

  const total = items
    ? items.reduce((sum, i) => sum + i.unitPriceAmount * i.quantity, 0)
    : 0;
  const currency =
    items && items.length > 0 ? items[0].unitPriceCurrency : "USD";

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-baseline justify-between">
        <h1 className="text-2xl font-semibold tracking-tight text-neutral-900">
          Coș
        </h1>
        {items ? (
          <span className="text-sm text-neutral-500">
            {items.length} produs{items.length === 1 ? "" : "e"}
          </span>
        ) : null}
      </div>

      {placedOrder ? (
        <div className="flex flex-col gap-1 rounded-md border border-neutral-300 bg-white px-4 py-3 text-sm text-neutral-900">
          <span>
            Comanda{" "}
            <Link
              href={`/orders/${placedOrder.id}`}
              className="font-semibold underline underline-offset-2"
            >
              {placedOrder.orderNumber}
            </Link>{" "}
            a fost plasată cu succes.
          </span>
          <span className="text-neutral-600">
            Metodă de plată:{" "}
            {paymentMethodLabel(placedOrder.paymentMethod ?? paymentMethod)}
            {placedOrder.paymentStatus
              ? ` · Status plată: ${paymentStatusLabel(
                  placedOrder.paymentStatus
                )}`
              : ""}
          </span>
        </div>
      ) : null}

      {error ? (
        <p className="rounded-md border border-neutral-300 px-3 py-2 text-sm text-neutral-900">
          {error}
        </p>
      ) : null}

      {items === null ? (
        <LoadingState label="Se încarcă coșul…" />
      ) : items.length === 0 ? (
        <EmptyState label="Coșul este gol. Adaugă produse din catalog." />
      ) : (
        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            {items.map((item) => (
              <Card key={item.productId}>
                <CardContent className="flex flex-wrap items-center justify-between gap-4 p-4">
                  <div className="flex min-w-0 flex-col gap-0.5">
                    <Link
                      href={`/products/${item.productId}`}
                      className="truncate text-sm font-medium text-neutral-900 hover:underline"
                    >
                      {item.productName}
                    </Link>
                    <span className="text-xs text-neutral-500">
                      {formatMoney(
                        item.unitPriceAmount,
                        item.unitPriceCurrency
                      )}{" "}
                      / buc.
                    </span>
                  </div>

                  <div className="flex items-center gap-2">
                    <Button
                      type="button"
                      variant="outline"
                      size="icon"
                      disabled={item.quantity <= 1}
                      onClick={() => updateQuantity(item, item.quantity - 1)}
                      aria-label="Scade cantitatea"
                    >
                      <Minus />
                    </Button>
                    <span className="w-8 text-center text-sm font-medium text-neutral-900">
                      {item.quantity}
                    </span>
                    <Button
                      type="button"
                      variant="outline"
                      size="icon"
                      onClick={() => updateQuantity(item, item.quantity + 1)}
                      aria-label="Crește cantitatea"
                    >
                      <Plus />
                    </Button>
                  </div>

                  <div className="flex items-center gap-4">
                    <span className="text-sm font-semibold text-neutral-900">
                      {formatMoney(
                        item.unitPriceAmount * item.quantity,
                        item.unitPriceCurrency
                      )}
                    </span>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => removeItem(item)}
                      aria-label="Șterge produsul din coș"
                    >
                      <Trash2 />
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Finalizare comandă</CardTitle>
            </CardHeader>
            <CardContent>
              <form onSubmit={placeOrder} className="flex flex-col gap-4">
                <div className="flex flex-col gap-1">
                  <label htmlFor="customerId" className={labelClass}>
                    ID client (customerId)
                  </label>
                  <input
                    id="customerId"
                    className={inputClass}
                    value={customerId}
                    onChange={(e) => setCustomerId(e.target.value)}
                    required
                    placeholder="GUID client"
                  />
                </div>

                <fieldset className="flex flex-col gap-2">
                  <legend className={labelClass}>Metodă de plată</legend>
                  <div className="flex flex-col gap-2 sm:flex-row sm:gap-3">
                    {PAYMENT_METHODS.map((method) => (
                      <label
                        key={method.value}
                        className={`flex flex-1 cursor-pointer flex-col gap-0.5 rounded-md border px-3 py-2 text-sm transition-colors ${
                          paymentMethod === method.value
                            ? "border-neutral-900 bg-neutral-50 text-neutral-900"
                            : "border-neutral-300 bg-white text-neutral-700 hover:border-neutral-400"
                        }`}
                      >
                        <span className="flex items-center gap-2 font-medium">
                          <input
                            type="radio"
                            name="paymentMethod"
                            value={method.value}
                            checked={paymentMethod === method.value}
                            onChange={() => setPaymentMethod(method.value)}
                            className="accent-neutral-900"
                          />
                          {method.label}
                        </span>
                        <span className="text-xs text-neutral-500">
                          {method.hint}
                        </span>
                      </label>
                    ))}
                  </div>
                </fieldset>

                <div className="flex flex-wrap items-center justify-between gap-4">
                  <span className="text-base font-semibold text-neutral-900">
                    Total: {formatMoney(total, currency)}
                  </span>
                  <Button type="submit" disabled={placing}>
                    {placing ? "Se plasează…" : "Plasează comanda"}
                  </Button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
