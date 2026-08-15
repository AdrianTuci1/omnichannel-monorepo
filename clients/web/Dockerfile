# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Web client (Next.js 15) — build multi-stage
# Context de build: clients/web
# NEXT_PUBLIC_API_BASE_URL este inlined în bundle-ul client la build-time;
# se referă la host-ul pe care rulează browser-ul (store-api expus pe 5180).
# ---------------------------------------------------------------------------

FROM node:20-alpine AS builder
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY . .
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build

FROM node:20-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
COPY --from=builder /app/package.json ./
COPY --from=builder /app/node_modules ./node_modules
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/next.config.mjs ./
EXPOSE 3000
CMD ["npm", "start"]
