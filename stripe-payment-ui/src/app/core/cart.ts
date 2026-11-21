import { computed, effect, Injectable, signal } from '@angular/core';
import { CartLine, Product } from './models';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly state = signal<CartLine[]>(this.restore());
  readonly lines = this.state.asReadonly();
  readonly quantity = computed(() => this.lines().reduce((sum, line) => sum + line.quantity, 0));
  readonly total = computed(() => this.lines().reduce(
    (sum, line) => sum + Math.round(line.product.price * 100) * line.quantity, 0) / 100);

  constructor() {
    effect(() => {
      const lines = this.lines();
      try { localStorage.setItem('stripe-cart-v1', JSON.stringify(lines)); } catch { /* Optional persistence */ }
    });
  }

  add(product: Product) {
    const line = this.lines().find(item => item.product.id === product.id);
    if (line) {
      this.setQuantity(product.id, line.quantity + 1);
    } else {
      this.state.update(lines => [...lines, { product, quantity: 1 }]);
    }
  }

  setQuantity(id: number, quantity: number) {
    if (!Number.isInteger(quantity) || quantity < 1 || quantity > 10) return;
    this.state.update(lines => lines.map(line => line.product.id === id ? { ...line, quantity } : line));
  }

  remove(id: number) {
    this.state.update(lines => lines.filter(line => line.product.id !== id));
  }

  private restore(): CartLine[] {
    try {
      const saved: unknown = JSON.parse(localStorage.getItem('stripe-cart-v1') ?? '[]');
      if (!Array.isArray(saved)) return [];
      return saved.filter((line): line is CartLine => line?.product &&
        Number.isInteger(line.product.id) && Number.isFinite(line.product.price) &&
        typeof line.product.name === 'string' && Number.isInteger(line.quantity) &&
        line.quantity >= 1 && line.quantity <= 10);
    } catch { return []; }
  }
}
