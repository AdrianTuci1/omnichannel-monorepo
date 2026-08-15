# Omnichannel Web (clients/web)

Client web pentru **StoreApi** (milestone m1). Construit cu **Next.js 15** (App
Router), **Tailwind CSS v4** și componente **shadcn/ui** (stil monochrome, flat).

## Rute

- `/` — pagină de start cu statusul conexiunii la API (GET `/health`).
- `/products` — lista produselor (GET `/products`).
- `/products/[id]` — detaliu produs (GET `/products/{id}`).
- `/orders` — lista comenzilor (GET `/orders`).
- `/orders/[id]` — detaliu comandă + linii (GET `/orders/{id}`).

## Configurare API

Base URL-ul API-ului se configurează prin variabila de mediu
`NEXT_PUBLIC_API_BASE_URL` (default `http://localhost:5000`).

```bash
cp .env.local.example .env.local
# editează .env.local dacă API-ul rulează pe alt host/port
```

## Build & rulare

```bash
npm install
npm run build   # build de producție (trebuie să treacă)
npm run dev     # dev server la http://localhost:3000
```

Notă: paginile de date sunt client components și fac fetch în browser, deci
`npm run build` trece și fără ca API-ul să ruleze în timpul build-ului.

## Contracte API consumate

Toate câmpurile reflectă wire-formatul JSON (camelCase) definit în
`apps/store-api/src/StoreApi.Api/Contracts.cs` (vezi și
`.agents/bus/contracts.json` → `cmd-m1-domain.result.schema`):

- `ProductResponse`: `id, sku, name, description, priceAmount, priceCurrency, categoryId, isActive, createdAt`
- `OrderResponse`: `id, orderNumber, customerId, status, currency, notes, totalAmount, totalCurrency, createdAt, lines[]`
- `OrderLineResponse`: `id, productId, productName, quantity, unitPriceAmount, unitPriceCurrency, lineTotalAmount, lineTotalCurrency`
