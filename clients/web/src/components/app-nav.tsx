"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

import { cn } from "@/lib/utils";

const links = [
  { href: "/", label: "Acasă", exact: true },
  { href: "/products", label: "Produse", exact: false },
  { href: "/orders", label: "Comenzi", exact: false },
];

export function AppNav() {
  const pathname = usePathname();

  return (
    <header className="border-b border-neutral-200">
      <nav className="mx-auto flex max-w-6xl items-center gap-6 px-4 py-4">
        <Link
          href="/"
          className="text-lg font-semibold tracking-tight text-neutral-900"
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
      </nav>
    </header>
  );
}
