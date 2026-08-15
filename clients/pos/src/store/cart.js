import { create } from "zustand";

// Coșul de vânzare. O linie păstrează doar datele necesare pentru a fi
// trimise la POST /orders ({ productId, quantity }) și pentru afișarea totalului.
export function cartTotal(lines) {
  return lines.reduce((sum, l) => sum + l.unitPriceAmount * l.quantity, 0);
}

export const useCartStore = create((set) => ({
  lines: [],

  add(product) {
    set((state) => {
      const existing = state.lines.find((l) => l.productId === product.id);
      if (existing) {
        return {
          lines: state.lines.map((l) =>
            l.productId === product.id ? { ...l, quantity: l.quantity + 1 } : l,
          ),
        };
      }
      return {
        lines: [
          ...state.lines,
          {
            productId: product.id,
            productName: product.name,
            unitPriceAmount: product.priceAmount,
            unitPriceCurrency: product.priceCurrency,
            quantity: 1,
          },
        ],
      };
    });
  },

  increment(productId) {
    set((state) => ({
      lines: state.lines.map((l) =>
        l.productId === productId ? { ...l, quantity: l.quantity + 1 } : l,
      ),
    }));
  },

  decrement(productId) {
    set((state) => ({
      lines: state.lines
        .map((l) =>
          l.productId === productId ? { ...l, quantity: l.quantity - 1 } : l,
        )
        .filter((l) => l.quantity > 0),
    }));
  },

  remove(productId) {
    set((state) => ({
      lines: state.lines.filter((l) => l.productId !== productId),
    }));
  },

  clear() {
    set({ lines: [] });
  },
}));
