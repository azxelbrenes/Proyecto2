import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import {
  IonContent, IonIcon, IonButton, IonSpinner,
  ToastController, AlertController, ViewWillEnter
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  logoBitcoin, arrowBackOutline, openOutline, checkmarkCircle,
  closeCircleOutline, shieldCheckmarkOutline, flame
} from 'ionicons/icons';
import { PagoService } from '../../services/pago.service';
import { AuthService } from '../../services/auth';

@Component({
  selector:    'app-tienda-monedas',
  templateUrl: './tienda-monedas.page.html',
  styleUrls:   ['./tienda-monedas.page.scss'],
  standalone:  true,
  imports: [CommonModule, IonContent, IonIcon, IonButton, IonSpinner]
})
export class TiendaMonedasPage implements OnInit, ViewWillEnter {

  // ── Variables ─────────────────────────────────────────────────
  usuario:   any     = null;
  paquetes:  any[]   = [];
  cargando:  boolean = true;

  // ID del paquete que está procesándose (para el spinner)
  procesando: number | null = null;

  // ── Estado de la orden en curso ──────────────────────────────
  // Cuando el usuario abre PayPal, guardamos estos datos para
  // poder capturar el pago cuando vuelva
  ordenPendiente: {
    ordenId:   string;
    paqueteId: number;
    monedas:   number;
    nombre:    string;
  } | null = null;

  capturando: boolean = false;

  constructor(
    private pagoService:     PagoService,
    private authService:     AuthService,
    private router:          Router,
    private toastController: ToastController,
    private alertController: AlertController
  ) {
    addIcons({
      logoBitcoin, arrowBackOutline, openOutline, checkmarkCircle,
      closeCircleOutline, shieldCheckmarkOutline, flame
    });
  }

  ngOnInit(): void {
    this.usuario = this.authService.getUsuario();
    this.cargarPaquetes();
  }

  ionViewWillEnter(): void {
    this.usuario = this.authService.getUsuario();
  }

  // ================================================================
  // CARGAR PAQUETES
  // ================================================================
  private cargarPaquetes(): void {
    this.cargando = true;

    this.pagoService.obtenerPaquetes().subscribe({
      next: (respuesta: any) => {
        this.cargando = false;
        this.paquetes = respuesta.ValorRetorno ?? [];
      },
      error: async () => {
        this.cargando = false;
        await this.mostrarToast('No se pudieron cargar los paquetes.', 'danger');
      }
    });
  }

  // ================================================================
  // PASO 1 — CREAR LA ORDEN Y ABRIR PAYPAL
  // ================================================================
  async comprarPaquete(paquete: any): Promise<void> {
    if (this.procesando !== null) return;

    // Confirmación antes de mandarlo a PayPal
    const alert = await this.alertController.create({
      cssClass: 'alert-parchis',
      header:   paquete.Nombre,
      message:  `Vas a comprar ${this.formatearNumero(paquete.Monedas)} monedas por $${paquete.PrecioUSD} USD.\n\nSe abrirá PayPal para completar el pago.`,
      buttons: [
        { text: 'Cancelar', role: 'cancel', cssClass: 'btn-cancelar' },
        {
          text: 'Continuar',
          cssClass: 'btn-unirme',
          handler: () => this.crearOrden(paquete)
        }
      ]
    });
    await alert.present();
  }

  private crearOrden(paquete: any): void {
    this.procesando = paquete.PaqueteId;

    this.pagoService.crearOrden(paquete.PaqueteId).subscribe({
      next: async (respuesta: any) => {
        this.procesando = null;
        const datos = respuesta.ValorRetorno;

        if (!datos?.UrlAprobacion) {
          await this.mostrarToast('No se pudo generar la orden de pago.', 'danger');
          return;
        }

        // Guardamos la orden para poder capturarla después
        this.ordenPendiente = {
          ordenId:   datos.OrdenId,
          paqueteId: paquete.PaqueteId,
          monedas:   paquete.Monedas,
          nombre:    paquete.Nombre
        };

        // Abrimos PayPal en una ventana nueva.
        // El usuario paga allá y vuelve a esta pantalla.
        window.open(datos.UrlAprobacion, '_blank');
      },
      error: async (error: any) => {
        this.procesando = null;
        const mensaje = error.error?.strMensajeRespuesta
          ?? 'No se pudo conectar con PayPal.';
        await this.mostrarToast(mensaje, 'danger');
      }
    });
  }

  // ================================================================
  // PASO 2 — CAPTURAR EL PAGO
  // ================================================================
  // El backend le pregunta a PayPal si la orden está COMPLETED.
  // Solo si PayPal lo confirma se acreditan las monedas.
  confirmarPago(): void {
    if (!this.ordenPendiente || this.capturando) return;

    this.capturando = true;

    this.pagoService.capturarPago(
      this.ordenPendiente.ordenId,
      this.ordenPendiente.paqueteId
    ).subscribe({
      next: async (respuesta: any) => {
        this.capturando = false;
        const datos = respuesta.ValorRetorno;

        if (datos?.Exitoso) {
          // Actualizamos el saldo con el valor real del backend
          this.usuario.UsuMonedasTotal = datos.SaldoNuevo;
          localStorage.setItem('usuario', JSON.stringify(this.usuario));

          this.ordenPendiente = null;

          await this.mostrarExito(datos);
        } else {
          await this.mostrarToast(
            datos?.Mensaje ?? 'El pago no pudo completarse.',
            'warning'
          );
        }
      },
      error: async (error: any) => {
        this.capturando = false;

        const mensaje = error.error?.strMensajeRespuesta
          ?? 'El pago no se pudo verificar. Si ya pagaste, esperá un momento e intentá de nuevo.';

        await this.mostrarToast(mensaje, 'danger');
      }
    });
  }

  // ── cancelarOrden ────────────────────────────────────────────
  // El usuario decide no completar el pago
  async cancelarOrden(): Promise<void> {
    const alert = await this.alertController.create({
      cssClass: 'alert-parchis',
      header:   'Cancelar compra',
      message:  '¿Seguro que querés cancelar? No se te cobrará nada.',
      buttons: [
        { text: 'Seguir con el pago', role: 'cancel', cssClass: 'btn-cancelar' },
        {
          text: 'Cancelar',
          cssClass: 'btn-peligro',
          handler: () => { this.ordenPendiente = null; }
        }
      ]
    });
    await alert.present();
  }

  // ── mostrarExito ─────────────────────────────────────────────
  private async mostrarExito(datos: any): Promise<void> {
    const alert = await this.alertController.create({
      cssClass: 'alert-parchis',
      header:   '¡Compra exitosa! 🎉',
      message:  `Recibiste ${this.formatearNumero(datos.MonedasAcreditadas)} monedas.\n\nNuevo saldo: ${this.formatearNumero(datos.SaldoNuevo)}`,
      buttons: [{ text: '¡Genial!', cssClass: 'btn-unirme' }]
    });
    await alert.present();
  }

  // ================================================================
  // HELPERS
  // ================================================================

  // Clase de estilo según el paquete (más caro = más llamativo)
  getPaqueteClass(paquete: any): string {
    const clases: { [key: number]: string } = {
      1: 'paquete-pequeno',
      2: 'paquete-mediano',
      3: 'paquete-grande',
      4: 'paquete-premium'
    };
    return clases[paquete.PaqueteId] ?? 'paquete-pequeno';
  }

  formatearNumero(valor: number): string {
    return valor?.toLocaleString('es-CR') ?? '0';
  }

  private async mostrarToast(mensaje: string, color: string): Promise<void> {
    const toast = await this.toastController.create({
      message:  mensaje,
      duration: 3000,
      color,
      position: 'top'
    });
    await toast.present();
  }

  volver(): void {
    this.router.navigate(['/tienda']);
  }
}
