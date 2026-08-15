import { formatMoney, formatDateTime } from "../format.js";
import StatusBadge from "./StatusBadge.jsx";

function customerName(customers, id) {
  const c = customers.find((c) => c.id === id);
  return c ? `${c.firstName} ${c.lastName}` : id;
}

export default function OrdersPanel({ orders, customers, loading, error, onDelete }) {
  if (loading) {
    return (
      <div className="panel">
        <h2>Comenzi</h2>
        <p className="muted">Se încarcă…</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="panel">
        <h2>Comenzi</h2>
        <p className="error-text">{error.message}</p>
      </div>
    );
  }

  if (orders.length === 0) {
    return (
      <div className="panel">
        <h2>Comenzi</h2>
        <p className="muted">Nicio comandă încă.</p>
      </div>
    );
  }

  return (
    <div className="panel">
      <h2>Comenzi ({orders.length})</h2>
      <ul className="order-list">
        {orders.map((o) => (
          <li key={o.id} className="order-item">
            <div className="order-main">
              <span className="order-number">{o.orderNumber}</span>
              <StatusBadge status={o.status} />
            </div>
            <div className="order-meta">
              <span>{customerName(customers, o.customerId)}</span>
              <span>{o.lines.length} produse</span>
              <span>{formatDateTime(o.createdAt)}</span>
            </div>
            <div className="order-foot">
              <strong>{formatMoney(o.totalAmount, o.totalCurrency)}</strong>
              <button className="btn btn-ghost btn-sm" onClick={() => onDelete(o.id)}>
                Șterge
              </button>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
