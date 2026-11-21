import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject, Injectable, isDevMode, signal } from '@angular/core';
import { catchError, exhaustMap, retry, takeUntil, takeWhile, throwError, timer } from 'rxjs';
import { CartLine, Customer, Order, PaymentStatus, Product } from './models';

const API = '/api';
export function errorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return 'Cannot reach the store. Try again shortly.';
    return error.error?.detail ?? error.error?.title ?? 'The request could not be completed.';
  }
  return 'The request could not be completed.';
}

export const apiInterceptor: HttpInterceptorFn = (request, next) => {
  if (isDevMode()) console.debug(request.method, request.url);
  return next(request).pipe(retry({
    count: request.method === 'GET' ? 2 : 0,
    delay: (error: HttpErrorResponse, attempt) =>
      [0, 502, 503, 504].includes(error.status) ? timer(attempt * 300) : throwError(() => error),
  }), catchError(error => {
    if (isDevMode()) console.debug('Request failed', request.method, error.status);
    return throwError(() => error);
  }));
};

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  search(query: string, category: string, sort: string) {
    return this.http.get<Product[]>(API + '/products', { params: { query, category, sort } });
  }
  get(id: string) { return this.http.get<Product>(API + '/products/' + encodeURIComponent(id)); }
}

@Injectable({ providedIn: 'root' })
export class CheckoutService {
  private readonly http = inject(HttpClient);
  create(customer: Customer, lines: readonly CartLine[], requestId: string) {
    return this.http.post<{ id: string; url: string }>(API + '/checkout/session', {
      customer, requestId, items: lines.map(line => ({ productId: line.product.id, quantity: line.quantity })),
    });
  }
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  list() { return this.http.get<Order[]>(API + '/orders'); }
  get(id: string) { return this.http.get<Order>(API + '/orders/' + encodeURIComponent(id)); }
  status(id: string) {
    return this.http.get<{ status: PaymentStatus }>(API + '/orders/' + encodeURIComponent(id) + '/status');
  }
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly orders = inject(OrderService);
  watch(id: string) {
    return timer(0, 2000).pipe(
      exhaustMap(() => this.orders.status(id)),
      takeWhile(result => result.status === 'Pending', true),
      takeUntil(timer(120000)),
    );
  }
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly message = signal('');
  readonly text = this.message.asReadonly();
  private timeout?: ReturnType<typeof setTimeout>;
  show(message: string) {
    clearTimeout(this.timeout);
    this.message.set(message);
    this.timeout = setTimeout(() => this.message.set(''), 3000);
  }
}
