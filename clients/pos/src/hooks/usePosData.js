import { useCallback, useEffect, useState } from "react";
import { api, resolveBaseUrl, setBaseUrl, errorMessage } from "../api.js";

export function usePosData() {
  const [baseUrl, setBaseUrlState] = useState(() => resolveBaseUrl());
  const [products, setProducts] = useState([]);
  const [customers, setCustomers] = useState([]);
  const [orders, setOrders] = useState([]);
  const [health, setHealth] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [healthRes, productsRes, customersRes, ordersRes] = await Promise.all([
        api.health(),
        api.listProducts(),
        api.listCustomers(),
        api.listOrders(),
      ]);
      setHealth(healthRes);
      setProducts(productsRes ?? []);
      setCustomers(customersRes ?? []);
      setOrders(ordersRes ?? []);
    } catch (err) {
      setError(new Error(errorMessage(err)));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load, baseUrl]);

  const changeBaseUrl = useCallback((url) => {
    setBaseUrl(url);
    setBaseUrlState(resolveBaseUrl());
  }, []);

  return {
    baseUrl,
    changeBaseUrl,
    products,
    customers,
    orders,
    health,
    loading,
    error,
    reload: load,
  };
}
