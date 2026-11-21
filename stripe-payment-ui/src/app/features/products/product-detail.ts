import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, map, of, startWith, switchMap } from 'rxjs';
import { errorMessage, NotificationService, ProductService } from '../../core/api';
import { CartService } from '../../core/cart';
import { Product } from '../../core/models';
import { Loading } from '../../shared/store-ui';

@Component({
  imports: [RouterLink, CurrencyPipe, Loading],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a routerLink="/products" class="back">← All products</a>
    @if (state(); as result) {
      @if (result.kind === 'product') {
        <section class="detail-grid">
          <img [src]="result.product.imageUrl" [alt]="result.product.name">
          <div class="panel">
            <span class="category">{{ result.product.category }}</span>
            <h1>{{ result.product.name }}</h1>
            <p>{{ result.product.description }}</p>
            <p class="large-price">{{ result.product.price | currency:'INR' }}</p>
            <button (click)="add(result.product)">Add to cart</button>
          </div>
        </section>
      } @else if (result.kind === 'error') {
        <p role="alert">{{ result.message }}</p>
      } @else { <app-loading label="Loading product…" /> }
    }
  `,
})
export class ProductDetail {
  private readonly api = inject(ProductService);
  private readonly cart = inject(CartService);
  private readonly notifications = inject(NotificationService);
  protected readonly state = toSignal(inject(ActivatedRoute).paramMap.pipe(
    switchMap(params => this.api.get(params.get('id') ?? '').pipe(
      map(product => ({ kind: 'product' as const, product })),
      catchError(error => of({ kind: 'error' as const, message: errorMessage(error) })),
      startWith({ kind: 'loading' as const }),
    )),
  ));
  protected add(product: Product) {
    const before = this.cart.quantity();
    this.cart.add(product);
    this.notifications.show(this.cart.quantity() > before ? product.name + ' added to your cart' : 'Limit of 10 per product');
  }
}
