

import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    // Ruta vacía → redirige al login automáticamente
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
    path: 'home',
    loadComponent: () =>
      import('./home/home.page').then(m => m.HomePage)
  },
  {
    path: 'registro',
    loadComponent: () => import('./pages/registro/registro.page').then( m => m.RegistroPage)
  },
  {
    path: 'home',
    loadComponent: () => import('./pages/home/home.page').then( m => m.HomePage)
  }
];
