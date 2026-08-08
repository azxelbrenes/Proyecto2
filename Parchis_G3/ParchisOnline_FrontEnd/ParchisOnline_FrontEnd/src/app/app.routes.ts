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
    loadComponent: () => import('./pages/tienda/tienda.page').then( m => m.TiendaPage)
  },
  {
    path: 'perfil',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/perfil/perfil.page').then( m => m.PerfilPage)
  },
  {
    path: 'sala-espera',
    loadComponent: () => import('./pages/sala-espera/sala-espera.page').then( m => m.SalaEsperaPage)
  },
  {
    path: 'tablero',
    loadComponent: () => import('./pages/tablero/tablero.page').then( m => m.TableroPage)
  }
];