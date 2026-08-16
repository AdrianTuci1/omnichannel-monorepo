"use client";

import {
  Suspense,
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

import { apiGet } from "@/lib/api";
import {
  formatMoney,
  type CategoryResponse,
  type ProductListResponse,
  type ProductResponse,
} from "@/lib/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { EmptyState, ErrorState, LoadingState } from "@/components/states";

const PAGE_SIZE = 12;

const inputClass =
  "w-full rounded-md border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-1 focus:ring-neutral-400";
const labelClass = "text-sm font-medium text-neutral-700";

const SORT_OPTIONS: { value: SortValue; label: string }[] = [
  { value: "name", label: "Nume" },
  { value: "price_asc", label: "Preț crescător" },
  { value: "price_desc", label: "Preț descrescător" },
  { value: "newest", label: "Cele mai noi" },
];

type SortValue = "name" | "price_asc" | "price_desc" | "newest";

interface FilterDraft {
  search: string;
  categoryId: string;
  minPrice: string;
  maxPrice: string;
  inStock: boolean;
  sort: SortValue;
}

const DEFAULT_DRAFT: FilterDraft = {
  search: "",
  categoryId: "",
  minPrice: "",
  maxPrice: "",
  inStock: false,
  sort: "name",
};

function isSortValue(value: string | null): value is SortValue {
  return value === "name" || value === "price_asc" || value === "price_desc" || value === "newest";
}

function draftFromParams(searchParams: URLSearchParams): FilterDraft {
  const sort = searchParams.get("sort");
  return {
    search: searchParams.get("search") ?? "",
    categoryId: searchParams.get("categoryId") ?? "",
    minPrice: searchParams.get("minPrice") ?? "",
    maxPrice: searchParams.get("maxPrice") ?? "",
    inStock: searchParams.get("inStock") === "true",
    sort: isSortValue(sort) ? sort : "name",
  };
}

function buildQuery(
  draft: FilterDraft,
  page: number
): URLSearchParams {
  const params = new URLSearchParams();
  if (draft.search.trim() !== "") params.set("search", draft.search.trim());
  if (draft.categoryId.trim() !== "") params.set("categoryId", draft.categoryId.trim());
  if (draft.minPrice.trim() !== "") params.set("minPrice", draft.minPrice.trim());
  if (draft.maxPrice.trim() !== "") params.set("maxPrice", draft.maxPrice.trim());
  if (draft.inStock) params.set("inStock", "true");
  if (draft.sort !== "name") params.set("sort", draft.sort);
  if (page > 1) params.set("page", String(page));
  params.set("pageSize", String(PAGE_SIZE));
  return params;
}

function ProductsList() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const [draft, setDraft] = useState<FilterDraft>(() =>
    draftFromParams(new URLSearchParams(searchParams.toString()))
  );
  const [categories, setCategories] = useState<CategoryResponse[]>([]);
  const [result, setResult] = useState<ProductListResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const queryString = searchParams.toString();
  const page = useMemo(() => {
    const parsed = Number.parseInt(searchParams.get("page") ?? "1", 10);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : 1;
  }, [searchParams]);

  // Sincronizează formularul de filtre când URL-ul se schimbă (back/forward,
  // linkuri externe, reset).
  useEffect(() => {
    setDraft(draftFromParams(new URLSearchParams(searchParams.toString())));
  }, [searchParams]);

  // Încarcă lista de categorii o singură dată, pentru dropdown.
  useEffect(() => {
    let cancelled = false;
    apiGet<CategoryResponse[]>("/categories")
      .then((data) => {
        if (!cancelled) setCategories(data);
      })
      .catch(() => {
        // Eșecul la încărcarea categoriilor nu blochează filtrarea: dropdown-ul
        // rămâne funcțional, doar fără opțiuni.
        if (!cancelled) setCategories([]);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  // Reîncarcă produsele la fiecare schimbare a parametrilor din URL.
  useEffect(() => {
    let cancelled = false;
    setResult(null);
    setError(null);
    const qs = queryString === "" ? "" : `?${queryString}`;
    apiGet<ProductListResponse>(`/products${qs}`)
      .then((data) => {
        if (!cancelled) setResult(data);
      })
      .catch((e: Error) => {
        if (!cancelled) setError(e.message);
      });
    return () => {
      cancelled = true;
    };
  }, [queryString]);

  function applyFilters(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const params = buildQuery(draft, 1);
    const qs = params.toString();
    router.push(qs === "" ? pathname : `${pathname}?${qs}`);
  }

  function resetFilters() {
    setDraft(DEFAULT_DRAFT);
    router.push(pathname);
  }

  function goToPage(nextPage: number) {
    const params = buildQuery(
      draftFromParams(new URLSearchParams(searchParams.toString())),
      nextPage
    );
    const qs = params.toString();
    router.push(qs === "" ? pathname : `${pathname}?${qs}`);
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.total / PAGE_SIZE)) : 1;

  if (error) return <ErrorState message={error} />;

  const products: ProductResponse[] = result?.items ?? [];

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-baseline justify-between">
        <h1 className="text-2xl font-semibold tracking-tight text-neutral-900">
          Produse
        </h1>
        {result ? (
          <span className="text-sm text-neutral-500">
            {result.total} produs{result.total === 1 ? "" : "e"}
          </span>
        ) : null}
      </div>

      <Card>
        <CardContent className="p-4">
          <form
            onSubmit={applyFilters}
            className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3"
          >
            <div className="flex flex-col gap-1 sm:col-span-2 lg:col-span-1">
              <label htmlFor="search" className={labelClass}>
                Căutare
              </label>
              <input
                id="search"
                className={inputClass}
                value={draft.search}
                onChange={(e) =>
                  setDraft((d) => ({ ...d, search: e.target.value }))
                }
                placeholder="Nume, SKU sau descriere"
              />
            </div>

            <div className="flex flex-col gap-1">
              <label htmlFor="categoryId" className={labelClass}>
                Categorie
              </label>
              <select
                id="categoryId"
                className={inputClass}
                value={draft.categoryId}
                onChange={(e) =>
                  setDraft((d) => ({ ...d, categoryId: e.target.value }))
                }
              >
                <option value="">Toate categoriile</option>
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="flex flex-col gap-1">
              <label htmlFor="sort" className={labelClass}>
                Sortare
              </label>
              <select
                id="sort"
                className={inputClass}
                value={draft.sort}
                onChange={(e) =>
                  setDraft((d) => ({ ...d, sort: e.target.value as SortValue }))
                }
              >
                {SORT_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>

            <div className="flex flex-col gap-1">
              <label htmlFor="minPrice" className={labelClass}>
                Preț minim
              </label>
              <input
                id="minPrice"
                type="number"
                min="0"
                step="0.01"
                className={inputClass}
                value={draft.minPrice}
                onChange={(e) =>
                  setDraft((d) => ({ ...d, minPrice: e.target.value }))
                }
                placeholder="0"
              />
            </div>

            <div className="flex flex-col gap-1">
              <label htmlFor="maxPrice" className={labelClass}>
                Preț maxim
              </label>
              <input
                id="maxPrice"
                type="number"
                min="0"
                step="0.01"
                className={inputClass}
                value={draft.maxPrice}
                onChange={(e) =>
                  setDraft((d) => ({ ...d, maxPrice: e.target.value }))
                }
                placeholder="1000"
              />
            </div>

            <div className="flex items-end">
              <label className="flex cursor-pointer items-center gap-2 text-sm text-neutral-700">
                <input
                  type="checkbox"
                  className="h-4 w-4 rounded border-neutral-300 text-neutral-900 accent-neutral-900"
                  checked={draft.inStock}
                  onChange={(e) =>
                    setDraft((d) => ({ ...d, inStock: e.target.checked }))
                  }
                />
                Doar produsele în stoc
              </label>
            </div>

            <div className="flex items-end gap-2 sm:col-span-2 lg:col-span-3">
              <Button type="submit">Aplică filtrele</Button>
              <Button type="button" variant="ghost" onClick={resetFilters}>
                Resetează
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      {!result ? (
        <LoadingState label="Se încarcă produsele…" />
      ) : products.length === 0 ? (
        <EmptyState label="Niciun produs nu corespunde filtrelor aplicate." />
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

      {result ? (
        <div className="flex items-center justify-between gap-4">
          <span className="text-sm text-neutral-500">
            Pagina {result.page} din {totalPages} · {result.total} produs
            {result.total === 1 ? "" : "e"}
          </span>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={page <= 1}
              onClick={() => goToPage(page - 1)}
            >
              Precedent
            </Button>
            <Button
              type="button"
              variant="outline"
              disabled={page >= totalPages}
              onClick={() => goToPage(page + 1)}
            >
              Următor
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}

export default function ProductsPage() {
  return (
    <Suspense fallback={<LoadingState label="Se încarcă produsele…" />}>
      <ProductsList />
    </Suspense>
  );
}
