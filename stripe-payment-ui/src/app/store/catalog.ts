import { inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { createActionGroup, createFeature, createReducer, on, props } from '@ngrx/store';
import { catchError, map, of, switchMap } from 'rxjs';
import { errorMessage, ProductService } from '../core/api';
import { Product } from '../core/models';

export const catalogActions = createActionGroup({
  source: 'Catalog',
  events: {
    Load: props<{ query: string; category: string; sort: string }>(),
    Loaded: props<{ products: Product[] }>(),
    Failed: props<{ error: string }>(),
  },
});

interface CatalogState { products: Product[]; loading: boolean; error: string }
const initial: CatalogState = { products: [], loading: false, error: '' };

export const catalog = createFeature({
  name: 'catalog',
  reducer: createReducer(initial,
    on(catalogActions.load, state => ({ ...state, loading: true, error: '' })),
    on(catalogActions.loaded, (_, { products }) => ({ products, loading: false, error: '' })),
    on(catalogActions.failed, (_, { error }) => ({ products: [], loading: false, error })),
  ),
});

export const loadCatalog = createEffect((actions = inject(Actions), api = inject(ProductService)) =>
  actions.pipe(
    ofType(catalogActions.load),
    switchMap(({ query, category, sort }) => api.search(query, category, sort).pipe(
      map(products => catalogActions.loaded({ products })),
      catchError(error => of(catalogActions.failed({ error: errorMessage(error) }))),
    )),
  ), { functional: true });
