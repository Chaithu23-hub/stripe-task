import { CurrencyPipe, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, OnChanges, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Product } from '../core/models';

@Component({
  selector: 'app-product-card',
  imports: [CurrencyPipe, NgClass, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="product-card" [ngClass]="{ 'premium': premium() }">
      <a [routerLink]="['/products', product().id]" class="product-image">
        <img [src]="product().imageUrl" [alt]="product().name" loading="lazy">
      </a>
      <div class="product-body">
        <span class="category">{{ product().category }}</span>
        <h2><a [routerLink]="['/products', product().id]">{{ product().name }}</a></h2>
        <p>{{ product().description }}</p>
        <div class="line"><strong>{{ product().price | currency:'INR':'symbol':'1.0-2' }}</strong>
          <button type="button" (click)="added.emit(product())">Add to cart</button>
        </div>
      </div>
    </article>
  `,
})
export class ProductCard implements OnChanges {
  readonly product = input.required<Product>();
  readonly added = output<Product>();
  protected readonly premium = signal(false);
  ngOnChanges() { this.premium.set(this.product().price >= 50000); }
}
