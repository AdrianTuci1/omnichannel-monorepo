# Odoo Bridge (integrations/odoo)

Worker .NET 9 care sincronizează produse și comenzi între Odoo ERP și store-api (`apps/store-api`).

## Ce face

- Citește produse (`product.template`) din Odoo prin JSON-RPC și le creează/actualizează în store-api, potrivind după SKU (`default_code`).
- Citește comenzi (`sale.order`), parteneri (`res.partner`) și linii de comandă (`sale.order.line`) din Odoo și creează comenzile corespunzătoare în store-api.
- **Sincronizare inversă**: citește comenzile din store-api (`GET /orders`) și actualizează statusul în Odoo (`sale.order`) pentru cele modificate.
- Rulează ciclic (interval configurabil) sau o singură dată (`--run-once`).
- Idempotență: comenzile deja sincronizate sunt marcate în câmpul `notes` (prefix `Odoo:<număr_comandă>`) și nu sunt recreate.

## Flux bidirecțional

### Forward — Odoo → store-api

Produsele și comenzile sunt preluate din Odoo și persistate în store-api (a se vedea secțiunea „Mapare"). Fiecare comandă creată în store primește în `notes` marcatorul `Odoo:<sale.order.name>`, care leagă cele două reprezentări.

### Reverse — store-api → Odoo

La fiecare ciclu, workerul citește comenzile din store-api și, pentru cele care provin din Odoo (au marcatorul `Odoo:` în `notes`), compară statusul din store cu starea din Odoo. Dacă diferă, actualizează `sale.order.state` prin JSON-RPC `write`.

Maparea status store-api → stare Odoo:

| Status store-api (`OrderStatus`) | Stare Odoo (`sale.order.state`) |
|----------------------------------|---------------------------------|
| `Pending`                        | `sent`                          |
| `Paid`                           | `sale`                          |
| `Shipped`                        | `done`                          |
| `Delivered`                      | `done`                          |
| `Cancelled`                      | `cancel`                        |
| `Draft`                          | *(nu se propagă)*               |

`Draft` este starea inițială a comenzilor create în store de sincronizarea forward, deci nu este propagată înapoi (ar regresa comanda în Odoo). Comenzile fără marcator `Odoo:` (create direct în store-api de alte canale) nu sunt atinse de sincronizarea inversă.

## Build

```bash
cd integrations/odoo
dotnet build
```

## Configurare

Configurarea se face prin `appsettings.json` sau variabile de mediu (variabilele de mediu au prioritate). Convenția pentru variabile de mediu este `Secțiune__Cheie` (dublu underscore), de exemplu `Odoo__BaseUrl`.

### Secțiunea `Odoo`

| Cheie        | Variabilă de mediu  | Descriere                                              | Valoare implicită        |
|--------------|---------------------|--------------------------------------------------------|--------------------------|
| BaseUrl      | Odoo__BaseUrl       | URL-ul de bază al instanței Odoo                       | https://odoo.example.com |
| Database     | Odoo__Database      | Numele bazei de date Odoo                              | omnichannel              |
| Username     | Odoo__Username      | Utilizator Odoo (login)                                | admin                    |
| ApiKey       | Odoo__ApiKey        | API key Odoo (din profilul utilizatorului)             | (gol)                    |
| ProductModel | Odoo__ProductModel  | Modelul tehnic pentru produse                          | product.template         |
| OrderModel   | Odoo__OrderModel    | Modelul tehnic pentru comenzi                          | sale.order               |
| PartnerModel | Odoo__PartnerModel  | Modelul tehnic pentru parteneri                        | res.partner              |
| PageSize     | Odoo__PageSize      | Numărul maxim de înregistrări citite per search_read   | 200                      |

### Secțiunea `StoreApi`

| Cheie   | Variabilă de mediu  | Descriere           | Valoare implicită   |
|---------|---------------------|---------------------|---------------------|
| BaseUrl | StoreApi__BaseUrl   | URL-ul store-api    | http://localhost:5180 |

### Secțiunea `Sync`

| Cheie           | Variabilă de mediu      | Descriere                                      | Valoare implicită |
|-----------------|-------------------------|------------------------------------------------|-------------------|
| IntervalSeconds | Sync__IntervalSeconds   | Intervalul dintre ciclurile de sincronizare     | 300               |
| ProductsEnabled | Sync__ProductsEnabled   | Activează sincronizarea produselor              | true              |
| OrdersEnabled   | Sync__OrdersEnabled     | Activează sincronizarea comenzilor              | true              |
| ReverseOrdersEnabled | Sync__ReverseOrdersEnabled | Activează sincronizarea inversă a statusurilor (store-api → Odoo) | true |

## Rulare

```bash
# rulare ciclică (worker)
dotnet run --project integrations/odoo

# o singură trecere de sincronizare, apoi ieșire
dotnet run --project integrations/odoo -- --run-once

# cu variabile de mediu
Odoo__BaseUrl=https://odoo.magazin.ro \
Odoo__ApiKey=... \
StoreApi__BaseUrl=http://localhost:5180 \
dotnet run --project integrations/odoo
```

## Mapare

- **Produse**: `default_code` (Odoo) → `sku` (store-api). Dacă SKU există deja, se actualizează (`PUT`); altfel se creează (`POST`). Produsele fără `default_code` sunt omise.
- **Comenzi**: `sale.order.name` devine marcatorul de idempotență stocat în `notes`. Clienții se potrivesc după `email`; dacă lipsesc, se creează. Liniile se mapează prin SKU (prin `default_code` al variantei `product.product`); liniile fără produs mapabil sunt omise.
- **Moneda**: codul ISO din `currency_id`; dacă lipsește, se folosește `USD`.
- **Prețul liniilor**: se recalculează de store-api pe baza produsului; `price_unit` din Odoo nu este preluat.
- **Categorii**: la crearea produsului se folosește categoria implicită din store-api; la actualizare se păstrează categoria existentă.

## Structura proiectului

```
integrations/odoo/
├── OdooBridge.csproj
├── Program.cs
├── appsettings.json
├── Configuration/Options.cs        # opțiuni tipizate (Odoo/StoreApi/Sync)
├── Clients/OdooClient.cs           # client JSON-RPC Odoo
├── Clients/StoreApiClient.cs       # client HTTP store-api
├── Models/JsonRpc.cs               # cadru JSON-RPC 2.0
├── Models/OdooEntities.cs          # entități Odoo (produs, comandă, linie, partener, variantă)
├── Models/OdooJson.cs              # utilitare pentru referințe many2one
├── Models/StoreApiEntities.cs      # DTO-uri store-api
└── Services/
    ├── SyncService.cs              # logica de sincronizare
    ├── SyncReport.cs               # rezumatul unei treceri de sincronizare
    └── OdooSyncWorker.cs           # background service (bucla de sincronizare)
```

## Note

- Requeră Odoo ≥ 14 cu endpoint-ul JSON-RPC extern activ (`/jsonrpc`).
- Câmpurile `write_date` și `date_order` sunt parseate ca ISO 8601.
