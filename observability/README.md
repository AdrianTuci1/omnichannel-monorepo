# Observabilitate — Prometheus + Grafana + OpenTelemetry Collector

Stivă de observabilitate open-source pentru monorepo-ul Omnichannel. Adaugă
metrici HTTP și tracing pentru `store-api` (și, opțional, celelalte servicii)
fără componente proprietare.

## Componente

| Serviciu         | Imagine                                        | Port host | Rol                                                         |
|------------------|------------------------------------------------|-----------|-------------------------------------------------------------|
| Prometheus       | `prom/prometheus:v2.53.1`                      | 9090      | Colectează/scrapează metrici (format Prometheus)           |
| Grafana          | `grafana/grafana:11.1.0`                       | 3001      | Dashboard-uri + vizualizare                                 |
| OpenTelemetry Collector | `otel/opentelemetry-collector-contrib:0.105.0` | 4317 (OTLP gRPC), 4318 (OTLP HTTP), 8889 (Prometheus) | Primește telemetrie OTLP și o expune în format Prometheus |

## Pași de acces

```bash
# pornește doar stiva de observabilitate (plus dependințele de rețea)
docker compose up -d prometheus grafana otel-collector

# sau toată platforma
docker compose up -d
```

- **Prometheus UI:** http://localhost:9090 — `Status > Targets` arată starea
  job-urilor `prometheus`, `store-api` și `otel-collector`.
- **Grafana:** http://localhost:3001 — login implicit `admin` / `admin`
  (configurabil prin `GRAFANA_ADMIN_USER` / `GRAFANA_ADMIN_PASSWORD` în `.env`;
  vezi `.env.example`). Dashboard-ul `Omnichannel > Omnichannel Store API` este
  provisionat automat la pornire.
- **OpenTelemetry Collector:** OTLP gRPC pe `:4317`, OTLP HTTP pe `:4318`,
  endpoint Prometheus pe `:8889/metrics`.

## Trasee de metrici

Store API expune `GET /metrics` în format Prometheus text (implementat de
backend3_worker, faza 2). Există două trasee către Prometheus:

1. **Direct (principal):** Prometheus scrapează `store-api:5180/metrics`
   (job `store-api` în `prometheus/prometheus.yml`).
2. **Prin Collector (opțional):** dacă store-api este configurat să exporte
   OTLP, setează endpoint-ul la `http://otel-collector:4318` (HTTP) sau
   `http://otel-collector:4317` (gRPC). Collectorul expune totul pe
   `:8889/metrics`, de unde Prometheus scrapează (job `otel-collector`).

## Contract de metrice (așteptat de dashboard)

Dashboard-ul `Omnichannel Store API` se bazează pe următoarele metrici pe care
`store-api` trebuie să le expună la `/metrics` (nume canonice prometheus-net):

| Metrică                         | Tip       | Etichete   | Descriere                          |
|---------------------------------|-----------|------------|------------------------------------|
| `http_requests_total`           | counter   | `code`     | Total cereri HTTP, pe status code  |
| `http_request_duration_seconds` | histogram | `code`     | Durata cererilor HTTP (histogramă) |

Panourile dashboard-ului:
- **HTTP Request Rate** — `sum(rate(http_requests_total[$__rate_interval]))`
- **HTTP Error Rate (5xx)** — raportul cererilor cu `code` 5xx
- **HTTP Request Duration** — percentile p50 / p95 / p99 din histogramă
- **Requests by Status Code** — rata de cereri defalcată pe `code`

Dacă backend-ul folosește altă convenție de denumire, actualizează atât
metricele din store-api, cât și interogările din dashboard (sau expune
ambele nume).

## Fișiere

```
observability/
├── prometheus/
│   └── prometheus.yml                    # scrape_configs (prometheus self, store-api, otel-collector)
├── otel-collector/
│   └── otel-collector-config.yaml        # OTLP in, Prometheus out (:8889)
├── grafana/
│   ├── provisioning/
│   │   ├── datasources/datasource.yml    # datasource Prometheus (uid: prometheus)
│   │   └── dashboards/dashboards.yml     # provider care încarcă JSON-urile
│   └── dashboards/
│       └── omnichannel-store-api.json    # dashboard: rate, latency, error rate
└── README.md
```

## Configurare

Credențialele Grafana sunt configurabile prin variabile de mediu (default
`admin`/`admin` pentru dev local):

```bash
GRAFANA_ADMIN_USER=admin
GRAFANA_ADMIN_PASSWORD=change-me
```
