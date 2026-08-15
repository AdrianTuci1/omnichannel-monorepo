# store-api Helm chart

Chart Helm pentru serviciul `store-api` (ASP.NET Core 9 minimal API) din
monorepo-ul Omnichannel. Expune CRUD pentru `/products`, `/orders`,
`/categories`, `/customers` și un endpoint de health `/health`.

## Cerințe

- Helm 3
- Un cluster Kubernetes (Ingress și HPA presupun controller ingress + metrics-server)

## Build imagine (prerechizit)

Chart-ul nu conține Dockerfile — imaginea se construiește separat din
`apps/store-api`. Valoarea implicită din `values.yaml` este
`ghcr.io/omnichannel/store-api:1.0.0`. La instalare, suprascrie-o cu imaginea
reală publicată:

```bash
docker build -t ghcr.io/omnichannel/store-api:1.0.0 apps/store-api
docker push ghcr.io/omnichannel/store-api:1.0.0
```

## Instalare

```bash
helm upgrade --install store-api ./helm/store-api \
  --namespace omnichannel --create-namespace \
  --set image.repository=ghcr.io/omnichannel/store-api \
  --set image.tag=1.0.0
```

## Expunere (Ingress)

```bash
helm upgrade --install store-api ./helm/store-api \
  --set ingress.enabled=true \
  --set 'ingress.hosts[0].host=store-api.example.com'
```

## Configurare cheie (values.yaml)

| Cheie                                      | Implicit | Descriere                                      |
|--------------------------------------------|----------|------------------------------------------------|
| `replicaCount`                             | `2`      | Replici când autoscaling e dezactivat          |
| `image.repository` / `image.tag`           | `ghcr.io/omnichannel/store-api` / `1.0.0` | Imaginea containerului |
| `app.port`                                 | `8080`   | Portul Kestrel (ASPNETCORE_URLS)               |
| `app.aspnetEnvironment`                    | `Production` | ASPNETCORE_ENVIRONMENT                     |
| `autoscaling.enabled`                      | `true`   | Activează HPA (CPU + memorie)                  |
| `autoscaling.minReplicas` / `maxReplicas`  | `2` / `10` | Intervalul HPA                            |
| `ingress.enabled`                          | `false`  | Activează Ingress                              |
| `serviceAccount.create`                    | `true`   | Creează ServiceAccount dedicat                 |

## Note privind starea curentă a aplicației

`Program.cs` folosește în prezent `UseInMemoryDatabase("StoreApi")`, deci
chart-ul nu definește nicio dependență de bază de date sau secret de conexiune.
Când backend-ul va trece la PostgreSQL (conform contractului m1), se vor adăuga
aici un Secret pentru connection string și `app.*.connectionString`, fără a
schimba resursele Kubernetes existente.

## Validare

```bash
helm lint ./helm/store-api
helm template store-api ./helm/store-api
```

Securitate: pod-ul rulează non-root (UID 1654, utilizatorul `app` din imaginea
oficială .NET), `readOnlyRootFilesystem: true` (cu `/tmp` montat ca `emptyDir`)
și `allowPrivilegeEscalation: false`.
