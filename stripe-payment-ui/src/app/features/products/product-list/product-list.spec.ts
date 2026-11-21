import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideState, provideStore } from '@ngrx/store';
import { catalog, loadCatalog } from '../../../store/catalog';
import { ProductList } from './product-list';

describe('ProductList', () => {
  it('renders products returned by the catalog effect', async () => {
    TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [
        provideRouter([]), provideHttpClient(), provideHttpClientTesting(), provideStore(),
        provideState(catalog), provideEffects({ loadCatalog }),
      ],
    });
    const fixture = TestBed.createComponent(ProductList);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/products' && request.params.get('query') === '').flush([
      { id: 1, name: 'Laptop', description: 'A notebook', category: 'Computers', imageUrl: '/products/laptop.jpg', price: 50000 },
    ]);
    await fixture.whenStable();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent).toBe('Stripe Payment Application');
    expect(root.querySelector('.product-card h2')?.textContent).toContain('Laptop');
    http.verify();
  });
});
