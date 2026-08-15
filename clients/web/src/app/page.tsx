"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import {
  Package,
  ShoppingCart,
  CircleCheck,
  CircleX,
  ArrowRight,
} from "lucide-react";

import { apiGet } from "@/lib/api";
import {
  formatMoney,
  type HealthResponse,
  type ProductResponse,
} from "@/lib/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Spinner } from "@/components/ui/spinner";
import { EmptyState, LoadingState } from "@/components/states";

type Health = "checking" | "ok" | "down";

function HealthCheck() {
  const [health, setHealth] = useState<Health>("checking");

  useEffect(() => {
    let cancelled = false;
    apiGet<HealthResponse>("/health")
      .then((res) => {
        if (!cancelled) setHealth(res.status === "ok" ? "ok" : "down");
      })
      .catch(() => {
        if (!cancelled) setHealth("down");
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (health === "checking") {
    return (
      <div className="flex items-center gap-2 text-sm text-neutral-600">
        <Spinner /> Verific conexiunea cu StoreApi…
      </div>
    );
  }

  if (health === "ok") {
    return (
      <div className="flex items-center gap-2 text-sm text-neutral-700">
        <CircleCheck className="h-4 w-4" /> StoreApi conectat
      </div>
    );
  }

  return (
    <div className="flex items-center gap-2 text-sm text-neutral-600">
      <CircleX className="h-4 w-4" /> StoreApi indisponibil — porniți-l la
      localhost:5000
    </div>
  );
}

const sections = [
  {
    href: "/products",
    icon: Package,
    title: "Produse",
    description:
      "Catalogul de produse: SKU, preț, monedă, categorie și starea de activitate.",
  },
  {
    href: "/orders",
    icon: ShoppingCart,
    title: "Comenzi",
    description:
      "Comenzi cu linii de produs, status, total calculat și istoric.",
  },
];

export default function HomePage() {
  const [featured, setFeatured] = useState<ProductResponse[] | null>(null);

  useEffect(() => {
    let cancelled = false;
    apiGet<ProductResponse[]>("/products")
      .then((data) => {
        if (!cancelled) setFeatured(data.slice(0, 8));
      })
      .catch(() => {
        if (!cancelled) setFeatured([]);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className="flex flex-col gap-8">
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold tracking-tight text-neutral-900">
          Omnichannel E-commerce
        </h1>
        <p className="text-sm text-neutral-600">
          Client web pentru StoreApi. Navighează la produse sau comenzi pentru a
          explora datele din backend.
        </p>
        <HealthCheck />
      </div>

      <section className="flex flex-col gap-4">
        <div className="flex items-baseline justify-between">
          <h2 className="text-lg font-semibold tracking-tight text-neutral-900">
            Produse recomandate
          </h2>
          <Link
            href="/products"
            className="flex items-center gap-1 text-sm font-medium text-neutral-600 hover:text-neutral-900"
          >
            Vezi toate <ArrowRight className="h-4 w-4" />
          </Link>
        </div>

        {featured === null ? (
          <LoadingState label="Se încarcă produsele…" />
        ) : featured.length === 0 ? (
          <EmptyState label="Nu există produse active. Adaugă primul produs din /products/new." />
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {featured.map((product) => (
              <Link key={product.id} href={`/products/${product.id}`}>
                <Card className="h-full transition-colors hover:border-neutral-400">
                  <CardHeader>
                    <CardTitle className="line-clamp-2 text-base">
                      {product.name}
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="flex flex-col gap-2">
                    <span className="text-base font-semibold text-neutral-900">
                      {formatMoney(product.priceAmount, product.priceCurrency)}
                    </span>
                    <span className="line-clamp-2 text-xs text-neutral-600">
                      {product.description || "Fără descriere."}
                    </span>
                  </CardContent>
                </Card>
              </Link>
            ))}
          </div>
        )}
      </section>

      <div className="grid gap-4 sm:grid-cols-2">
        {sections.map((section) => (
          <Link key={section.href} href={section.href}>
            <Card className="h-full transition-colors hover:border-neutral-400">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <section.icon className="h-5 w-5" />
                  {section.title}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-neutral-600">{section.description}</p>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}
