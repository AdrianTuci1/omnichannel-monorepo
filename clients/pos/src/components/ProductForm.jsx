import { useState } from "react";
import { SUPPORTED_CURRENCIES, DEFAULT_CURRENCY } from "../types.js";

const EMPTY = {
  sku: "",
  name: "",
  priceAmount: "",
  priceCurrency: DEFAULT_CURRENCY,
  description: "",
};

export default function ProductForm({ onClose, onCreate, busy }) {
  const [form, setForm] = useState(EMPTY);

  const set = (key) => (e) => setForm((f) => ({ ...f, [key]: e.target.value }));

  const submit = async (e) => {
    e.preventDefault();
    await onCreate({
      sku: form.sku,
      name: form.name,
      priceAmount: Number(form.priceAmount),
      priceCurrency: form.priceCurrency,
      description: form.description || null,
    });
    setForm(EMPTY);
  };

  return (
    <div className="modal-backdrop">
      <form className="modal" onSubmit={submit}>
        <h3>Produs nou</h3>
        <input
          className="input"
          placeholder="Nume"
          value={form.name}
          onChange={set("name")}
          required
        />
        <input
          className="input"
          placeholder="SKU"
          value={form.sku}
          onChange={set("sku")}
          required
        />
        <div className="grid-2">
          <input
            className="input"
            type="number"
            step="0.01"
            min="0"
            placeholder="Preț"
            value={form.priceAmount}
            onChange={set("priceAmount")}
            required
          />
          <select
            className="input"
            value={form.priceCurrency}
            onChange={set("priceCurrency")}
          >
            {SUPPORTED_CURRENCIES.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </div>
        <input
          className="input"
          placeholder="Descriere (opțional)"
          value={form.description}
          onChange={set("description")}
        />
        <div className="row">
          <button className="btn" type="submit" disabled={busy}>
            {busy ? "Se salvează…" : "Salvează"}
          </button>
          <button className="btn btn-ghost" type="button" onClick={onClose}>
            Anulează
          </button>
        </div>
      </form>
    </div>
  );
}
