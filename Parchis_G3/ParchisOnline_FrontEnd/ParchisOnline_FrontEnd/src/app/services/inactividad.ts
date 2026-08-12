import { Injectable, NgZone, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ToastController } from '@ionic/angular/standalone';
import { AuthService } from './auth';


@Injectable({
  providedIn: 'root'
})
export class InactividadService {

  private readonly MINUTOS_LIMITE = 30;
 private readonly MINUTOS_AVISO  = 2;

  private temporizador: any = null;
  private temporizadorAviso: any = null;
  private activo = false;

  // Eventos que cuentan como actividad. Van con passive: true para
  // no bloquear el scroll en móvil.
  private readonly EVENTOS = [
    'click', 'touchstart', 'keydown', 'scroll', 'mousemove'
  ];

  // Se guarda la referencia para poder quitar los listeners después
  private readonly manejador = () => this.reiniciar();

  private router           = inject(Router);
  private authService      = inject(AuthService);
  private toastController  = inject(ToastController);
  private zone             = inject(NgZone);

  /**
   * Arranca la vigilancia. Se llama al iniciar sesión.
   */
  iniciar(): void {
    if (this.activo) return;

    this.activo = true;

    // Los listeners corren fuera de la zona de Angular: sin esto,
    // mousemove dispararía detección de cambios en cada píxel que
    // se mueve el cursor y la app se arrastraría.
    this.zone.runOutsideAngular(() => {
      for (const evento of this.EVENTOS) {
        document.addEventListener(evento, this.manejador, { passive: true });
      }
    });

    this.reiniciar();
  }

  /**
   * Detiene la vigilancia. Se llama al cerrar sesión.
   */
  detener(): void {
    if (!this.activo) return;

    this.activo = false;

    for (const evento of this.EVENTOS) {
      document.removeEventListener(evento, this.manejador);
    }

    this.limpiarTemporizadores();
  }

  /**
   * Reinicia la cuenta. Lo llama cada evento de actividad.
   */
  private reiniciar(): void {
    if (!this.activo) return;

    this.limpiarTemporizadores();

    const msLimite = this.MINUTOS_LIMITE * 60 * 1000;
    const msAviso  = msLimite - (this.MINUTOS_AVISO * 60 * 1000);

    this.temporizadorAviso = setTimeout(() => {
      // Volvemos a la zona de Angular para que el toast se renderice
      this.zone.run(() => this.avisarProximoCierre());
    }, msAviso);

    this.temporizador = setTimeout(() => {
      this.zone.run(() => this.cerrarPorInactividad());
    }, msLimite);
  }

  private limpiarTemporizadores(): void {
    if (this.temporizador)      clearTimeout(this.temporizador);
    if (this.temporizadorAviso) clearTimeout(this.temporizadorAviso);

    this.temporizador = null;
    this.temporizadorAviso = null;
  }

  private async avisarProximoCierre(): Promise<void> {
    const toast = await this.toastController.create({
      message:  `Tu sesión se cerrará en ${this.MINUTOS_AVISO} minutos por inactividad.`,
      duration: 5000,
      color:    'warning',
      position: 'top'
    });
    await toast.present();
  }

  private async cerrarPorInactividad(): Promise<void> {
    this.detener();
    this.authService.logout();

    const toast = await this.toastController.create({
      message:  'Tu sesión se cerró por inactividad.',
      duration: 4000,
      color:    'medium',
      position: 'top'
    });
    await toast.present();

    this.router.navigate(['/login'], { replaceUrl: true });
  }

  /**
   * Minutos que faltan para el cierre. Útil si alguna pantalla
   * quiere mostrarlo.
   */
  get minutosLimite(): number {
    return this.MINUTOS_LIMITE;
  }
}