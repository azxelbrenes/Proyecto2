import { Component, OnInit, inject } from '@angular/core';
import { IonApp, IonRouterOutlet } from '@ionic/angular/standalone';
import { InactividadService } from '../app/services/inactividad';
import { ConexionService } from '../app/services/conexion';
import { AuthService } from './services/auth';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  imports: [IonApp, IonRouterOutlet],
})
export class AppComponent implements OnInit {

  private inactividad = inject(InactividadService);
  private authService = inject(AuthService);

  // ConexionService se inyecta aunque no se use directamente: su
  // constructor engancha los listeners de online/offline, y sin la
  // inyección Angular nunca lo instancia (RF-19).
  private conexion = inject(ConexionService);

  ngOnInit(): void {
    // Si la app arranca con sesión abierta, el reloj de inactividad
    // empieza acá. Al iniciar sesión hay que llamar a iniciar() desde
    // login.page.ts, porque este ngOnInit ya pasó.
    if (this.authService.getToken()) {
      this.inactividad.iniciar();
    }
  }
}