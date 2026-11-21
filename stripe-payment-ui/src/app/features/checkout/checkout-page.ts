import { HttpErrorResponse } from '@angular/common/http';
import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize, from, switchMap } from 'rxjs';
import { CheckoutService, errorMessage } from '../../core/api';
import { CartService } from '../../core/cart';
import { CartLine, Customer } from '../../core/models';
import { BusyButton } from '../../shared/store-ui';

export function indianPostalCode(control: AbstractControl) {
  return /^[1-9][0-9]{5}$/.test(control.value) ? null : { indianPostalCode: true };
}

function nonBlank(control: AbstractControl) {
  return typeof control.value === 'string' && control.value.trim() ? null : { required: true };
}

async function requestKey(customer: Customer, lines: readonly CartLine[]): Promise<{ id: string; key: string }> {
  const items = lines.map(line => [line.product.id, line.quantity]).sort(([a], [b]) => a - b);
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(JSON.stringify({ customer, items })));
  const hash = Array.from(new Uint8Array(digest), byte => byte.toString(16).padStart(2, '0')).join('');
  const key = 'stripe-attempt-' + hash;
  const existing = localStorage.getItem(key);
  if (existing) return { id: existing, key };
  const id = crypto.randomUUID();
  localStorage.setItem(key, id);
  return { id, key };
}

@Component({
  imports: [ReactiveFormsModule, CurrencyPipe, RouterLink, BusyButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a routerLink="/cart" class="back">← Shopping cart</a>
    <h1>Checkout</h1>
    <div class="checkout-grid">
      <form class="panel" [formGroup]="form" (ngSubmit)="submit()">
        <h2>Contact and delivery details</h2>
        <div class="field-grid">
          @for (field of fields; track field.name) {
            @let control = form.controls[field.name];
            @let invalid = (control.touched || control.dirty) && control.invalid;
            <div [class.full]="field.name === 'address'">
              <label [for]="field.name">{{ field.label }}</label>
              <input [id]="field.name" [formControlName]="field.name" [type]="field.type"
                [attr.autocomplete]="field.autocomplete" [attr.aria-invalid]="invalid"
                [attr.aria-describedby]="invalid ? field.name + '-error' : null">
              @if (invalid) { <p [id]="field.name + '-error'" class="field-error">{{ field.error }}</p> }
            </div>
          }
        </div>
        @if (error()) { <p class="field-error" role="alert">{{ error() }}</p> }
        <button type="submit" [appBusy]="submitting()" [blocked]="form.invalid || !cart.quantity()">
          {{ submitting() ? 'Opening Stripe…' : 'Continue to Stripe' }}
        </button>
        <p class="muted">Enter test card details on Stripe Checkout.</p>
      </form>
      <aside class="panel summary">
        <h2>Order summary</h2>
        @for (line of cart.lines(); track line.product.id) {
          <p class="line"><span>{{ line.product.name }} × {{ line.quantity }}</span>
            <span>{{ line.product.price * line.quantity | currency:'INR' }}</span></p>
        }
        <p class="line total"><strong>Total</strong><strong>{{ cart.total() | currency:'INR' }}</strong></p>
      </aside>
    </div>
  `,
})
export class CheckoutPage {
  protected readonly cart = inject(CartService);
  private readonly checkout = inject(CheckoutService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly builder = inject(FormBuilder).nonNullable;
  protected readonly submitting = signal(false);
  protected readonly error = signal('');
  protected readonly form = this.builder.group({
    name: ['', [nonBlank, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
    phone: ['', [Validators.required, Validators.pattern(/^\+?[0-9 ()-]{7,20}$/)]],
    address: ['', [nonBlank, Validators.maxLength(250)]],
    city: ['', [nonBlank, Validators.maxLength(100)]],
    state: ['', [nonBlank, Validators.maxLength(100)]],
    postalCode: ['', [Validators.required, indianPostalCode]],
  });
  protected readonly fields: { name: keyof Customer; label: string; type: string; autocomplete: string; error: string }[] = [
    { name: 'name', label: 'Full name', type: 'text', autocomplete: 'name', error: 'Enter your full name (up to 100 characters).' },
    { name: 'email', label: 'Email', type: 'email', autocomplete: 'email', error: 'Enter a valid email address.' },
    { name: 'phone', label: 'Phone', type: 'tel', autocomplete: 'tel', error: 'Enter a valid phone number.' },
    { name: 'address', label: 'Street address', type: 'text', autocomplete: 'street-address', error: 'Enter your street address.' },
    { name: 'city', label: 'City', type: 'text', autocomplete: 'address-level2', error: 'Enter your city.' },
    { name: 'state', label: 'State', type: 'text', autocomplete: 'address-level1', error: 'Enter your state.' },
    { name: 'postalCode', label: 'PIN code', type: 'text', autocomplete: 'postal-code', error: 'Enter a six-digit Indian PIN code.' },
  ];

  protected submit() {
    if (this.form.invalid || this.submitting() || !this.cart.quantity()) return;
    const customer = this.form.getRawValue();
    const lines = this.cart.lines();
    this.error.set('');
    this.submitting.set(true);
    let storageKey: string | undefined;
    from(requestKey(customer, lines)).pipe(
      switchMap(({ id, key }) => {
        storageKey = key;
        return this.checkout.create(customer, lines, id);
      }),
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.submitting.set(false)),
    ).subscribe({
      next: ({ url }) => window.location.assign(url),
      error: (error: unknown) => {
        if (storageKey && error instanceof HttpErrorResponse && error.error?.attemptClosed === true) {
          localStorage.removeItem(storageKey);
        }
        this.error.set(errorMessage(error));
      },
    });
  }
}
