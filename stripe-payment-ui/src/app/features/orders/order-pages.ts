import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, combineLatest, concat, distinctUntilChanged, filter, forkJoin, map, mergeMap, of, startWith, switchMap, take } from 'rxjs';
import { errorMessage, OrderService, PaymentService } from '../../core/api';
import { Loading, OrderReference } from '../../shared/store-ui';

@Component({
  imports: [RouterLink, DatePipe, CurrencyPipe, OrderReference, Loading],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>Orders</h1>
    @if (state(); as result) {
      @if (result.kind === 'loading') { <app-loading label="Loading orders…" /> }
      @else if (result.kind === 'error') { <p role="alert">{{ result.message }}</p> }
      @else {
        <section class="panel">
          @for (order of result.orders; track order.id) {
            <a class="order-row" [routerLink]="['/orders', order.id]">
              <strong>{{ order.id | orderReference }}</strong><time>{{ order.createdAt | date:'mediumDate' }}</time>
              <span class="status" [attr.data-status]="order.status">{{ order.status }}</span><span>{{ order.total | currency:'INR' }}</span>
            </a>
          } @empty { <p>No orders have been placed from this browser.</p><a class="button" routerLink="/products">Browse products</a> }
        </section>
      }
    }
  `,
})
export class OrderList {
  protected readonly state = toSignal(inject(OrderService).list().pipe(
    map(orders => ({ kind: 'orders' as const, orders })),
    catchError(error => of({ kind: 'error' as const, message: errorMessage(error) })),
    startWith({ kind: 'loading' as const }),
  ));
}

@Component({
  imports: [RouterLink, CurrencyPipe, DatePipe, OrderReference, Loading],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a routerLink="/orders" class="back">← Order history</a>
    <h1>{{ payment ? 'Payment status' : 'Order details' }}</h1>
    @if (state(); as result) {
      @if (result.kind === 'loading') { <app-loading label="Loading order…" /> }
      @else if (result.kind === 'error') { <p role="alert">{{ result.message }}</p> }
      @else {
        <section class="panel order-detail">
          <div class="line"><h2>Order {{ result.order.id | orderReference }}</h2>
            <span class="status" [attr.data-status]="result.order.status">{{ result.order.status }}</span></div>
          <p class="muted order-id">Order ID {{ result.order.id }}</p>
          <p class="muted">{{ result.order.createdAt | date:'medium' }}</p>
          @switch (result.order.status) {
            @case ('Paid') { <p>Your payment has been confirmed.</p> }
            @case ('Pending') { <p role="status">Waiting for Stripe confirmation. This page checks for updates for up to two minutes.</p> }
            @case ('Expired') { <p>This checkout session expired.</p> }
            @case ('Failed') { <p>The payment failed.</p> }
          }
          @for (item of result.order.items; track $index) {
            <p class="line"><span>{{ item.name }} × {{ item.quantity }}</span><span>{{ item.unitPrice * item.quantity | currency:'INR' }}</span></p>
          }
          <p class="line total"><strong>Total</strong><strong>{{ result.order.total | currency:'INR' }}</strong></p>
        </section>
      }
    }
  `,
})
export class OrderDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly orders = inject(OrderService);
  private readonly payments = inject(PaymentService);
  protected readonly payment = this.route.snapshot.data['payment'] === true;
  protected readonly state = toSignal(combineLatest([this.route.paramMap, this.route.queryParamMap]).pipe(
    map(([params, query]) => params.get('id') ?? query.get('orderId') ?? ''),
    distinctUntilChanged(),
    switchMap(id => forkJoin({ order: this.orders.get(id), status: this.orders.status(id) }).pipe(
      map(({ order, status }) => ({ ...order, status: status.status })),
      switchMap(order => concat(of(order), this.payments.watch(id).pipe(
        filter(result => result.status !== order.status),
        take(1),
        mergeMap(() => this.orders.get(id)),
      )).pipe(take(order.status === 'Pending' ? 2 : 1))),
      map(order => ({ kind: 'order' as const, order })),
      catchError(error => of({ kind: 'error' as const, message: errorMessage(error) })),
      startWith({ kind: 'loading' as const }),
    )),
  ));
}

@Component({
  imports: [RouterLink, OrderReference],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>Checkout cancelled</h1>
    <section class="panel order-detail">
      <p>You left Stripe Checkout. Your cart is still available.</p>
      @if (orderId(); as id) {
        <p>Order <a [routerLink]="['/orders', id]">{{ id | orderReference }}</a></p>
      }
      <a class="button" routerLink="/cart">Return to cart</a>
    </section>
  `,
})
export class PaymentCancel {
  protected readonly orderId = toSignal(inject(ActivatedRoute).queryParamMap.pipe(map(params => params.get('orderId'))));
}
