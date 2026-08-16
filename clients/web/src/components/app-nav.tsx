"use client";

import { useEffect, useState, type FormEvent } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { Search, ShoppingCart, LogOut, LogIn } from "lucide-react";

import { cn } from "@/lib/utils";
import { isAuthenticated, logout } from "@/lib/auth";

const links = [
  { href: "/", label: "Acasă", exact: true },
  { href: "/products", label: "Produse", exact: false },
  { href: "/orders", label: "Comenzi", exact: false },
  { href: "/admin", label: "Admin", exact: false },
];

export function AppNav() {
  const pathname = usePathname();
  const router = useRouter();
  const [authed, setAuthed] = useState(false);
  const [query, setQuery] = useState("");

  useEffect(() => {
    setAuthed(isAuthenticated());
  }, [pathname]);

  function handleSearch(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const q = query.trim();
    router.push(
      q === "" ? "/products" : `/products?search=${encodeURIComponent(q)}`
    );
  }

  async function handleLogout() {
    await logout();
    router.push("/login");
  }

  return (
    <header className="border-b border-neutral-200">
      <nav className="mx-auto flex max-w-6xl items-center gap-3 px-4 py-4">
        <Link
          href="/"
          className="shrink-0 text-lg font-semibold tracking-tight text-neutral-900"
        >
          Omnichannel Store
        </Link>

        <div className="flex items-center gap-1">
          {links.map((link) => {
            const active = link.exact
              ? pathname === link.href
              : pathname === link.href ||
                pathname.startsWith(`${link.href}/`);
            return (
              <Link
                key={link.href}
                href={link.href}
                className={cn(
                  "rounded-md px-3 py-2 text-sm font-medium transition-colors",
                  active
                    ? "bg-neutral-100 text-neutral-900"
                    : "text-neutral-600 hover:text-neutral-900"
                )}
              >
                {link.label}
              </Link>
            );
          })}
        </div>

        <div className="ml-auto flex items-center gap-2">
          <form onSubmit={handleSearch} className="flex items-center gap-1">
            <input
              type="search"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Caută produse…"
              aria-label="Caută produse"
              className="w-40 rounded-md border border-neutral-300 bg-white px-3 py-1.5 text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-1 focus:ring-neutral-400 sm:w-56"
            />
            <button
              type="submit"
              aria-label="Caută"
              className="inline-flex h-8 w-8 items-center justify-center rounded-md text-neutral-600 transition-colors hover:bg-neutral-100 hover:text-neutral-900"
            >
              <Search className="h-4 w-4" />
            </button>
          </form>

          <Link
            href="/cart"
            className="inline-flex items-center gap-1 rounded-md px-2 py-2 text-sm font-medium text-neutral-600 transition-colors hover:text-neutral-900"
          >
            <ShoppingCart className="h-4 w-4" />
            <span className="hidden sm:inline">Coș</span>
          </Link>

          {authed ? (
            <button
              type="button"
              onClick={handleLogout}
              className="inline-flex items-center gap-1 rounded-md px-2 py-2 text-sm font-medium text-neutral-600 transition-colors hover:text-neutral-900"
            >
              <LogOut className="h-4 w-4" />
              <span className="hidden sm:inline">Logout</span>
            </button>
          ) : (
            <Link
              href="/login"
              className="inline-flex items-center gap-1 rounded-md px-2 py-2 text-sm font-medium text-neutral-600 transition-colors hover:text-neutral-900"
            >
              <LogIn className="h-4 w-4" />
              <span className="hidden sm:inline">Login</span>
            </Link>
          )}
        </div>
      </nav>
    </header>
  );
}
