import { Injectable, NgZone, inject } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { ToastController } from '@ionic/angular/standalone';


@Injectable({
  providedIn: 'root'
})
export class ConexionService {

  private apiUrl = 'http://localhost:5051/api';

  // BehaviorSubject para que una pantalla que se suscriba tarde
  // igual sepa el estado actual
  public enLinea$ = new BehaviorSubject<boolean>(navigator.onLine);
  public apiDisponible$ = new BehaviorSubject<boolean>(true);

  private toastSinConexion: HTMLIonToastElement | null = null;

  private toastController = inject(ToastController);
  private zone = inject(NgZone);

  constructor() {
    this.escucharCambios();
  }

  private escucharCambios(): void {
    this.zone.runOutsideAngular(() => {

      window.addEventListener('offline', () => {
        this.zone.run(() => {
          this.enLinea$.next(false);
          this.mostrarSinConexion();
        });
      });

      window.addEventListener('online', () => {
        this.zone.run(() => {
          this.enLinea$.next(true);
          this.ocultarSinConexion();
          this.mostrarReconectado();
        });
      });
    });
  }

  /**
   * Comprueba que la API responda. Detecta el caso de tener internet
   * pero con el servidor caído o en mantenimiento, que navigator
   * .onLine no puede ver.
   */
  async verificarApi(): Promise<boolean> {
    try {
      const respuesta = await fetch(`${this.apiUrl}/sala`, {
        method: 'HEAD',
        // 5 segundos: más que eso y para el usuario ya está caído
        signal: AbortSignal.timeout(5000)
      });

      // 401 significa que la API responde pero falta el token:
      // el servidor está vivo, que es lo que queremos saber.
      const disponible = respuesta.ok || respuesta.status === 401;
      this.apiDisponible$.next(disponible);

      return disponible;
    } catch {
      this.apiDisponible$.next(false);
      return false;
    }
  }

  get estaEnLinea(): boolean {
    return navigator.onLine;
  }

  // ── Avisos ─────────────────────────────────────────────────────
  // El toast de sin conexión no expira solo: se queda hasta que
  // vuelva la red, porque el problema sigue existiendo.
  private async mostrarSinConexion(): Promise<void> {
    if (this.toastSinConexion) return;

    this.toastSinConexion = await this.toastController.create({
      message:  'Sin conexión a internet. Revisá tu red.',
      color:    'danger',
      position: 'top',
      cssClass: 'toast-sin-conexion'
    });

    await this.toastSinConexion.present();
  }

  private async ocultarSinConexion(): Promise<void> {
    if (!this.toastSinConexion) return;

    await this.toastSinConexion.dismiss();
    this.toastSinConexion = null;
  }

  private async mostrarReconectado(): Promise<void> {
    const toast = await this.toastController.create({
      message:  'Conexión restablecida.',
      duration: 2500,
      color:    'success',
      position: 'top'
    });
    await toast.present();
  }

  /**
   * Aviso de mantenimiento. Se llama cuando la API devuelve 503.
   */
  async mostrarMantenimiento(mensaje?: string): Promise<void> {
    const toast = await this.toastController.create({
      message:  mensaje ?? 'El servidor está en mantenimiento. Intentá más tarde.',
      duration: 6000,
      color:    'warning',
      position: 'top'
    });
    await toast.present();
  }
}