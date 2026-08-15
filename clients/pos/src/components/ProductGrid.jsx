import { useState } from "react";
import { formatMoney } from "../format.js";

export default function ProductGrid({ products, onAdd, onNewProduct }) {
  const [query, setQuery] = useState("");

  const filtered = products.filter((p) =>
    `${p.name} ${p.sku}`.toLowerCase().includes(query.toLowerCase()),
  );

  return (
    <div className="panel">
      <div className="panel-head">
        <h2>Produse</h2>
        <button className="btn" onClick={onNewProduct}>
          + Produs
        </button>
      </div>
      <input
        className="search-input"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Caută produs (nume sau SKU)…"
      />
      {filtered.length === 0 ? (
        <p className="muted">Niciun produs disponibil.</p>
      ) : (
        <div className="product-grid">
          {filtered.map((p) => (
            <button key={p.id} className="product-card" onClick={() => onAdd(p)}>
              <span className="product-name">{p.name}</span>
              <span className="product-sku">{p.sku}</span>
              <span className="product-price">
                {formatMoney(p.priceAmount, p.priceCurrency)}
              </span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
