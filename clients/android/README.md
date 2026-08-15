# Omnichannel Store — client Android

Client Android (Kotlin + Jetpack Compose + Room) pentru backend-ul `apps/store-api` (m1).

## Ce face

- **Listă produse** — afișează numele, SKU-ul, categoria (rezolvată din cache) și prețul,
  cu un indicator „Activ / Inactiv".
- **Detaliu produs** — nume, preț, status, SKU, categorie, descriere, data creării și ID.
- **Cache offline** — produsele și categoriile sunt persistate în Room; la pornire se face
  sync cu backend-ul, iar dacă serverul nu e accesibil se afișează datele din cache
  (cu un banner „Offline").
- **Client API complet** — `StoreApi` acoperă toate cele 18 rute ale `apps/store-api`
  (CRUD `/products`, CRUD `/orders`, CRUD `/categories`, CRUD `/customers`, `GET /health`).
  UI-ul folosește în acest milestone listele și detaliul de produs + categorii.

## Arhitectură

```
data/
  remote/     Retrofit + kotlinx.serialization (DTO-uri, StoreApi, ApiClient)
  local/      Room (StoreDatabase, ProductEntity, CategoryEntity, DAO-uri, mappers)
  repository/ ProductRepository, CategoryRepository (offline-first: Room <- sync -> API)
ui/
  list/       ProductListScreen + ProductListViewModel
  detail/     ProductDetailScreen + ProductDetailViewModel
  components/ stări comune (loading, offline, empty, badge status)
  navigation/ Navigation Compose (lista -> detaliu)
  theme/      Material 3 light/dark
util/         MoneyFormatter, DateFormatter
```

Pattern-ul e **offline-first**: ViewModel-urile observă `Flow` din Room (sursa de adevăr
pentru UI), iar `refresh()` sincronizează cache-ul cu rețeaua. Fără framework de DI —
dependențele se construiesc în `AppContainer`, expus de `StoreApplication`.

## Cerințe de build

- **JDK 17+** (compilare Java/Kotlin cu target 17).
- **Android SDK** cu `platforms;android-35` și `build-tools;35.0.0`, licențe acceptate.
  Indică locația prin `local.properties` (ex. `sdk.dir=/opt/android-sdk`) sau `ANDROID_HOME`.
- **Gradle** (wrapper-ul descarcă automat distribuția 8.11.1).

## Build

```bash
cd clients/android
./gradlew assembleDebug
```

APK-ul rezultat: `app/build/outputs/apk/debug/app-debug.apk`.

## Configurare backend

URL-ul API e setat în `app/build.gradle.kts` prin `buildConfigField("API_BASE_URL", ...)`.
Valoarea implicită e `http://10.0.2.2:8080/` (localhost văzut din emulatorul Android).

- **Emulator**: pornește `apps/store-api` pe portul 8080; `10.0.2.2` îl țintește direct.
- **Dispozitiv fizic**: înlocuiește `10.0.2.2` cu IP-ul LAN al mașinii care rulează backend-ul.

Traficul HTTP necriptat e permis doar către `10.0.2.2` / `localhost` / `127.0.0.1`
(vezi `res/xml/network_security_config.xml`); restul rămâne pe HTTPS.

## Schema de date

DTO-urile din `data/remote/dto` reflectă 1:1 contractele din
`apps/store-api/src/StoreApi.Api/Contracts.cs` (camelCase, `Guid` → `String`,
`decimal` → `Double`, `DateTime` → `String` ISO-8601). Entitățile Room (`products`,
`categories`) mapează răspunsurile `ProductResponse` și `CategoryResponse`.
