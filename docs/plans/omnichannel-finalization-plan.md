# Plan: Finalizare Omnichannel E-commerce (până la capăt)

Obiectiv: ducem monorepo-ul de la „servicii izolate care compilează" la un sistem e-commerce integrat, rulabil end-to-end, cu autentificare distribuită, și gata de GitHub.

## Faze

### Faza 1 — Backend: auth distribuit + search + cart + migrări (store-api)
Worker: `backend2_worker` (apps/store-api).

Contracte (stabilite înainte, ca workerii să ruleze în paralel):
- **Auth (JWT + refresh tokens în Redis — distribuit):**
  - `POST /auth/register` `{email, password, firstName, lastName}` -> `201 {userId}`
  - `POST /auth/login` `{email, password}` -> `200 {accessToken, refreshToken, expiresIn}`
  - `POST /auth/refresh` `{refreshToken}` -> `200 {accessToken, refreshToken, expiresIn}`
  - `POST /auth/logout` `{refreshToken}` -> `204` (invalidează refresh-ul)
  - Access token: JWT HS256 (secret din config), TTL 15 min. Refresh token: opac, stocat în Redis `refresh:<token>` = userId, TTL 7 zile. Parole: BCrypt.
  - `[Authorize]` pe endpoint-urile mutaționale (products POST/PUT/DELETE, orders, reviews, cart).
  - Redis prin `StackExchange.Redis` (IDistributedCache) — astfel mai multe instanțe API împart starea de auth.
- **Cart (Redis, per user):** `GET /cart`, `POST /cart/items` `{productId, quantity}`, `PUT /cart/items/{productId}` `{quantity}`, `DELETE /cart/items/{productId}`.
- **Search:** `GET /products/search?q=...` (ILIKE pe name/sku/description — funcționează și pe InMemory, și pe PostgreSQL).
- **Migrări EF Core:** `Microsoft.EntityFrameworkCore.Design` + migrare `InitialCreate` pentru PostgreSQL (Npgsql) + `CREATE EXTENSION vector`.
- Verificare: `dotnet build` + `dotnet test` (teste noi pentru auth/search/cart).

### Faza 1 — Frontend: auth + cart + search + admin (clients/web)
Worker: `frontend2_worker` (clients/web). Rulează în paralel cu backend2 (directoare disjuncte), consumând contractele de mai sus.
- Pagini login/register + stocare token (localStorage) + apeluri autentificate (Authorization: Bearer).
- Coș: buton „adaugă în coș" + pagină /cart + checkout (creează comandă din coș).
- Search box în header (GET /products/search).
- Admin: pagină /admin cu liste produse/comenzi/recenzii + ștergere.
- Verificare: `npm run build`.

### Faza 2 — Infra: docker-compose + seed (repo root)
Worker: `infra2_worker`.
- `docker-compose.yml`: postgres:16+pgvector, redis:7, store-api, recommender, cdp, frontend; healthchecks + volumes.
- `.env.example` (JWT secret, connection strings, baze URL).
- Script/migrare seed: produse + categorii de test.
- Verificare: `docker compose config` (sau YAML valid) + README cu pași de pornire.

### Faza 3 — Verificare end-to-end
- Pornește stack-ul (`docker compose up`) și rulează un smoke test: register → login → create product → search → add to cart → create order → review → related products.
- Repară ce pică.

### Faza 4 — GitHub + CI
- git init + .gitignore + commit inițial (făcut în faza de setup).
- Creare repo GitHub (gh CLI) + push.
- `GitHub Actions` workflow: build + test store-api, build frontend.

## Ce NU pot face complet (necesită resurse externe)
- Odoo/Akeneo/Azure Service Bus/`terraform apply` — codul există, dar validarea reală necesită instanțe + credențiale.
- Plata (Stripe etc.) — necesită cont.

## Urmărire
Orchestratorul vede statusurile în `.agents/bus/contracts.json` + `.agents/state/workers.json`.
