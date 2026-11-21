import { Routes } from '@angular/router';
import { checkoutGuard } from './core/checkout-guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'products' },
  { path: 'products', title: 'Products | Circuit Supply', loadComponent: () => import('./features/products/product-list/product-list').then(m => m.ProductList) },
  { path: 'products/:id', title: 'Product details | Circuit Supply', loadComponent: () => import('./features/products/product-detail').then(m => m.ProductDetail) },
  { path: 'cart', title: 'Cart | Circuit Supply', loadComponent: () => import('./features/cart/cart-page').then(m => m.CartPage) },
  { path: 'checkout', title: 'Checkout | Circuit Supply', canActivate: [checkoutGuard], loadComponent: () => import('./features/checkout/checkout-page').then(m => m.CheckoutPage) },
  { path: 'payment/success', title: 'Payment status | Circuit Supply', loadComponent: () => import('./features/orders/order-pages').then(m => m.OrderDetail), data: { payment: true } },
  { path: 'payment/cancel', title: 'Checkout cancelled | Circuit Supply', loadComponent: () => import('./features/orders/order-pages').then(m => m.PaymentCancel) },
  { path: 'orders', title: 'Orders | Circuit Supply', loadComponent: () => import('./features/orders/order-pages').then(m => m.OrderList) },
  { path: 'orders/:id', title: 'Order details | Circuit Supply', loadComponent: () => import('./features/orders/order-pages').then(m => m.OrderDetail) },
  { path: '**', redirectTo: 'products' },
];
