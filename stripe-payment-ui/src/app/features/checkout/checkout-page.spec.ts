import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { CheckoutService } from '../../core/api';
import { routes } from '../../app.routes';
import { indianPostalCode, CheckoutPage } from './checkout-page';

describe('checkout', () => {
  it('validates Indian postal codes', () => {
    expect(indianPostalCode(new FormControl('012345'))).toEqual({ indianPostalCode: true });
    expect(indianPostalCode(new FormControl('560001'))).toBeNull();
  });

  it('redirects an empty cart to the cart page', async () => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter(routes)] });
    const harness = await RouterTestingHarness.create('/checkout');
    expect(harness.routeNativeElement?.textContent).toContain('Your cart is empty');
  });

  it('disables checkout for invalid form values', async () => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient()] });
    const fixture = TestBed.createComponent(CheckoutPage);
    await fixture.whenStable();
    const page: HTMLElement = fixture.nativeElement;
    expect(page.querySelector<HTMLButtonElement>('button[type=submit]')?.disabled).toBe(true);
  });

  it('sends only product IDs and quantities for cart lines', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const customer = { name: 'Test Buyer', email: 'buyer@example.com', phone: '9876543210',
      address: '10 Main St', city: 'Bengaluru', state: 'Karnataka', postalCode: '560001' };
    const product = { id: 1, name: 'Laptop', description: '', category: '', imageUrl: '', price: 50000 };
    TestBed.inject(CheckoutService).create(customer, [{ product, quantity: 2 }], 'reference').subscribe();
    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne('/api/checkout/session');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ customer, requestId: 'reference', items: [{ productId: 1, quantity: 2 }] });
    request.flush({ id: 'order', url: 'https://checkout.stripe.com/' });
    http.verify();
  });
});
