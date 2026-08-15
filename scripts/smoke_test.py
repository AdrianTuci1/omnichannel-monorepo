#!/usr/bin/env python3
"""Smoke test end-to-end pentru Omnichannel: auth -> customer -> product -> search -> cart -> order -> review -> related."""
import json, urllib.request, urllib.error, uuid

BASE = "http://localhost:5180"
ok_count = 0
fail_count = 0


def req(method, path, body=None, token=None):
    url = BASE + path
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(url, data=data, method=method)
    r.add_header("Content-Type", "application/json")
    if token:
        r.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(r, timeout=15) as resp:
            raw = resp.read().decode()
            return resp.status, json.loads(raw) if raw else None
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()


def step(name, status, body=None):
    global ok_count, fail_count
    ok = 200 <= status < 300
    ok_count += ok
    fail_count += (not ok)
    detail = json.dumps(body, ensure_ascii=False)[:180] if body is not None else ""
    print(f"{'OK ' if ok else 'FAIL'} {name}: HTTP {status} {detail}")
    return body if ok else None


email = f"smoke_{uuid.uuid4().hex[:8]}@test.com"
pw = "StrongPass123!"
uniq = uuid.uuid4().hex[:6]

s, b = req("POST", "/auth/register", {"email": email, "password": pw, "firstName": "Smoke", "lastName": "Test"})
step("register", s, b)

s, b = req("POST", "/auth/login", {"email": email, "password": pw})
login = step("login", s, b)
token = (login or {}).get("accessToken")
refresh = (login or {}).get("refreshToken")

s, b = req("POST", "/auth/refresh", {"refreshToken": refresh})
step("refresh token", s, b)

s, b = req("POST", "/customers", {"email": email, "firstName": "Smoke", "lastName": "Test"}, token)
cust = step("create customer", s, b)
customer_id = (cust or {}).get("id")

s, b = req("POST", "/categories", {"name": f"Electronice {uniq}", "slug": f"electronice-{uniq}"}, token)
cat = step("create category", s, b)
cat_id = (cat or {}).get("id")

s, b = req("POST", "/products", {"sku": f"SKU-{uniq}", "name": f"Laptop Test {uniq}", "priceAmount": 2999.99, "priceCurrency": "USD", "description": "Laptop de test", "categoryId": cat_id}, token)
prod = step("create product", s, b)
product_id = (prod or {}).get("id")

s, b = req("GET", f"/products/search?q=Laptop%20{uniq}")
step("search", s, b)

s, b = req("POST", "/cart/items", {"productId": product_id, "quantity": 2}, token)
step("add to cart", s, b)
s, b = req("GET", "/cart", None, token)
step("get cart", s, b)

s, b = req("POST", "/orders", {"customerId": customer_id, "lines": [{"productId": product_id, "quantity": 2}]}, token)
step("create order", s, b)

s, b = req("POST", f"/products/{product_id}/reviews", {"rating": 5, "title": "Excelent", "comment": "Recomand", "customerId": customer_id}, token)
step("create review", s, b)
s, b = req("GET", f"/products/{product_id}/reviews")
step("get reviews", s, b)

s, b = req("GET", f"/products/{product_id}/related")
step("related products", s, b)

s, b = req("PUT", f"/products/{product_id}/inventory", {"quantityOnHand": 100, "reserved": 0, "reorderThreshold": 10}, token)
step("set inventory", s, b)
s, b = req("GET", f"/products/{product_id}/inventory")
step("get inventory", s, b)

print(f"\nRezultat: {ok_count} OK, {fail_count} FAIL")
