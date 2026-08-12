import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import {
  IonContent, IonIcon, IonButton, IonSpinner,
  ToastController, AlertController, ViewWillEnter
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  logoBitcoin, arrowBackOutline, openOutline, checkmarkCircle,
  closeCircleOutline, shieldCheckmarkOutline, flame, timeOutline
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
export class TiendaMonedasPage implements OnInit, OnDestroy, ViewWillEnter {
 
  // Clave donde guardamos la orden en sessionStorage.
  // Usamos sessionStorage y no una variable normal porque el
  // componente se reinicia cuando el usuario vuelve de PayPal.
  private readonly CLAVE_ORDEN = 'orden_paypal_pendiente';
 
  // ── Variables ─────────────────────────────────────────────────
  usuario:    any     = null;
  paquetes:   any[]   = [];
  cargando:   boolean = true;
  procesando: number | null = null;
  capturando: boolean = false;
 
  // Marca si el usuario ya volvió de PayPal (para resaltar el botón)
  volvioDePayPal: boolean = false;
 
  ordenPendiente: {
    ordenId:   string;
    paqueteId: number;
    monedas:   number;
    nombre:    string;
  } | null = null;
 
  // Guardamos la referencia para poder quitarla en ngOnDestroy
  private manejadorVisibilidad = () => this.alVolverALaPestana();
 
  constructor(
    private pagoService:     PagoService,
    private authService:     AuthService,
    private router:          Router,
    private toastController: ToastController,
    private alertController: AlertController
  ) {
    addIcons({
      logoBitcoin, arrowBackOutline, openOutline, checkmarkCircle,
      closeCircleOutline, shieldCheckmarkOutline, flame, timeOutline
    });
  }
 
  ngOnInit(): void {
    this.usuario = this.authService.getUsuario();
    this.recuperarOrdenPendiente();
    this.cargarPaquetes();
 
    // Escuchamos cuando el usuario vuelve a esta pestaña.
    // Así detectamos automáticamente su regreso de PayPal.
    document.addEventListener('visibilitychange', this.manejadorVisibilidad);
  }
 
  ngOnDestroy(): void {
    // Limpiamos el listener para no dejar basura en memoria
    document.removeEventListener('visibilitychange', this.manejadorVisibilidad);
  }
 
  ionViewWillEnter(): void {
    this.usuario = this.authService.getUsuario();
    this.recuperarOrdenPendiente();
  }
 
  // DETECCIÓN DE REGRESO DE PAYPAL
  // Se dispara cuando la pestaña vuelve a estar visible, es decir
  // cuando el usuario cierra PayPal y regresa a la app.
  private alVolverALaPestana(): void {
    if (document.visibilityState !== 'visible') return;
 
    // Si hay una orden pendiente, marcamos que ya volvió para
    // resaltar visualmente el botón de confirmar
    if (this.ordenPendiente && !this.volvioDePayPal) {
      this.volvioDePayPal = true;
      this.mostrarToast('Si ya pagaste, confirmá abajo para recibir tus monedas.', 'warning');
    }
  }
  // PERSISTENCIA DE LA ORDEN
  // Guardamos la orden en sessionStorage para que sobreviva al
  // reinicio del componente cuando el usuario vuelve de PayPal.
 
  private guardarOrdenPendiente(): void {
    if (this.ordenPendiente) {
      sessionStorage.setItem(this.CLAVE_ORDEN, JSON.stringify(this.ordenPendiente));
    }
  }
 
  private recuperarOrdenPendiente(): void {
    const guardada = sessionStorage.getItem(this.CLAVE_ORDEN);
 
    if (guardada) {
      try {
        this.ordenPendiente = JSON.parse(guardada);
      } catch {
        this.limpiarOrdenPendiente();
      }
    }
  }
 
  private limpiarOrdenPendiente(): void {
    this.ordenPendiente = null;
    this.volvioDePayPal = false;
    sessionStorage.removeItem(this.CLAVE_ORDEN);
  }
  // CARGAR PAQUETES
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
 

  // PASO 1 — CREAR LA ORDEN Y ABRIR PAYPAL
 
  async comprarPaquete(paquete: any): Promise<void> {
    if (this.procesando !== null) return;
 
    const alert = await this.alertController.create({
      cssClass: 'alert-parchis',
      header:   paquete.Nombre,
      message:  `Vas a comprar ${this.formatearNumero(paquete.Monedas)} monedas por $${paquete.PrecioUSD} USD.\n\nSe abrirá PayPal en otra pestaña. Cuando termines de pagar, volvé acá y confirmá.`,
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
 
        // Guardamos la orden ANTES de abrir PayPal, así si el
        // componente se reinicia la información no se pierde
        this.ordenPendiente = {
          ordenId:   datos.OrdenId,
          paqueteId: paquete.PaqueteId,
          monedas:   paquete.Monedas,
          nombre:    paquete.Nombre
        };
        this.guardarOrdenPendiente();
        this.volvioDePayPal = false;
 
        // Abrimos PayPal en una pestaña nueva
        window.open(datos.UrlAprobacion, '_blank');
      },
      error: async (error: any) => {
        this.procesando = null;
        const mensaje = error.error?.strMensajeRespuesta
          ?? 'No se pudo conectar con PayPal.';
        await this.mostrarToast(this.limpiarMensaje(mensaje), 'danger');
      }
    });
  }
 
  // ── reabrirPayPal ────────────────────────────────────────────
  // Por si el usuario cerró la pestaña de PayPal sin querer
  reabrirPayPal(): void {
    if (!this.ordenPendiente) return;
 
    const url = `https://www.sandbox.paypal.com/checkoutnow?token=${this.ordenPendiente.ordenId}`;
    window.open(url, '_blank');
  }
 
  
  // PASO 2 — CAPTURAR EL PAGO

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
 
          this.limpiarOrdenPendiente();
          await this.mostrarExito(datos);
        } else {
          await this.mostrarErrorPago(
            this.limpiarMensaje(datos?.Mensaje ?? 'El pago no pudo completarse.')
          );
        }
      },
      error: async (error: any) => {
        this.capturando = false;
 
        const mensaje = error.error?.strMensajeRespuesta
          ?? error.error?.mensaje
          ?? 'No se pudo verificar el pago.';
 
        await this.mostrarErrorPago(this.limpiarMensaje(mensaje));
      }
    });
  }
 
  // ── cancelarOrden ────────────────────────────────────────────
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
          handler: () => this.limpiarOrdenPendiente()
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
 
  // ── mostrarErrorPago ─────────────────────────────────────────
  // Usamos un alert en vez de un toast porque los errores de pago
  // necesitan más espacio y atención del usuario. Un toast se
  // cierra solo en 3 segundos y puede pasar desapercibido justo
  // cuando más importa — el usuario está pendiente de si le
  // cobraron o no.
  //
  // Además el alert le ofrece la acción que realmente necesita:
  // volver a abrir PayPal para completar el pago.
  private async mostrarErrorPago(mensaje: string): Promise<void> {
    const alert = await this.alertController.create({
      cssClass: 'alert-parchis',
      header:   'Pago no confirmado',
      message:  mensaje,
      buttons: [
        {
          text: 'Reabrir PayPal',
          cssClass: 'btn-cancelar',
          handler: () => this.reabrirPayPal()
        },
        {
          text: 'Entendido',
          cssClass: 'btn-unirme'
        }
      ]
    });
    await alert.present();
  }
 
 
  // LIMPIAR MENSAJE
  // Blindaje contra JSON crudo. El backend ya traduce los errores
  // de PayPal a español, pero si por alguna razón llega un JSON
  // técnico (500+ caracteres con llaves), lo reemplazamos por un
  // mensaje genérico. Nunca debemos mostrarle JSON al usuario.
  private limpiarMensaje(mensaje: string): string {
    if (!mensaje) {
      return 'No se pudo verificar el pago. Intentá de nuevo.';
    }
 
    // Si parece JSON o es excesivamente largo, lo reemplazamos
    if (mensaje.length > 180 || mensaje.includes('{"') || mensaje.includes('debug_id')) {
      return 'Todavía no completaste el pago en PayPal. ' +
             'Abrí la ventana de PayPal, aprobá la compra y volvé a intentar.';
    }
 
    return mensaje;
  }
 
  // HELPERS
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