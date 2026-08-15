# Omnichannel POS

Client POS (desktop/tablet) pentru StoreApi, construit cu React + Vite.

## Funcționalitate

- Listă comenzi (număr, client, status, total, dată) consumând `GET /orders`.
- Vânzare rapidă: coș cu cantități și total, selecție/creare client, creare
  comandă prin `POST /orders`.
- Creare client walk-in (`POST /customers`) și client nominal.
- Creare produs (`POST /products`) pentru a alimenta catalogul.
- URL de bază configurabil (variabila de mediu `VITE_API_BASE_URL` sau la
  runtime din bara de conexiune, persistat în `localStorage`).

## Rulare

```bash
npm install
npm run dev       # dezvoltare, http://localhost:5173
npm run build     # build de producție în dist/
```

Backend-ul trebuie să ruleze la `http://localhost:5000` (sau URL-ul setat).
