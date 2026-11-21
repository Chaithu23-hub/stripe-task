import { FormsModule } from '@angular/forms';
import { ChangeDetectionStrategy, Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Store } from '@ngrx/store';
import { BehaviorSubject, combineLatest, debounceTime, distinctUntilChanged, startWith } from 'rxjs';
import { NotificationService } from '../../../core/api';
import { CartService } from '../../../core/cart';
import { Product } from '../../../core/models';
import { ProductCard } from '../../../shared/product-card';
import { catalog, catalogActions } from '../../../store/catalog';

@Component({
  selector: 'app-product-list',
  imports: [FormsModule, ProductCard],
  templateUrl: './product-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductList implements OnInit, OnDestroy {
  private readonly store = inject(Store);
  private readonly cart = inject(CartService);
  private readonly notifications = inject(NotificationService);
  protected readonly products = this.store.selectSignal(catalog.selectProducts);
  protected readonly loading = this.store.selectSignal(catalog.selectLoading);
  protected readonly error = this.store.selectSignal(catalog.selectError);
  protected readonly visible = signal(true);
  protected searchText = '';
  protected readonly query = new BehaviorSubject('');
  protected readonly category = new BehaviorSubject('');
  protected readonly sort = new BehaviorSubject('');

  constructor() {
    combineLatest([
      this.query.pipe(debounceTime(250), startWith(''), distinctUntilChanged()),
      this.category.pipe(distinctUntilChanged()),
      this.sort.pipe(distinctUntilChanged()),
    ]).pipe(takeUntilDestroyed()).subscribe(([query, category, sort]) =>
      this.store.dispatch(catalogActions.load({ query, category, sort })),
    );
  }

  ngOnInit() { console.debug('ProductList initialized'); }
  ngOnDestroy() { console.debug('ProductList destroyed'); }

  protected reload() {
    this.store.dispatch(catalogActions.load({
      query: this.searchText, category: this.category.value, sort: this.sort.value,
    }));
  }

  protected addToCart(product: Product) {
    const before = this.cart.quantity();
    this.cart.add(product);
    this.notifications.show(this.cart.quantity() > before ? product.name + ' added to your cart' : 'Limit of 10 per product');
  }
}
