# Arhitectură — Omnichannel E-commerce

Acest document descrie componentele monorepo-ului, relațiile dintre ele și
fluxul de date end-to-end.

---

## 1. Prezentare generală

Monorepo-ul implementează o platformă **Omnichannel E-commerce** care
centralizează un singur backend de domeniu (`StoreApi`) și expune multiple
canale de vânzare (web, mobil, POS) peste el, plus o serie de integrări
enterprise (ERP, PIM, CDP, recomandări) și layer-ele de date/operațiuni (dbt,
Terraform, Helm).

Principiile de design:

- **Un singur domeniu, canale multiple** — `StoreApi` este sursa de adevăr
  pentru produse, comenzi, clienți, categorii și stoc; toți clienții consumă
  același contract HTTP.
- **Arhitectură curată pe verticală** — `StoreApi` este împărțit în `Domain`
  (entități + reguli de business), `Infrastructure` (persistență, căutare) și
  `Api` (expunere HTTP). Domeniul nu depinde de infrastructură.
- **Persistență duală** — InMemory pentru rulare locală/rapidă, PostgreSQL +
  pgvector pentru producție. Comutarea se face la nivel de provider EF Core,
  fără schimbarea codului de domeniu.
- **Extensibilitate prin integrări decuplate** — Odoo, Akeneo, CDP și
  recommender sunt procese separate care comunică prin HTTP/Service Bus, nu
  cuplaje în codul `StoreApi`.

---

## 2. Diagrama componentelor

```
                          ┌──────────────────────────────────────────────┐
                          │               CLIENTS  (m2)                 │
                          │                                              │
                          │  ┌──────────┐ ┌──────────────┐ ┌─────────┐  │
                          │  │  Web     │ │   Android    │ │   POS   │  │
                          │  │ Next.js  │ │ Kotlin+Room  │ │ React+  │  │
                          │  │   15     │ │   offline    │ │  Vite   │  │
                          │  └────┬─────┘ └──────┬───────┘ └────┬────┘  │
                          └───────┼──────────────┼──────────────┼───────┘
                                  │   HTTP/JSON  │              │
                                  └──────────────┼──────────────┘
                                                 ▼
                          ┌──────────────────────────────────────────────┐
                          │             StoreApi  (m1)  ✅              │
                          │                                              │
                          │   StoreApi.Api (Minimal API, 18 rute)        │
                          │        │  DTO-uri (Contracts.cs)             │
                          │        ▼                                     │
                          │   StoreApi.Domain                            │
                          │   Product · Category · Customer · Order      │
                          │   OrderLine · Inventory · Money · OrderStatus│
                          │        │                                     │
                          │        ▼                                     │
                          │   StoreApi.Infrastructure                    │
                          │   StoreDbContext (EF Core)                   │
                          │   HashingEmbeddingService · VectorSearch     │
                          └───────┬───────────────────────┬──────────────┘
                                  │                       │
                     InMemory (local)          PostgreSQL + pgvector (prod)
                                              (products, orders, ...,
                                               product_embeddings vector(384))
```

```
   ┌────────────────────────────────────────────────────────────────────┐
   │                 INTEGRATIONS  (m3)                                │
   │                                                                    │
   │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────┐ │
   │  │ Odoo (ERP)   │  │ Akeneo (PIM) │  │ CDP (events) │  │Recomm. │ │
   │  │ .NET 9       │  │ .NET 9       │  │ Service Bus→ │  │.NET 9 +│ │
   │  │ JSON-RPC     │  │ REST         │  │ DuckDB/      │  │pgvector│ │
   │  │ bridge       │  │ connector    │  │ Iceberg      │  │ hybrid │ │
   │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └───┬────┘ │
   └─────────┼─────────────────┼─────────────────┼──────────────┼───────┘
             │ sync produse/   │ sync produse/   │ consumă      │ interoghează
             │ comenzi         │ atribute        │ evenimente   │ embeddings
             ▼                 ▼                 ▼              ▼
        Odoo SaaS         Akeneo SaaS      Azure Service Bus   PostgreSQL
```

```
   ┌────────────────────────────────────────────────────────────────────┐
   │              DATA & OPS  (m4)                                     │
   │                                                                    │
   │   data-pipelines/   dbt (Bronze/Silver/Gold → DuckDB/Iceberg)      │
   │   infra/            Terraform (Azure AKS, PostgreSQL Flexible,     │
   │                     Redis, Service Bus)                            │
   │   helm/             Helm chart store-api (Deployment, Service,     │
   │                     Ingress, HPA)                                  │
   └────────────────────────────────────────────────────────────────────┘
```

---

## 3. Descrierea componentelor

### 3.1 `apps/store-api` — backend-ul de domeniu (m1, finalizat)

Soluție .NET 9 cu trei proiecte + teste:

| Proiect | Rol |
|---------|-----|
| `StoreApi.Domain` | entități, value objects, enum-uri, reguli de business (invariante, mașina de stări a comenzilor) |
| `StoreApi.Infrastructure` | EF Core `StoreDbContext`, mapare persistență, căutare vectorială (pgvector), serviciu de embedding |
| `StoreApi.Api` | Minimal API — 18 rute, DTO-uri de request/response, seeding |
| `StoreApi.Tests` | xUnit + `WebApplicationFactory` — teste de domeniu și teste de integrare API |

Pachete cheie: `Microsoft.EntityFrameworkCore*` 9.0.4,
`Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4, `Pgvector` 0.3.2,
`Pgvector.EntityFrameworkCore` 0.3.0, `RedLock.net`, `Azure.Messaging.ServiceBus`.

**Invariante de domeniu** (implementate în entități, nu în controllers):

- `Money.Amount` nu poate fi negativ; operațiile `Add`/`Multiply` validează
  moneda identică.
- `Product.Sku` este obligatoriu și normalizat uppercase; `Name` obligatoriu.
- `Customer.Email` trebuie să conțină `@` și este normalizat lowercase.
- `Order` pornește `Draft`; tranzițiile respectă mașina de stări
  (`Submit→Pending`, `MarkPaid→Paid`, `MarkShipped→Shipped`,
  `MarkDelivered→Delivered`, `Cancel` — o comandă livrată nu poate fi anulată,
  iar o comandă goală nu poate fi trimisă).
- `OrderLine.Quantity > 0`; `LineTotal = UnitPrice × Quantity`.
- `Inventory.Available = QuantityOnHand − Reserved`; rezervarea nu poate depăși
  disponibilul.

### 3.2 `clients/` — canalele de vânzare (m2)

- `clients/web` — Next.js 15 (App Router) + Tailwind CSS v4 + shadcn/ui.
  Pagini: start, listă/detaliu produse, listă/detaliu comenzi. Consumă API-ul
  prin fetch în browser (`NEXT_PUBLIC_API_BASE_URL`, default
  `http://localhost:5000`).
- `clients/android` — Kotlin + Jetpack Compose + Room (cache offline).
- `clients/pos` — React + Vite, ecran de creare rapidă a comenzilor (coș +
  total).

### 3.3 `integrations/` — integrări enterprise (m3)

- `integrations/odoo` — bridge .NET 9 către Odoo ERP prin JSON-RPC
  (`OdooBridge`). Sincronizează produse/comenzi; configurare prin `appsettings.json`
  (secțiunile `Odoo`, `StoreApi`, `Sync`).
- `integrations/akeneo` — connector PIM Akeneo (sincronizare produse/atribute).
- `integrations/cdp` — consumă evenimente din Azure Service Bus și scrie în
  DuckDB/Iceberg.
- `integrations/recommender` — microserviciu de recomandări hibride
  (content-based cu pgvector + collaborative de bază).

### 3.4 `data-pipelines/`, `infra/`, `helm/` (m4)

- `data-pipelines/` — proiect dbt cu modelarea Bronze/Silver/Gold pentru
  `orders`, `products`, `customers` (DuckDB/Iceberg).
- `infra/` — Terraform pentru Azure (AKS multi-AZ, PostgreSQL Flexible Server,
  Redis, Service Bus).
- `helm/` — chart Helm pentru `store-api` (Deployment, Service, Ingress, HPA).

### 3.5 `.agents/` — orchestarea build-ului (nu face parte din produs)

Directorul `.agents/` conține infrastructura de auto-construire a monorepo-ului:

- `bus/contracts.json` — blackboard-ul cu planul de milestone-uri și contractele
  leaf workerilor (sursa de adevăr a planului).
- `bus/rpc.json` — coada de comunicare inter-worker.
- `state/workers.json` — statusul fiecărui worker (`running`/`done`/`failed`).
- `plans/*.prompt` — instrucțiunile fiecărui leaf worker.
- `dispatch_worker.py` / `run_worker.py` — dispecerizarea detached a workerilor.
- `memory.json` — starea root planner (milestone-uri, lecții).

**Model de execuție:** Root Planner rulează recurent, citește blackboard-ul și
dispecerizează în paralel leaf workerii `ready` (unul per serviciu), fără a
aștepta finalizarea. Fiecare worker scrie în directorul său `target_dir` și își
raportează statusul în `workers.json`; plannerul marchează contractele
`completed` la `done`.

---

## 4. Fluxul de date

### 4.1 Fluxul de bază (produse & comenzi)

```
[Client] --POST /products--> [StoreApi.Api] --new Product(...)--> [Domain]
   ^                              │                                  │
   │                              ▼                                  ▼
   │                     [StoreDbContext] ◄── EF Core ──── [PostgreSQL/InMemory]
   │                              │
   └──── GET /products ───  mapare ToProductResponse (Contracts.cs) ──┘
```

1. Un client (web/mobil/POS) trimite o cerere HTTP către `StoreApi.Api`.
2. Ruta minimală validează inputul (DTO) și invocă entitatea de domeniu, care
   aplică regulile de business și invariantele.
3. `StoreDbContext` persistă prin EF Core în providerul activ (InMemory sau
   PostgreSQL).
4. Răspunsul se construiește prin mapare către DTO-urile de răspuns
   (`ProductResponse`, `OrderResponse`, etc.), nu prin expunerea entităților.

### 4.2 Fluxul comenzilor (calcul total)

La `POST /orders`, pentru fiecare linie se citește prețul curent al produsului;
`OrderLine.LineTotal = UnitPrice × Quantity`, iar `Order.Total` este suma
`LineTotal` (calculat la citire, nu stocat). `OrderNumber` se generează în
format `ORD-yyyyMMdd-XXXXXXXX`.

### 4.3 Fluxul de integrare (m3)

- **Odoo/Akeneo** sincronizează periodic (`Sync.IntervalSeconds`, default 300s)
  produsele și comenzile între `StoreApi` și sistemele externe prin REST/JSON-RPC.
- **CDP** consumă evenimente din Azure Service Bus și le materializează în
  DuckDB/Iceberg pentru analiză.
- **Recommender** interoghează `product_embeddings` din PostgreSQL (cosine
  distance pgvector) pentru recomandări content-based, combinate cu semnale
  collaborative.

### 4.4 Fluxul de date analitice (m4)

dbt prelucrează datele în trei etape: **Bronze** (raw), **Silver** (curățat,
tipizat), **Gold** (agregări de business), pe DuckDB/Iceberg.

---

## 5. Tehnologii

| Strat | Tehnologie |
|-------|-----------|
| Backend | .NET 9, C# 13, ASP.NET Core Minimal API |
| Persistență | EF Core 9, PostgreSQL 15+, pgvector 0.3 |
| Cache local | InMemory (EF Core provider) |
| Web | Next.js 15, React 19, Tailwind CSS v4, shadcn/ui |
| Mobil | Kotlin, Jetpack Compose, Room |
| POS | React, Vite |
| Integrări | .NET 9 (Odoo/Akeneo/CDP/Recommender), Azure Service Bus |
| Date | dbt, DuckDB, Iceberg |
| Ops | Terraform (Azure), Helm (Kubernetes), Redis, AKS |

---

## 6. Decizii și compromisuri

- **InMemory implicit, Postgres în prod** — permite rulare locală fără
  dependențe și teste rapide; diferențele de provider sunt izolate în
  `StoreDbContext` (owned vs. complex types, vector ca string vs. `vector(384)`).
- **Prețul snapshot pe `OrderLine`** — comanda păstrează `ProductName` și
  `UnitPrice` la momentul plasării, deci modificările ulterioare ale produsului
  nu alterează istoricul comenzilor.
- **Calcul vs. stocare** — `Total`, `LineTotal` și `Available` sunt calculate,
  nu persistate, evitând stări inconsistente.
- **Mașina de stări în domeniu, nu în API** — tranzițiile de status sunt reguli
  de business în `Order`; expunerea lor HTTP este amânată după m1.
