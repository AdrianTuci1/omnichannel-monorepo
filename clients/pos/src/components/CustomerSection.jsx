import { useState } from "react";

const EMPTY_FORM = { firstName: "", lastName: "", email: "", phone: "" };

export default function CustomerSection({
  customers,
  selectedId,
  onSelect,
  onWalkIn,
  walkInBusy,
  onCreateCustomer,
  customerBusy,
}) {
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState(EMPTY_FORM);

  const set = (key) => (e) => setForm((f) => ({ ...f, [key]: e.target.value }));

  const submit = async (e) => {
    e.preventDefault();
    await onCreateCustomer({
      email: form.email,
      firstName: form.firstName,
      lastName: form.lastName,
      phone: form.phone || null,
    });
    setForm(EMPTY_FORM);
    setOpen(false);
  };

  return (
    <div className="panel">
      <h2>Client</h2>
      <div className="customer-row">
        <select
          className="input"
          value={selectedId ?? ""}
          onChange={(e) => onSelect(e.target.value || null)}
        >
          <option value="">— alege clientul —</option>
          {customers.map((c) => (
            <option key={c.id} value={c.id}>
              {c.firstName} {c.lastName} ({c.email})
            </option>
          ))}
        </select>
        <button className="btn" onClick={onWalkIn} disabled={walkInBusy}>
          {walkInBusy ? "Se creează…" : "Client walk-in"}
        </button>
      </div>

      {!open ? (
        <button className="btn btn-ghost" onClick={() => setOpen(true)}>
          + Client nou
        </button>
      ) : (
        <form className="stack" onSubmit={submit}>
          <div className="grid-2">
            <input
              className="input"
              placeholder="Prenume"
              value={form.firstName}
              onChange={set("firstName")}
              required
            />
            <input
              className="input"
              placeholder="Nume"
              value={form.lastName}
              onChange={set("lastName")}
              required
            />
          </div>
          <input
            className="input"
            type="email"
            placeholder="Email"
            value={form.email}
            onChange={set("email")}
            required
          />
          <input
            className="input"
            placeholder="Telefon (opțional)"
            value={form.phone}
            onChange={set("phone")}
          />
          <div className="row">
            <button className="btn" type="submit" disabled={customerBusy}>
              {customerBusy ? "Se salvează…" : "Salvează clientul"}
            </button>
            <button
              className="btn btn-ghost"
              type="button"
              onClick={() => {
                setForm(EMPTY_FORM);
                setOpen(false);
              }}
            >
              Anulează
            </button>
          </div>
        </form>
      )}
    </div>
  );
}
