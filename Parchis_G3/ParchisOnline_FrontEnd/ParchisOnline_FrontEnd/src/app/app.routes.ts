import { Routes } from '@angular/router';
import { authGuard } from './auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login.page').then(m => m.LoginPage)
  },
  {
    path: 'registro',
    loadComponent: () =>
      import('./pages/registro/registro.page').then(m => m.RegistroPage)
  },
  {
    path: 'home',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/home/home.page').then(m => m.HomePage)
  },
  {
    path: 'tienda',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/tienda/tienda.page').then(m => m.TiendaPage)
  },
  {
    path: 'perfil',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/perfil/perfil.page').then(m => m.PerfilPage)
  },
  {
    // Faltaba el guard: sin él cualquiera podía entrar
    // escribiendo la URL directamente sin estar logueado
    path: 'sala-espera',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/sala-espera/sala-espera.page').then(m => m.SalaEsperaPage)
  },
  {
    // Faltaba el guard
    path: 'tablero',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/tablero/tablero.page').then(m => m.TableroPage)
  },
  {
    // Faltaba el guard — esta es la más importante porque
    // maneja pagos con dinero real
    path: 'tienda-monedas',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/tienda-monedas/tienda-monedas.page').then(m => m.TiendaMonedasPage)
  }
];
