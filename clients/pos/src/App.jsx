import { useState } from "react";
import { api, errorMessage } from "./api.js";
import { DEFAULT_CURRENCY } from "./types.js";
import { usePosData } from "./hooks/usePosData.js";
import { useCartStore } from "./store/cart.js";
import ConnectionBar from "./components/ConnectionBar.jsx";
import OrdersPanel from "./components/OrdersPanel.jsx";
import ProductGrid from "./components/ProductGrid.jsx";
import Cart from "./components/Cart.jsx";
import CustomerSection from "./components/CustomerSection.jsx";
import ProductForm from "./components/ProductForm.jsx";

const WALK_IN = { email: "walk-in@pos.local", firstName: "Walk-in", lastName: "Client" };

export default function App() {
  const data = usePosData();
  const cart = useCartStore();

  const [selectedCustomerId, setSelectedCustomerId] = useState(null);
  const [notes, setNotes] = useState("");
  const [showProductForm, setShowProductForm] = useState(false);

  const [orderBusy, setOrderBusy] = useState(false);
  const [customerBusy, setCustomerBusy] = useState(false);
  const [walkInBusy, setWalkInBusy] = useState(false);
  const [productBusy, setProductBusy] = useState(false);
  const [toast, setToast] = useState(null);

  const notify = (type, text) => setToast({ type, text });

  const handleCreateOrder = async () => {
    if (cart.lines.length === 0) {
      notify("error", "Coșul este gol.");
      return;
    }
    if (!selectedCustomerId) {
      notify("error", "Selectează un client.");
      return;
    }
    setOrderBusy(true);
    try {
      const created = await api.createOrder({
        customerId: selectedCustomerId,
        currency: DEFAULT_CURRENCY,
        notes: notes.trim() ? notes.trim() : null,
        lines: cart.lines.map((l) => ({ productId: l.productId, quantity: l.quantity })),
      });
      cart.clear();
      setNotes("");
      notify("success", `Comanda ${created.orderNumber} a fost creată.`);
      await data.reload();
    } catch (err) {
      notify("error", errorMessage(err));
    } finally {
      setOrderBusy(false);
    }
  };

  const handleWalkIn = async () => {
    setWalkInBusy(true);
    try {
      const existing = data.customers.find((c) => c.email === WALK_IN.email);
      if (existing) {
        setSelectedCustomerId(existing.id);
        return;
      }
      const created = await api.createCustomer(WALK_IN);
      await data.reload();
      setSelectedCustomerId(created.id);
    } catch (err) {
      notify("error", errorMessage(err));
    } finally {
      setWalkInBusy(false);
    }
  };

  const handleCreateCustomer = async (payload) => {
    setCustomerBusy(true);
    try {
      const created = await api.createCustomer(payload);
      await data.reload();
      setSelectedCustomerId(created.id);
      notify("success", `Clientul ${created.firstName} ${created.lastName} a fost salvat.`);
    } catch (err) {
      notify("error", errorMessage(err));
    } finally {
      setCustomerBusy(false);
    }
  };

  const handleCreateProduct = async (payload) => {
    setProductBusy(true);
    try {
      await api.createProduct(payload);
      await data.reload();
      setShowProductForm(false);
      notify("success", `Produsul ${payload.name} a fost salvat.`);
    } catch (err) {
      notify("error", errorMessage(err));
    } finally {
      setProductBusy(false);
    }
  };

  const handleDeleteOrder = async (id) => {
    try {
      await api.deleteOrder(id);
      await data.reload();
    } catch (err) {
      notify("error", errorMessage(err));
    }
  };

  return (
    <div className="app">
      <header className="app-header">
        <h1>Omnichannel POS</h1>
        <ConnectionBar
          baseUrl={data.baseUrl}
          health={data.health}
          onSaveUrl={data.changeBaseUrl}
        />
      </header>

      {toast && <div className={`toast toast-${toast.type}`}>{toast.text}</div>}

      <main className="layout">
        <section className="left">
          <OrdersPanel
            orders={data.orders}
            customers={data.customers}
            loading={data.loading}
            error={data.error}
            onDelete={handleDeleteOrder}
          />
        </section>

        <section className="right">
          <ProductGrid
            products={data.products}
            onAdd={(p) => cart.add(p)}
            onNewProduct={() => setShowProductForm(true)}
          />
          <Cart
            lines={cart.lines}
            currency={DEFAULT_CURRENCY}
            onIncrement={(id) => cart.increment(id)}
            onDecrement={(id) => cart.decrement(id)}
            onRemove={(id) => cart.remove(id)}
          />
          <CustomerSection
            customers={data.customers}
            selectedId={selectedCustomerId}
            onSelect={setSelectedCustomerId}
            onWalkIn={handleWalkIn}
            walkInBusy={walkInBusy}
            onCreateCustomer={handleCreateCustomer}
            customerBusy={customerBusy}
          />
          <div className="panel checkout">
            <label className="field">
              <span>Note (opțional)</span>
              <input
                className="input"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder="Observații comandă…"
              />
            </label>
            <button
              className="btn btn-primary btn-lg"
              onClick={handleCreateOrder}
              disabled={orderBusy}
            >
              {orderBusy ? "Se creează…" : "Finalizează comanda"}
            </button>
          </div>
        </section>
      </main>

      {showProductForm && (
        <ProductForm
          onClose={() => setShowProductForm(false)}
          onCreate={handleCreateProduct}
          busy={productBusy}
        />
      )}
    </div>
  );
}
