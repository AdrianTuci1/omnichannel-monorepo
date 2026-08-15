# Plan: Integrare completă Omnichannel E-commerce (gap-fill)

Obiectiv: conectează serviciile într-un flux end-to-end și adaugă feature-urile esențiale lipsă (recenzii, recomandări, adăugare produs din UI, CRUD complet, integrare bidirecțională).

## Starea actuală (gap-uri confirmate)
- store-api: 18 rute. `products` are CRUD complet; `categories`/`customers`/`orders` NU au PUT; lipsesc `reviews`, `inventory`, `related`.
- frontend (clients/web): `lib/api.ts` are doar `apiGet`+`apiDelete`; pagina principală e doar health-check; fără recenzii/recomandări/adăugare produs.
- integrations: Odoo/Akeneo scriu în store-api (unidirecțional); CDP e stub; recommender e izolat (nu comunică cu store-api/frontend).

## Contracte (forme endpoint-uri — STABILITE ÎNAINTE, ca workerii să ruleze în paralel)
- `Review`: `{id, productId, customerId, rating(1-5), title, comment, createdAt}`
  - `GET /products/{id}/reviews`, `POST /products/{id}/reviews` (body `{rating, title, comment, customerId}`), `DELETE /reviews/{id}`
- `Inventory`: `GET /products/{id}/inventory`, `PUT /products/{id}/inventory` (body `{quantityOnHand, reserved, reorderThreshold}`)
- `PUT /categories/{id}` `{name, slug, description, parentId}`; `PUT /customers/{id}` `{email, firstName, lastName, phone}`; `PUT /orders/{id}` `{status}`
- `Related`: `GET /products/{id}/related` -> `[{productId, name, score}]` (store-api face proxy către recommender)
- `Events` (outbox): `POST /events` (scrie în tabel `EventOutbox`) + `GET /events?since=...` (citește neprocesate) — pentru CDP

## Task-uri (4 leaf workeri, directoare disjuncte → paralel-safe)
1. **T1 backend** (`apps/store-api`): Review entity + endpoint-uri; inventory GET/PUT; PUT categories/customers/orders; GET /products/{id}/related (proxy recommender); outbox events; teste xUnit. Verificare: `dotnet build` + `dotnet test`.
2. **T2 frontend** (`clients/web`): `apiPost`/`apiPut`; form adăugare produs (`/products/new`); recenzii (afișare + form); „produse similare" pe pagina de produs; storefront real pe `/`. Verificare: `npm run build`.
3. **T3 recommender** (`integrations/recommender`): citește produse/comenzi din store-api (StoreApiClient); răspuns `[{productId, name, score}]` compatibil cu store-api. Verificare: `dotnet build`.
4. **T4 integrations** (`integrations/odoo|akeneo|cdp`): sincronizare inversă (store-api → Odoo status comenzi; store-api → Akeneo export produse); CDP consumă `/events`. Verificare: `dotnet build` fiecare.

## Dependențe logice (rezolvate prin contracte)
- T2 consumă endpoint-urile din T1 (forme definite mai sus).
- T1 face proxy către T3 (`/recommendations/{productId}`).
- T4 consumă `/events` din T1.
Toți rulează în paralel; fiecare implementează partea lui din contract.

## Urmărire
Orchestratorul (Root Planner) vede statusurile în `.agents/bus/contracts.json` + `.agents/state/workers.json` și marchează completed/failed la următorul tick.
