import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CartService } from './cart';

export const checkoutGuard: CanActivateFn = () =>
  inject(CartService).quantity() > 0 || inject(Router).createUrlTree(['/cart']);
