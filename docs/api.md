# StoreApi — Referință API

Backend-ul monorepo-ului Omnichannel E-commerce (`apps/store-api`, milestone **m1**).

- **Tehnologie:** .NET 9 (C# 13), ASP.NET Core Minimal API, EF Core 9
- **Persistență:** EF Core InMemory (implicit, local) / PostgreSQL + pgvector (producție)
- **Format:** JSON, convenție `camelCase`
- **Sursă de adevăr:** `apps/store-api/src/StoreApi.Api/Program.cs` (rute) și
  `apps/store-api/src/StoreApi.Api/Contracts.cs` (contracte wire-format)

---

## 1. Generalități

### Base URL

Implicit `http://localhost:5000`. Portul este configurabil la pornire (vezi
`README.md`, secțiunea „Store API"). Toate căile de mai jos sunt relative la
base URL.

### Convenții

- Toate payload-urile sunt `application/json` (request și response).
- Identificatorii sunt `Guid` (UUID) și se transmit ca string.
- Valorile monetare se despart în două câmpuri: `*Amount` (decimal) și
  `*Currency` (string ISO, 3 litere, uppercase).
- Răspunsurile de listare returnează un array JSON la rădăcină (nu un obiect
  învelitor).
- Data/timpul se serializează ISO 8601 (UTC).

### Coduri de status

| Cod | Semnificație |
|-----|--------------|
| `200 OK` | GET listă/detaliu, `PUT /products/{id}` |
| `201 Created` | POST (cu header `Location` spre resursa nou-creată) |
| `204 No Content` | DELETE reușit |
| `400 Bad Request` | validare eșuată (mesaj text în body) |
| `404 Not Found` | resursă inexistentă (id necunoscut) |

---

## 2. Health

### `GET /health`

Verificare liveness. Nu necesită autentificare.

**Response `200`:**

```json
{ "status": "ok" }
```

---

## 3. Categories

### `GET /categories`

Listează toate categoriile, ordonate alfabetic după `name`.

**Response `200`** — array de `CategoryResponse`:

```json
[
  {
    "id": "a1b2c3d4-...",
    "name": "General",
    "slug": "general",
    "description": "Default category",
    "parentId": null
  }
]
```

### `GET /categories/{id}`

Detaliu categorie.

**Response `200`** — `CategoryResponse` (vezi mai sus).
**`404`** dacă id-ul nu există.

### `POST /categories`

Creează o categorie.

**Request body** (`CreateCategoryRequest`):

| Câmp | Tip | Obligatoriu | Descriere |
|------|-----|-------------|-----------|
| `name` | string | da | numele categoriei (trim, non-gol) |
| `slug` | string | nu | dacă lipsește, se generează din `name` (lowercase, spații → `-`) |
| `description` | string | nu | descriere liberă |
| `parentId` | Guid | nu | părinte pentru ierarhii |

```json
{ "name": "Electronice", "description": "Produse electronice" }
```

**Response `201`** — `CategoryResponse`, cu `Location: /categories/{id}`.

### `DELETE /categories/{id}`

Șterge o categorie.

**Response `204`**; **`404`** dacă nu există.

---

## 4. Customers

### `GET /customers`

Listează clienții, ordonați după `email`.

**Response `200`** — array de `CustomerResponse`:

```json
[
  {
    "id": "a1b2c3d4-...",
    "email": "ana@example.com",
    "firstName": "Ana",
    "lastName": "Popescu",
    "phone": "+40...",
    "createdAt": "2026-08-15T20:00:00Z"
  }
]
```

### `GET /customers/{id}`

Detaliu client. **`200`** cu `CustomerResponse`; **`404`** dacă nu există.

### `POST /customers`

Creează un client. Email-ul este validat (trebuie să conțină `@`) și normalizat
(lowercase, trim).

**Request body** (`CreateCustomerRequest`):

| Câmp | Tip | Obligatoriu | Descriere |
|------|-----|-------------|-----------|
| `email` | string | da | valid, unic la nivel de tabel |
| `firstName` | string | da | |
| `lastName` | string | da | |
| `phone` | string | nu | |

```json
{ "email": "ana@example.com", "firstName": "Ana", "lastName": "Popescu", "phone": "+40700000000" }
```

**Response `201`** — `CustomerResponse`, cu `Location: /customers/{id}`.

### `DELETE /customers/{id}`

Șterge un client. **`204`**; **`404`** dacă nu există.

---

## 5. Products

### `GET /products`

Listează **doar produsele active** (`isActive == true`), ordonate după `name`.

**Response `200`** — array de `ProductResponse`:

```json
[
  {
    "id": "a1b2c3d4-...",
    "sku": "SKU-001",
    "name": "Laptop 15\"",
    "description": "Laptop performant",
    "priceAmount": 3499.90,
    "priceCurrency": "RON",
    "categoryId": "c3d4e5f6-...",
    "isActive": true,
    "createdAt": "2026-08-15T20:00:00Z"
  }
]
```

### `GET /products/{id}`

Detaliu produs. Returnează produsul indiferent de `isActive`.
**`200`** cu `ProductResponse`; **`404`** dacă nu există.

### `POST /products`

Creează un produs. Dacă `categoryId` lipsește, se folosește categoria implicită
(prima alfabetic; la pornire se inserează „General"). Dacă nu există nicio
categorie, se returnează `400`.

**Request body** (`CreateProductRequest`):

| Câmp | Tip | Obligatoriu | Descriere |
|------|-----|-------------|-----------|
| `sku` | string | da | normalizat uppercase + trim |
| `name` | string | da | trim, non-gol |
| `priceAmount` | decimal | da | ≥ 0 |
| `priceCurrency` | string | da | ISO, normalizat uppercase |
| `description` | string | nu | |
| `categoryId` | Guid | nu | dacă lipsește → categoria implicită |

```json
{
  "sku": "SKU-001",
  "name": "Laptop 15\"",
  "priceAmount": 3499.90,
  "priceCurrency": "RON",
  "description": "Laptop performant",
  "categoryId": "c3d4e5f6-..."
}
```

**Response `201`** — `ProductResponse`, cu `Location: /products/{id}`.
**`400`** dacă `categoryId` e invalid sau lipsește categorie implicită.

### `PUT /products/{id}`

Actualizează complet un produs (nume, preț, categorie, descriere). Nu modifică
`sku` și nu schimbă `isActive` (vezi nota de mai jos).

**Request body** (`UpdateProductRequest`):

| Câmp | Tip | Obligatoriu | Descriere |
|------|-----|-------------|-----------|
| `name` | string | da | |
| `priceAmount` | decimal | da | ≥ 0 |
| `priceCurrency` | string | da | |
| `categoryId` | Guid | da | categoria trebuie să existe |
| `description` | string | nu | |

**Response `200`** — `ProductResponse` actualizat (cu `updatedAt` reîmprospătat).
**`404`** dacă produsul nu există; **`400`** dacă `categoryId` e invalid.

> Notă: entitatea are metode `Deactivate()`/`Activate()` pentru `isActive`, dar
> m1 nu expune rute HTTP pentru activare/dezactivare — `isActive` se setează
> `true` la creare și rămâne nemodificat prin `PUT`.

### `DELETE /products/{id}`

Șterge un produs. **`204`**; **`404`** dacă nu există.

---

## 6. Orders

### `GET /orders`

Listează comenzile cu liniile incluse, ordonate descrescător după `createdAt`.

**Response `200`** — array de `OrderResponse`:

```json
[
  {
    "id": "a1b2c3d4-...",
    "orderNumber": "ORD-20260815-1A2B3C4D",
    "customerId": "e5f6a7b8-...",
    "status": "Draft",
    "currency": "RON",
    "notes": "Livrare la adresa de birou",
    "totalAmount": 3499.90,
    "totalCurrency": "RON",
    "createdAt": "2026-08-15T20:30:00Z",
    "lines": [
      {
        "id": "a1b2c3d4-...",
        "productId": "c3d4e5f6-...",
        "productName": "Laptop 15\"",
        "quantity": 1,
        "unitPriceAmount": 3499.90,
        "unitPriceCurrency": "RON",
        "lineTotalAmount": 3499.90,
        "lineTotalCurrency": "RON"
      }
    ]
  }
]
```

### `GET /orders/{id}`

Detaliu comandă (cu linii). **`200`** cu `OrderResponse`; **`404`** dacă nu există.

### `POST /orders`

Creează o comandă. Clientul (`customerId`) trebuie să existe; fiecare linie
referă un produs existent. Prețul unitar se preia din prețul curent al
produsului la momentul creării; `total` se calculează ca sumă a `lineTotal`.

**Request body** (`CreateOrderRequest`):

| Câmp | Tip | Obligatoriu | Descriere |
|------|-----|-------------|-----------|
| `customerId` | Guid | da | client existent |
| `currency` | string | nu | default `"USD"`, normalizat uppercase |
| `notes` | string | nu | |
| `lines` | array | nu | vezi `CreateOrderLineRequest` |

`CreateOrderLineRequest`:

| Câmp | Tip | Obligatoriu | Descriere |
|------|-----|-------------|-----------|
| `productId` | Guid | da | produs existent |
| `quantity` | int | da | > 0 |

```json
{
  "customerId": "e5f6a7b8-...",
  "currency": "RON",
  "notes": "Livrare la adresa de birou",
  "lines": [
    { "productId": "c3d4e5f6-...", "quantity": 1 }
  ]
}
```

**Response `201`** — `OrderResponse` (cu `status: "Draft"`, `orderNumber`
generat), `Location: /orders/{id}`.
**`400`** dacă clientul sau un produs din linii nu există.

### `DELETE /orders/{id}`

Șterge o comandă (cascade pe linii). **`204`**; **`404`** dacă nu există.

> Notă: m1 nu expune rute pentru tranzițiile de status. Entitatea `Order` are
> mașina de stări (`Submit`, `MarkPaid`, `MarkShipped`, `MarkDelivered`,
> `Cancel`) în domeniu, dar acestea nu sunt mapate pe endpoint-uri HTTP încă.

---

## 7. Schema entităților

Sursa: `apps/store-api/src/StoreApi.Domain/Entities/*` și
`.agents/bus/contracts.json` → `root.api_schema.entities`.

Legenda tipurilor: `?` = opțional (nullable); `(calculat)` = proprietate
derivată, nu este stocată.

### Value object: `Money`

| Câmp | Tip | Note |
|------|-----|------|
| `Amount` | decimal | ≥ 0, precizie (18,2) în BD |
| `Currency` | string | ISO, uppercase |

### `Product`

| Câmp | Tip | Note |
|------|-----|------|
| `Id` | Guid | PK |
| `Sku` | string | unic, uppercase, max 64 |
| `Name` | string | max 200 |
| `Description` | string? | max 2000 |
| `Price` | Money | |
| `CategoryId` | Guid | FK → Category |
| `Category` | Category | navigație |
| `IsActive` | bool | |
| `CreatedAt` | DateTime | |
| `UpdatedAt` | DateTime | |

### `Category`

| Câmp | Tip | Note |
|------|-----|------|
| `Id` | Guid | PK |
| `Name` | string | max 200 |
| `Slug` | string | unic, max 200 |
| `Description` | string? | max 1000 |
| `ParentId` | Guid? | self-FK, ierarhie |
| `Parent` | Category? | navigație |
| `Children` | ICollection\<Category> | navigație |
| `Products` | ICollection\<Product> | navigație |

### `Customer`

| Câmp | Tip | Note |
|------|-----|------|
| `Id` | Guid | PK |
| `Email` | string | unic, max 320 |
| `FirstName` | string | max 100 |
| `LastName` | string | max 100 |
| `Phone` | string? | max 40 |
| `CreatedAt` | DateTime | |
| `Orders` | ICollection\<Order> | navigație |

### `Order`

| Câmp | Tip | Note |
|------|-----|------|
| `Id` | Guid | PK |
| `OrderNumber` | string | unic, format `ORD-yyyyMMdd-XXXXXXXX` |
| `CustomerId` | Guid | FK → Customer |
| `Customer` | Customer | navigație |
| `Status` | OrderStatus | vezi enum de mai jos |
| `Currency` | string | max 3, uppercase |
| `Notes` | string? | max 1000 |
| `CreatedAt` | DateTime | |
| `UpdatedAt` | DateTime? | |
| `Lines` | IReadOnlyCollection\<OrderLine> | |
| `Total` | Money | (calculat) sumă `LineTotal` |

### `OrderLine`

| Câmp | Tip | Note |
|------|-----|------|
| `Id` | Guid | PK |
| `OrderId` | Guid | FK → Order |
| `Order` | Order | navigație |
| `ProductId` | Guid | FK → Product (snapshot) |
| `ProductName` | string | denumire la momentul comenzii |
| `UnitPrice` | Money | preț unitar la momentul comenzii |
| `Quantity` | int | > 0 |
| `LineTotal` | Money | (calculat) `UnitPrice × Quantity` |

### `Inventory` (stoc)

| Câmp | Tip | Note |
|------|-----|------|
| `ProductId` | Guid | PK + FK → Product |
| `Product` | Product | navigație 1:1 |
| `QuantityOnHand` | int | ≥ 0 |
| `Reserved` | int | ≥ 0 |
| `ReorderThreshold` | int | ≥ 0 |
| `UpdatedAt` | DateTime | |
| `Available` | int | (calculat) `QuantityOnHand − Reserved` |

### `ProductEmbedding` (căutare vectorială)

| Câmp | Tip | Note |
|------|-----|------|
| `ProductId` | Guid | PK + FK → Product |
| `Product` | Product | navigație 1:1 |
| `Embedding` | Vector | `vector(384)` în Postgres |
| `ModelVersion` | int | > 0 |
| `UpdatedAt` | DateTime | |

### Enum: `OrderStatus`

| Valoare | Nume | Int |
|---------|------|-----|
| `Draft` | ciornă | 1 |
| `Pending` | în așteptare | 2 |
| `Paid` | plătită | 3 |
| `Shipped` | expediată | 4 |
| `Delivered` | livrată | 5 |
| `Cancelled` | anulată | 6 |

În JSON, `status` se serializează ca string (`"Draft"`, `"Paid"`, etc.).

---

## 8. Mapare persistență

Tabele EF Core (vezi `StoreApi.Infrastructure/Persistence/StoreDbContext.cs`):

| Entitate | Tabel | Index unic |
|----------|-------|-----------|
| Category | `categories` | `Slug` |
| Product | `products` | `Sku` |
| Customer | `customers` | `Email` |
| Order | `orders` | `OrderNumber` |
| OrderLine | `order_lines` | — |
| Inventory | `inventory` | PK = `ProductId` |
| ProductEmbedding | `product_embeddings` | PK = `ProductId` |

Particularități:

- `Money` se mapează ca **complex type** (`ComplexProperty`) pe Postgres sau
  **owned type** (`OwnsOne`) pe InMemory, cu coloane `*_amount` (decimal 18,2)
  și `*_currency` (max 3).
- `Order.Total` este ignorat la persistență (`e.Ignore(o => o.Total)`).
- `ProductEmbedding.Embedding` este `vector(384)` pe Postgres (extensia
  `vector`), iar pe InMemory se convertește la/din string.
- Ștergerea unei comenzi șterge cascade liniile; relațiile `Category↔Product`,
  `Customer↔Order` și `Category.Parent` folosesc `Restrict`.

---

## 9. Căutare vectorială (infrastructură, non-HTTP în m1)

- `HashingEmbeddingService` generează embedding deterministic (feature hashing
  bag-of-words, 384 dimensiuni, normalizare L2) — fără servicii externe.
- `VectorProductSearchService` interoghează `product_embeddings` cu operatorul
  pgvector `<=>` (cosine distance) și returnează `ProductSearchResult(Product,
  Similarity)`.
- Aceste servicii trăiesc în `StoreApi.Infrastructure` și nu sunt expuse ca
  endpoint-uri HTTP în m1; sunt folosite de microserviciul de recomandări
  (`integrations/recommender`, m3).

---

## 10. Flux exemplu (curl)

```bash
BASE=http://localhost:5000

# 1. Categorie
curl -X POST $BASE/categories -H 'Content-Type: application/json' \
  -d '{"name":"Electronice"}'

# 2. Client
curl -X POST $BASE/customers -H 'Content-Type: application/json' \
  -d '{"email":"ana@example.com","firstName":"Ana","lastName":"Popescu"}'

# 3. Produs (categoria implicită dacă nu trimiți categoryId)
curl -X POST $BASE/products -H 'Content-Type: application/json' \
  -d '{"sku":"SKU-001","name":"Laptop 15\"","priceAmount":3499.90,"priceCurrency":"RON"}'

# 4. Comandă
curl -X POST $BASE/orders -H 'Content-Type: application/json' \
  -d '{"customerId":"<id-client>","currency":"RON","lines":[{"productId":"<id-produs>","quantity":1}]}'

# 5. Listări
curl $BASE/products
curl $BASE/orders
```
