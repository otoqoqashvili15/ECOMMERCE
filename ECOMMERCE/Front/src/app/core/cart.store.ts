import { computed, Injectable, signal } from '@angular/core';
import { CartLine, Product } from './store.models';

@Injectable({ providedIn: 'root' })
export class CartStore {
  private readonly storageKey = 'atelier.cart';
  private readonly state = signal<CartLine[]>(this.readStorage());
  readonly lines = this.state.asReadonly();
  readonly count = computed(() => this.state().reduce((total, line) => total + line.quantity, 0));
  readonly subtotal = computed(() => this.state().reduce(
    (total, line) => total + line.product.effectivePrice * line.quantity,
    0,
  ));

  add(product: Product): void {
    this.state.update(lines => {
      const existing = lines.find(line => line.product.id === product.id);
      if (existing) {
        return lines.map(line => line.product.id === product.id
          ? { ...line, product, quantity: Math.min(product.stock, line.quantity + 1) }
          : line);
      }
      return [...lines, { product, quantity: 1 }];
    });
    this.persist();
  }

  setQuantity(productId: number, quantity: number): void {
    this.state.update(lines => lines
      .map(line => line.product.id === productId
        ? { ...line, quantity: Math.max(1, Math.min(line.product.stock, quantity)) }
        : line));
    this.persist();
  }

  remove(productId: number): void {
    this.state.update(lines => lines.filter(line => line.product.id !== productId));
    this.persist();
  }

  replace(lines: CartLine[]): void {
    this.state.set(lines);
    this.persist();
  }

  clear(): void {
    this.state.set([]);
    this.persist();
  }

  private persist(): void {
    localStorage.setItem(this.storageKey, JSON.stringify(this.state()));
  }

  private readStorage(): CartLine[] {
    try {
      const raw = localStorage.getItem(this.storageKey);
      return raw ? (JSON.parse(raw) as CartLine[]) : [];
    } catch {
      return [];
    }
  }
}
