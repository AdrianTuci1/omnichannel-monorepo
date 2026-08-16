# Plan: Faza 2 — Filtrare complexă, plată ramburs, depozite, observabilitate

Obiectiv: filtrare avansată produse, metodă de plată ramburs (cash on delivery), alocare stoc pe depozite la plasarea comenzii, și observabilitate open-source (OpenTelemetry + Prometheus + Grafana).

## Contracte (stabilite înainte, ca workerii să ruleze în paralel)
- **Filtrare:** `GET /products?search=&categoryId=&minPrice=&maxPrice=&inStock=&sort=&page=1&pageSize=20`
  -> `{items: [ProductResponse], total, page, pageSize}`. `sort` ∈ name|price_asc|price_desc|newest. `inStock` filtrează produsele cu stoc disponibil > 0.
- **Plată:** `Order` primește `PaymentMethod` (CashOnDelivery=ramburs, Card, BankTransfer) și `PaymentStatus` (Pending, Paid, Refunded). `CreateOrderRequest.paymentMethod` (default CashOnDelivery). Ramburs = plata la livrare (status Pending la plasare).
- **Depozite:** `Warehouse {Id, Name, Code, IsActive}` + stoc per depozit (`WarehouseInventory {WarehouseId, ProductId, QuantityOnHand, Reserved}`). La plasarea comenzii, stocul se alocă dintr-un depozit (first-fit: primul depozit cu stoc suficient; documentează strategia). Endpoint-uri: `GET /warehouses`, `GET /warehouses/{id}/inventory`.
- **Observabilitate:** store-api expune `GET /metrics` (Prometheus text format) + trace OpenTelemetry. Prometheus scrape `store-api:5180/metrics`. Grafana dashboards.

## Workeri (directoare disjuncte → paralel-safe)
1. **backend3** (`apps/store-api`): filtrare + PaymentMethod/Status + Warehouse/WarehouseInventory + alocare + OTel/metrics. Teste xUnit noi.
2. **frontend3** (`clients/web`): UI filtrare (categorie, preț, sortare, paginare, search) + selector metodă plată la checkout.
3. **observability3** (repo root): docker-compose + prometheus.yml + grafana provisioning + otel-collector.

## Verificare
- backend3: `dotnet build` + `dotnet test`.
- frontend3: `npm run build`.
- observability3: `docker compose config` valid + prometheus/grafana configuri valide.
