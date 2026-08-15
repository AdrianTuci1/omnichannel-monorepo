# CDP Worker (.NET 9)

Customer Data Platform în timp real pentru monorepo-ul Omnichannel E-commerce.
Consumă evenimente de domeniu din **store-api** (outbox `GET /events?since=...`, printr-un
poller periodic) și opțional din **Azure Service Bus**, apoi le scrie în **DuckDB**
(store fierbinte, query-abil) cu export periodic al snapshot-ului în format **Iceberg**
(lakehouse, pentru Spark/Trino/Athena).

## Arhitectura

```
store-api (m1) ──GET /events?since=...──▶ Cdp.Worker (poller periodic)
   (outbox)                                  │
                                             ├─ normalizează outbox → DomainEvent
                                             │
Azure Service Bus ──(opțional)───────────────┤
                                             │
                                             ├─ append  ─▶ DuckDB (data/cdp.duckdb)
                                             │              ├─ events      (event log)
                                             │              ├─ customers   (profil)
                                             │              ├─ orders      (stare)
                                             │              └─ products    (catalog)
                                             │
                                             └─ flush periodic ─▶ Iceberg (data/iceberg/<tabel>/)
```

- **Poller store-api** este calea principală: un `BackgroundService` (`StoreApiEventPoller`)
  interoghează periodic `GET /events?since=<cursor>` și livrează evenimentele normalizate
  către sink. Cursorul (timestamp) este persistat într-un fișier pentru consum incremental.
- **Service Bus** rămâne o cale opțională: dacă `ServiceBus:ConnectionString` este configurată,
  `ServiceBusEventProcessor` ascultă coada/topicul în paralel.
- **DuckDB** este sursa de adevăr a workerului: evenimentele sunt persistate imediat
  (append idempotent — `ON CONFLICT (event_id) DO NOTHING`), iar profilurile
  (`customers`, `orders`, `products`) sunt actualizate prin `UPSERT`
  (`ON CONFLICT DO UPDATE`) pentru a menține starea cea mai recentă a fiecărei entități.
- **Iceberg** este exportul analitic: la fiecare `FlushIntervalSeconds`, fiecare tabel
  nevid este rescris ca tabel Iceberg (metadata + manifest avro + date parquet) sub
  `CatalogPath/<nume_tabel>/`. Fiecare flush produce un snapshot complet și coerent.

## Consum din store-api (outbox)

`store-api` expune un outbox de evenimente la `GET /events?since=<ISO8601>`, care
returnează evenimentele produse după timestamp-ul dat. Forma unui eveniment din outbox
(EventOutbox) este:

```json
{
  "id": "4d2f0e12-...",
  "type": "ProductCreated",
  "payload": { "id": "...", "sku": "...", "name": "..." },
  "createdAt": "2026-08-15T22:10:00Z",
  "processedAt": null
}
```

Poller-ul mapează fiecare eveniment în envelope-ul intern `DomainEvent`:

| Outbox (`type`) | `entityType` | `eventType` |
|---|---|---|
| `ProductCreated` | `product` | `product.created` |
| `ProductUpdated` | `product` | `product.updated` |
| `OrderCreated` | `order` | `order.created` |
| `CustomerCreated` / `CustomerUpdated` | `customer` | `customer.created` / `customer.updated` |
| `CategoryCreated` / `CategoryUpdated` | `category` | `category.created` / `category.updated` |
| `InventoryUpdated` | `inventory` | `inventory.updated` |

Conversia este generică: prefixul entității (`Product`, `Order`, `OrderLine`, `Customer`,
`Category`, `Inventory`) devine `entityType`, iar sufixul de acțiune (`Created`, `Updated`,
`Paid`, `Shipped`, …) devine sufixul `eventType` (lowerCamel). `entityId` este extras din
`payload.id` (fallback: `id`-ul evenimentului din outbox).

Semantica este **at-least-once**: cursorul este persistat după fiecare lot procesat, iar
inserarea în `events` este idempotentă, deci re-procesarea unor evenimente la graniță nu
produce duplicate.

## Cerințe

- .NET SDK 9.0 (verificat cu 9.0.317)
- store-api accesibil (pentru poller-ul de evenimente)
- (Opțional) Acces la un namespace Azure Service Bus (coadă sau topic + subscripție)
- La prima rulare, extensia `iceberg` DuckDB este instalată automat (necesită acces
  la rețeaua de extensii DuckDB, doar prima dată).

## Build

```bash
cd integrations/cdp
dotnet build
```

## Configurare

Configurația se citește din `appsettings.json` și poate fi suprascrisă prin variabile
de mediu (convenția `Secțiune__Cheie`, de ex. `ServiceBus__ConnectionString`).

| Cheie | Tip | Implicit | Descriere |
|---|---|---|---|
| `ServiceBus:ConnectionString` | string | `""` | Connection string SAS către namespace (obligatoriu). |
| `ServiceBus:QueueName` | string | `""` | Numele cozii (mod coadă). |
| `ServiceBus:TopicName` | string | `omnichannel-events` | Numele topicului (mod topic+subscripție). |
| `ServiceBus:SubscriptionName` | string | `cdp-worker` | Numele subscripției (mod topic+subscripție). |
| `ServiceBus:MaxConcurrentCalls` | int | `1` | Câte mesaje procesează în paralel (DuckDB e single-writer, scrierile se serializează intern). |
| `ServiceBus:PrefetchCount` | int | `0` | Prefetch Service Bus (0 = decizia clientului). |
| `ServiceBus:MaxAutoLockRenewalSeconds` | int | `300` | Durata maximă de reînnoire a lock-ului. |
| `DuckDb:DatabasePath` | string | `data/cdp.duckdb` | Fișierul DuckDB (relativ la cwd-ul workerului). |
| `StoreApi:BaseUrl` | string | `http://localhost:5180` | URL-ul store-api pentru outbox-ul de evenimente. |
| `StoreApi:Enabled` | bool | `true` | Activează poller-ul de evenimente store-api. |
| `StoreApi:PollIntervalSeconds` | int | `15` | Intervalul de interogare a outbox-ului. |
| `StoreApi:CursorFilePath` | string | `data/cdp.cursor` | Fișierul în care se persistă cursorul (timestamp). |
| `Iceberg:Enabled` | bool | `true` | Activează exportul Iceberg periodic. |
| `Iceberg:CatalogPath` | string | `data/iceberg` | Directorul rădăcină al tabelelor Iceberg (suportă și `s3://bucket/prefix`). |
| `Iceberg:FlushIntervalSeconds` | int | `30` | Frecvența exportului Iceberg. |
| `Iceberg:ExportProfiles` | bool | `true` | Exportă și profilurile (customers/orders/products), nu doar `events`. |

Modul de consum Service Bus se deduce automat: dacă `QueueName` e completat se folosește
coada, altfel topic + subscripție. `ConnectionString` este secret — nu-l comiteți în repo;
folosiți o variabilă de mediu. **Service Bus este opțional**: fără `ConnectionString`,
workerul pornește folosind doar poller-ul store-api.

## Contractul de evenimente (envelope)

Fiecare mesaj Service Bus este un obiect JSON cu următoarea structură (camelCase):

```json
{
  "eventId": "4d2f0e12-...",
  "eventType": "order.paid",
  "entityType": "order",
  "entityId": "6f9619ff-...",
  "occurredAt": "2026-08-15T22:10:00Z",
  "source": "store-api",
  "correlationId": "7c9e6679-...",
  "payload": { }
}
```

| Câmp | Obligatoriu | Descriere |
|---|---|---|
| `eventId` | da | ID unic al evenimentului (UUID). |
| `eventType` | da | Tipul evenimentului (vezi catalogul de mai jos). |
| `entityType` | da | Tipul entității: `product`, `category`, `customer`, `order`, `order_line`, `inventory`. |
| `entityId` | da | ID-ul entității afectate (UUID). |
| `occurredAt` | nu (fallback: now) | Timestamp ISO 8601 al producerii evenimentului. |
| `source` | nu (fallback: `unknown`) | Producătorul evenimentului. |
| `correlationId` | nu | ID de corelare pentru trasabilitate end-to-end. |
| `payload` | nu | Snapshot-ul entității, ca obiect JSON (vezi mai jos). |

Mesajele care nu respectă envelope-ul (lipsesc câmpurile de identitate) sunt trimise
automat în **dead-letter** cu motivul `InvalidEnvelope`. Mesajele valide sunt completate
doar după ce au fost persistate în DuckDB — livrarea este **at-least-once**.

### Tipuri de evenimente (`eventType`)

- Produse: `product.created`, `product.updated`, `product.activated`, `product.deactivated`
- Categorii: `category.created`, `category.updated`
- Clienți: `customer.created`, `customer.updated`
- Comenzi: `order.created`, `order.submitted`, `order.paid`, `order.shipped`, `order.delivered`, `order.cancelled`
- Stoc: `inventory.updated`

### Payload-uri (profiluri)

Payload-urile reflectă entitățile din `apps/store-api` (m1) și sunt deserializate
case-insensitive; câmpurile lipsă sunt ignorate fără a pierde evenimentul din `events`.

- **customer**: `{ "email", "firstName", "lastName", "phone", "createdAt" }`
- **order**: `{ "orderNumber", "customerId", "status", "currency", "totalAmount", "totalCurrency", "createdAt" }`
  (`status` este numele enum-ului `OrderStatus`: `Draft`, `Pending`, `Paid`, `Shipped`, `Delivered`, `Cancelled`)
- **product**: `{ "sku", "name", "priceAmount", "priceCurrency", "categoryId", "isActive", "createdAt" }`

## Schema de ieșire DuckDB

Baza de date `data/cdp.duckdb` conține următoarele tabele.

### `events` — event log append-only

| Coloană | Tip | Descriere |
|---|---|---|
| `event_id` | VARCHAR (PK) | ID-ul evenimentului. |
| `event_type` | VARCHAR | Tipul evenimentului. |
| `entity_type` | VARCHAR | Tipul entității. |
| `entity_id` | VARCHAR | ID-ul entității. |
| `occurred_at` | TIMESTAMP | Timestamp-ul producerii (UTC). |
| `source` | VARCHAR | Producătorul. |
| `correlation_id` | VARCHAR | ID de corelare. |
| `payload` | VARCHAR | Snapshot-ul complet al entității, ca text JSON. |

Index: `idx_events_entity (entity_type, entity_id)`.

`payload` este stocat ca `VARCHAR` deoarece formatul Iceberg nu are un tip `JSON` nativ;
pentru interogări JSON în DuckDB folosiți view-ul `events_json` (care expune
`payload` ca `JSON`) sau `CAST(payload AS JSON)` / `payload::JSON`.

### `customers` — profil client (upsert)

| Coloană | Tip | Descriere |
|---|---|---|
| `customer_id` | VARCHAR (PK) | ID client. |
| `email` | VARCHAR | Email. |
| `first_name` | VARCHAR | Prenume. |
| `last_name` | VARCHAR | Nume. |
| `phone` | VARCHAR | Telefon. |
| `created_at` | TIMESTAMP | Primul timestamp observat. |
| `last_updated_at` | TIMESTAMP | Timestamp-ul ultimului eveniment. |

### `orders` — stare comandă (upsert)

| Coloană | Tip | Descriere |
|---|---|---|
| `order_id` | VARCHAR (PK) | ID comandă. |
| `order_number` | VARCHAR | Număr comandă. |
| `customer_id` | VARCHAR | ID client. |
| `status` | VARCHAR | Stare curentă (nume `OrderStatus`). |
| `currency` | VARCHAR | Monedă. |
| `total_amount` | DECIMAL(18,2) | Total comandă. |
| `created_at` | TIMESTAMP | Primul timestamp observat. |
| `last_updated_at` | TIMESTAMP | Timestamp-ul ultimului eveniment. |

### `products` — catalog produse (upsert)

| Coloană | Tip | Descriere |
|---|---|---|
| `product_id` | VARCHAR (PK) | ID produs. |
| `sku` | VARCHAR | SKU. |
| `name` | VARCHAR | Nume. |
| `price_amount` | DECIMAL(18,2) | Preț. |
| `price_currency` | VARCHAR | Monedă preț. |
| `category_id` | VARCHAR | Categorie. |
| `is_active` | BOOLEAN | Activ/inactiv. |
| `created_at` | TIMESTAMP | Primul timestamp observat. |
| `last_updated_at` | TIMESTAMP | Timestamp-ul ultimului eveniment. |

Evenimentele de tip `category.*` și `inventory.updated` sunt păstrate integral în
`events` (event log = sursa de adevăr); modelarea lor analitică revine stratului dbt
(worker separat, `data-pipelines`).

## Structura de ieșire Iceberg

Sub `CatalogPath` fiecare tabel exportat devine un tabel Iceberg independent:

```
data/iceberg/
├── events/
│   ├── metadata/
│   │   ├── <uuid>.metadata.json      # metadatele tabelului Iceberg
│   │   ├── snap-<id>-<uuid>.avro     # manifest list (snapshot)
│   │   ├── <uuid>-m0.avro            # manifest de date
│   │   └── version-hint.text
│   └── data/
│       └── <uuid>.parquet            # date coloanare
├── customers/
├── orders/
└── products/
```

Tabelele sunt citibile direct din DuckDB (`iceberg_scan('data/iceberg/events')`) și din
orice motor compatibil Iceberg (Spark, Trino, Athena, Flink).

## Rulare

```bash
cd integrations/cdp
dotnet run
```

Fără alte configurări, workerul pornește cu poller-ul store-api activ (consum din
`GET /events?since=...`) și exportul Iceberg periodic. Pentru consum suplimentar din
Service Bus, setați `ServiceBus__ConnectionString` (și `QueueName` sau
`TopicName`+`SubscriptionName`):

```bash
export ServiceBus__ConnectionString="Endpoint=sb://...;SharedAccessKeyName=...;SharedAccessKey=..."
dotnet run
```

Pentru o rulare locală, exportul Iceberg poate fi validat independent prin orice client
DuckDB (vezi `SELECT * FROM iceberg_scan('data/iceberg/events')`).

## Decizii de proiectare

- **DuckDB single-writer**: toate scrierile trec printr-un lock intern; `MaxConcurrentCalls`
  poate fi crescut, dar scrierile rămân serializate (corect pentru DuckDB).
- **Poller cu cursor persistent**: consumul din outbox este incremental (cursor timestamp
  într-un fișier) și reia de unde a rămas după restart. Inserarea evenimentelor este
  idempotentă (`ON CONFLICT DO NOTHING`), deci at-least-once nu produce duplicate.
- **At-least-once**: mesajele Service Bus sunt completate doar după persistare; la erori
  tranzitorii rămân blocate și sunt re-livrate, apoi dead-letter după `MaxDeliveryCount`.
- **Snapshot Iceberg per flush**: exportul rescrie întregul tabel (nu delta incremental);
  potrivit pentru volumul curent, simplu și atomic. Pentru volume mari se poate comuta pe
  append incremental specific motorului.
