import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './services/auth';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router      = inject(Router);

  const token = authService.getToken();

  // ── Sin token: fuera ─────────────────────────────────────────
  if (!token) {
    router.navigate(['/login'], { replaceUrl: true });
    return false;
  }

  // ── Con token pero vencido: fuera también ────────────────────
  if (tokenVencido(token)) {
    // Limpiamos la sesión muerta para que no quede basura
    authService.logout();
    router.navigate(['/login'], { replaceUrl: true });
    return false;
  }

  return true;
};


// Un JWT tiene 3 partes separadas por punto:
//   header.payload.firma
// El payload está en Base64 y contiene "exp" = fecha de expiración
// en segundos desde 1970 (Unix timestamp).
//
// NOTA: esto es solo una validación de conveniencia para la UX.
// La seguridad REAL la hace el backend, que valida la firma
// criptográfica del token en cada request. Un atacante podría
// falsificar el payload aquí, pero el backend lo rechazaría.
function tokenVencido(token: string): boolean {
  try {
    const partes = token.split('.');
    if (partes.length !== 3) return true;  // No es un JWT válido

    // Decodificamos el payload (segunda parte)
    const payload = JSON.parse(atob(partes[1]));

    if (!payload.exp) return false;  // Sin fecha de expiración

    // exp viene en segundos, Date.now() en milisegundos
    const ahora = Math.floor(Date.now() / 1000);

    return payload.exp < ahora;
  } catch {
    // Si no se puede decodificar, lo tratamos como inválido
    return true;
  }
}
