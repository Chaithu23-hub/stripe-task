import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CartService } from '../../core/cart';

@Component({
  imports: [RouterLink, CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>Shopping cart</h1>
    @if (cart.quantity()) {
      <div class="checkout-grid">
        <section class="panel" aria-label="Cart items">
          @for (line of cart.lines(); track line.product.id) {
            <article class="cart-row">
              <img [src]="line.product.imageUrl" alt="">
              <div><a [routerLink]="['/products', line.product.id]">{{ line.product.name }}</a>
                <p>{{ line.product.price | currency:'INR' }} each</p>
                <div class="quantity">
                  <button type="button" class="secondary" [disabled]="line.quantity <= 1"
                    [attr.aria-label]="'Decrease quantity of ' + line.product.name"
                    (click)="cart.setQuantity(line.product.id, line.quantity - 1)">−</button>
                  <span>{{ line.quantity }}</span>
                  <button type="button" class="secondary" [disabled]="line.quantity >= 10"
                    [attr.aria-label]="'Increase quantity of ' + line.product.name"
                    (click)="cart.setQuantity(line.product.id, line.quantity + 1)">+</button>
                </div>
              </div>
              <button type="button" class="secondary" [attr.aria-label]="'Remove ' + line.product.name"
                (click)="cart.remove(line.product.id)">Remove</button>
            </article>
          }
        </section>
        <aside class="panel summary">
          <h2>Order summary</h2>
          <p class="line"><span>Subtotal</span><span>{{ cart.total() | currency:'INR' }}</span></p>
          <p class="line total"><strong>Total</strong><strong>{{ cart.total() | currency:'INR' }}</strong></p>
          <a class="button" routerLink="/checkout">Continue to checkout</a>
        </aside>
      </div>
    } @else {
      <section class="panel"><p>Your cart is empty.</p><a class="button" routerLink="/products">Browse products</a></section>
    }
  `,
})
export class CartPage {
  protected readonly cart = inject(CartService);
}
