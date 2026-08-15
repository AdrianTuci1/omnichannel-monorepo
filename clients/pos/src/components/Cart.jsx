import { formatMoney } from "../format.js";
import { cartTotal } from "../store/cart.js";

export default function Cart({ lines, currency, onIncrement, onDecrement, onRemove }) {
  const total = cartTotal(lines);

  if (lines.length === 0) {
    return (
      <div className="panel">
        <h2>Coș</h2>
        <p className="muted">Coșul este gol. Adaugă produse.</p>
      </div>
    );
  }

  return (
    <div className="panel">
      <h2>Coș</h2>
      <ul className="cart-list">
        {lines.map((l) => (
          <li key={l.productId} className="cart-line">
            <div className="cart-line-name">
              <span>{l.productName}</span>
              <span className="muted">
                {formatMoney(l.unitPriceAmount, l.unitPriceCurrency)} × {l.quantity}
              </span>
            </div>
            <div className="cart-line-qty">
              <button
                className="btn btn-ghost btn-sm"
                onClick={() => onDecrement(l.productId)}
              >
                −
              </button>
              <span className="qty-value">{l.quantity}</span>
              <button
                className="btn btn-ghost btn-sm"
                onClick={() => onIncrement(l.productId)}
              >
                +
              </button>
            </div>
            <div className="cart-line-total">
              <strong>
                {formatMoney(l.unitPriceAmount * l.quantity, l.unitPriceCurrency)}
              </strong>
              <button
                className="btn btn-ghost btn-sm"
                onClick={() => onRemove(l.productId)}
              >
                ✕
              </button>
            </div>
          </li>
        ))}
      </ul>
      <div className="cart-total">
        <span>Total</span>
        <strong>{formatMoney(total, currency)}</strong>
      </div>
    </div>
  );
}
