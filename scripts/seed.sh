#!/usr/bin/env bash
set -euo pipefail

# =============================================================================
# Seed pentru Store API: creează categorii + produse de test prin HTTP (curl).
# Cerințe: curl, jq. Store API trebuie să fie pornit.
#
#   API_BASE_URL=http://localhost:5180 ./scripts/seed.sh
# =============================================================================

API_BASE_URL="${API_BASE_URL:-http://localhost:5180}"

log()  { printf '[seed] %s\n' "$*"; }
fail() { printf '[seed][EROARE] %s\n' "$*" >&2; exit 1; }

post_json() {
  local path="$1" body="$2"
  curl -fsS -X POST "${API_BASE_URL}${path}" \
    -H 'Content-Type: application/json' \
    -H 'Accept: application/json' \
    -d "$body"
}

log "Aștept Store API la ${API_BASE_URL} ..."
for i in $(seq 1 60); do
  if curl -fsS "${API_BASE_URL}/health" >/dev/null 2>&1; then
    log "Store API disponibil."
    break
  fi
  [ "$i" -eq 60 ] && fail "Store API nu a răspuns în 60 de secunde (pornit la ${API_BASE_URL}?)."
  sleep 1
done

# ---- Categorii (name | slug | description) ----
CATEGORIES=(
  "Electronice|electronice|Produse electronice si accesorii"
  "Imbracaminte|imbracaminte|Articole de imbracaminte"
  "Casa si Gradina|casa-gradina|Produse pentru casa si gradina"
  "Carti|carti|Carti si publicatii"
)

declare -A CATEGORY_IDS

for entry in "${CATEGORIES[@]}"; do
  IFS='|' read -r name slug desc <<< "$entry"
  log "Creez categoria: ${name}"
  resp=$(post_json "/categories" "$(jq -nc \
    --arg n "$name" --arg s "$slug" --arg d "$desc" \
    '{name: $n, slug: $s, description: $d}')")
  cid=$(jq -r '.id' <<< "$resp")
  [ -n "$cid" ] || fail "Răspunsul POST /categories nu conține .id: ${resp}"
  CATEGORY_IDS["$slug"]="$cid"
done

# ---- Produse (sku | name | priceAmount | priceCurrency | category_slug) ----
PRODUCTS=(
  "SKU-ELEC-001|Laptop Ultrabook 14|4599.99|RON|electronice"
  "SKU-ELEC-002|Casti wireless cu anulare zgomot|899.99|RON|electronice"
  "SKU-ELEC-003|Smartphone 5G 128GB|3299.50|RON|electronice"
  "SKU-IMB-001|Tricou bumbac organic|89.90|RON|imbracaminte"
  "SKU-IMB-002|Blugi slim fit|199.99|RON|imbracaminte"
  "SKU-CG-001|Set unelte gradinarit|149.50|RON|casa-gradina"
  "SKU-CG-002|Ghiveci ceramic 20cm|45.00|RON|casa-gradina"
  "SKU-CRT-001|Roman contemporan - editie de buzunar|59.90|RON|carti"
  "SKU-CRT-002|Ghid de programare functionala|129.00|RON|carti"
)

for entry in "${PRODUCTS[@]}"; do
  IFS='|' read -r sku name price currency slug <<< "$entry"
  cid="${CATEGORY_IDS[$slug]:-}"
  [ -n "$cid" ] || fail "Categoria '$slug' nu are id — seed incomplet."
  log "Creez produsul: ${name} (${sku})"
  post_json "/products" "$(jq -nc \
    --arg sku "$sku" --arg name "$name" --argjson price "$price" \
    --arg currency "$currency" --arg cid "$cid" \
    '{sku: $sku, name: $name, priceAmount: $price, priceCurrency: $currency, categoryId: $cid}')" \
    >/dev/null
done

log "Seed finalizat: ${#CATEGORIES[@]} categorii, ${#PRODUCTS[@]} produse."
log "Verifică: curl ${API_BASE_URL}/products"
