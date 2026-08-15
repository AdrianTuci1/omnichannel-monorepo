# Omnichannel E-commerce — Monorepo

Platformă **Omnichannel E-commerce** de producție: un singur backend de domeniu
(`StoreApi`) și mai multe canale de vânzare (web, Android, POS), integrări
enterprise (Odoo, Akeneo, CDP, recomandări) și layer-e de date/operațiuni (dbt,
Terraform, Helm).

Documentația este generată și menținută de **Docs Worker** pe baza blackboard-ului
din `.agents/bus/contracts.json` și a codului real din repository.

- [API — referință completă](api.md)
- [Arhitectură & flux de date](architecture.md)

---

## 1. Structura repository-ului

```
omnichannel-monorepo/
├── apps/
│   └── store-api/                 # Backend .NET 9 (m1) ✅
│       ├── store-api.sln
│       ├── src/
│       │   ├── StoreApi.Domain/         # entități, value objects, reguli business
│       │   ├── StoreApi.Infrastructure/ # EF Core, persistență, căutare vectorială
│       │   └── StoreApi.Api/            # Minimal API (18 rute), DTO-uri
│       └── tests/StoreApi.Tests/        # xUnit (domeniu + integrare API)
├── clients/
│   ├── web/                       # Next.js 15 + Tailwind v4 + shadcn/ui (m2) ✅
│   ├── android/                   # Kotlin + Jetpack Compose + Room (m2)
│   └── pos/                       # React + Vite, creare rapidă comenzi (m2)
├── integrations/
│   ├── odoo/                      # bridge Odoo ERP, JSON-RPC (m3)
│   ├── akeneo/                    # connector PIM Akeneo (m3)
│   ├── cdp/                       # CDP real-time, Service Bus → DuckDB/Iceberg (m3)
│   └── recommender/               # recomandări hibride pgvector (m3)
├── data-pipelines/                # dbt, Bronze/Silver/Gold (m4)
├── infra/                         # Terraform Azure (AKS, PostgreSQL, Redis, SB) (m4)
├── helm/                          # Helm chart store-api (m4)
├── docs/                          # această documentație
└── .agents/                       # orchestrare build (blackboard + leaf workers)
```

---

## 2. Cerințe

| Dependență | Versiune | Necesar pentru |
|-----------|----------|----------------|
| .NET SDK | 9.0.x | `apps/store-api`, `integrations/*` |
| Node.js + npm | 18+ | `clients/web`, `clients/pos` |
| PostgreSQL + pgvector | 15+ | producție (opțional; local se folosește InMemory) |
| JDK + Android SDK | 17+ | `clients/android` (opțional la build) |
| dbt | 1.x | `data-pipelines` (opțional) |
| Terraform | 1.x | `infra` (opțional) |
| Helm | 3.x | `helm` (opțional) |

---

## 3. Cum rulezi fiecare serviciu

### 3.1 Store API — `apps/store-api` ✅

```bash
cd apps/store-api
dotnet restore
dotnet build

# rulare locală (InMemory, port configurabil):
dotnet run --project src/StoreApi.Api --urls http://localhost:5000
```

Alternativ, portul se setează prin variabila de mediu `ASPNETCORE_URLS`:

```bash
ASPNETCORE_URLS=http://localhost:5000 dotnet run --project src/StoreApi.Api
```

La pornire, se creează schema și se inserează categoria implicită „General".
Verifică sănătatea:

```bash
curl http://localhost:5000/health   # → {"status":"ok"}
```

**Teste:**

```bash
cd apps/store-api
dotnet test
```

Testele acoperă domeniul (invariante, mașina de stări a comenzilor) și API-ul
(integrare prin `WebApplicationFactory`).

> Providerul de persistență este ales în `Program.cs`:
> `UseInMemoryDatabase("StoreApi")`. Pentru PostgreSQL + pgvector, înlocuiește
> înregistrarea DbContext cu `UseNpgsql(...)` — maparea (`StoreDbContext`)
> suportă deja ambele provider-e (vezi `architecture.md` §3.1).

### 3.2 Web client — `clients/web` ✅

```bash
cd clients/web
npm install

# configurare base URL (opțional; default http://localhost:5000):
cp .env.local.example .env.local

# dev server:
npm run dev        # → http://localhost:3000

# producție:
npm run build
npm run start
```

Pagini: `/` (status conexiune), `/products`, `/products/[id]`, `/orders`,
`/orders/[id]`. Paginile de date fac fetch în browser, deci `npm run build`
trece și fără ca API-ul să ruleze.

### 3.3 Integrare Odoo — `integrations/odoo` (m3, în construcție)

Bridge .NET 9 (`OdooBridge`) care sincronizează produse/comenzi cu Odoo prin
JSON-RPC. Configurarea se face în `appsettings.json`:

- `Odoo` — `BaseUrl`, `Database`, `Username`, `ApiKey`, modelele Odoo
  (`product.template`, `sale.order`, `res.partner`), `PageSize`.
- `StoreApi` — `BaseUrl` al backend-ului.
- `Sync` — `IntervalSeconds` (default 300), flag-uri `ProductsEnabled` /
  `OrdersEnabled`.

Compilare (după finalizarea workerului odoo):

```bash
cd integrations/odoo
dotnet build
```

### 3.4 Celelalte servicii (roadmap)

Serviciile de mai jos sunt definite în blackboard (`.agents/bus/contracts.json`)
și construite de leaf workeri dedicați. Comenzile reprezintă modul de rulare
canonic, conform stivei fiecăruia:

| Serviciu | Director | Stivă | Milestone | Comandă de rulare/validare |
|----------|----------|-------|-----------|---------------------------|
| Android client | `clients/android` | Kotlin, Jetpack Compose, Room | m2 | `./gradlew assembleDebug` |
| POS client | `clients/pos` | React, Vite | m2 | `npm run build` |
| Akeneo connector | `integrations/akeneo` | .NET 9 | m3 | `dotnet build` |
| CDP | `integrations/cdp` | .NET 9 + Azure Service Bus + DuckDB/Iceberg | m3 | `dotnet build` |
| Recommender | `integrations/recommender` | .NET 9 Web + pgvector | m3 | `dotnet build` |
| Data pipelines | `data-pipelines` | dbt | m4 | `dbt compile` |
| Infrastructure | `infra` | Terraform (Azure) | m4 | `terraform validate` |
| Helm chart | `helm` | Helm (Kubernetes) | m4 | `helm lint` |

---

## 4. Procesul de build (auto-construire)

Monorepo-ul se construiește autonom printr-o orchestră de agenți coordonată prin
blackboard:

1. **Root Planner** (`.agents/plans/root_planner.prompt`) rulează recurent,
   citește `.agents/bus/contracts.json` și `.agents/memory.json`.
2. Pentru fiecare contract leaf cu status `ready`, dispecerizează **detached** un
   worker dedicat (`dispatch_worker.py <worker> <prompt_file>`), câte unul per
   serviciu, în paralel.
3. Fiecare leaf worker scrie în `target_dir`-ul său și își raportează statusul
   în `.agents/state/workers.json` (`running`/`done`/`failed`/`timeout`).
4. Plannerul marchează contractul `completed` când workerul raportează `done`,
   sau îl re-pune `ready` la eșec (o singură dată, cu lecție în
   `memory.json.lessons`).

Statusul curent al milestone-urilor și contractelor se află în
`.agents/bus/contracts.json` (sursa de adevăr) și `.agents/memory.json`.

---

## 5. Milestone-uri

| Milestone | Conținut | Status |
|-----------|----------|--------|
| **m1** | Domain core (.NET 9 + EF Core + PostgreSQL): `apps/store-api` | ✅ completed |
| **m2** | Clienți (Next.js, Android, POS): `clients/*` | în curs |
| **m3** | Integrări (Odoo, Akeneo, CDP, Recommender): `integrations/*` | în curs |
| **m4** | Date & Ops (dbt, Terraform, Helm): `data-pipelines`, `infra`, `helm` | în curs |
| **docs** | Documentație: `docs/` | în curs |

---

## 6. Convenții

- **Limbaj:** comentariile de cod și documentația sunt în română; identificatorii
  de cod sunt în engleză.
- **Wire-format:** JSON cu `camelCase`; valorile monetare ca perechi
  `*Amount`/`*Currency`; enum-urile serializate ca string.
- **Fără placeholder-uri:** niciun artefact din repo nu conține `TODO`/stub-uri;
  fiecare worker livrează implementări complete.
- **Contracte:** schema entităților și rutele sunt fixate în
  `.agents/bus/contracts.json` și nu se modifică de către leaf workeri.
