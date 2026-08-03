import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './services/auth';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router       = inject(Router);

  // Si hay token, dejamos pasar
  if (authService.estaAutenticado()) {
    return true;
  }

  // Si no hay token, redirigimos al login y bloqueamos el acceso
  router.navigate(['/login']);
  return false;
};