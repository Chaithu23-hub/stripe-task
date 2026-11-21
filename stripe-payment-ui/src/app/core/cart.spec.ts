import { CartService } from './cart';
import { TestBed } from '@angular/core/testing';

it('calculates totals and rejects invalid quantities', () => {
  localStorage.clear();
  const cart = TestBed.inject(CartService);
  const product = { id: 1, name: 'Laptop', price: 50000, description: '', category: '', imageUrl: '' };
  cart.add(product);
  cart.add(product);
  cart.setQuantity(1, -1);
  expect(cart.quantity()).toBe(2);
  expect(cart.total()).toBe(100000);
  cart.remove(1);
  expect(cart.total()).toBe(0);
});
