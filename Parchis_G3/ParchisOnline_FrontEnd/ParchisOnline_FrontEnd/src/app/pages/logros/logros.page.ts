import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import {
  IonContent, IonIcon, IonButton, IonSpinner, ToastController
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  arrowBack, medalOutline, logoBitcoin, lockClosed, checkmarkCircle
} from 'ionicons/icons';
import { LogroService } from '../../services/logro.services';
import { AuthService } from '../../services/auth';

@Component({
  selector:    'app-logros',
  templateUrl: './logros.page.html',
  styleUrls:   ['./logros.page.scss'],
  standalone:  true,
  imports: [CommonModule, IonContent, IonIcon, IonButton, IonSpinner]
})
export class LogrosPage implements OnInit {

  resumen:   any     = null;
  cargando:  boolean = true;
  reclamando: boolean = false;

  constructor(
    private logroService:    LogroService,
    private authService:     AuthService,
    private router:          Router,
    private toastController: ToastController
  ) {
    addIcons({
      arrowBack, medalOutline, logoBitcoin, lockClosed, checkmarkCircle
    });
  }

  ngOnInit(): void {
    this.cargarLogros();
  }

  private cargarLogros(): void {
    this.cargando = true;

    this.logroService.obtenerLogros().subscribe({
      next: (respuesta: any) => {
        this.cargando = false;
        this.resumen  = respuesta.ValorRetorno;
      },
      error: async () => {
        this.cargando = false;
        await this.mostrarToast('No se pudieron cargar los logros.', 'danger');
      }
    });
  }

  get hayPendientes(): boolean {
    return (this.resumen?.RecompensaPendiente ?? 0) > 0;
  }

  reclamar(): void {
    if (this.reclamando || !this.hayPendientes) return;

    this.reclamando = true;

    this.logroService.reclamar().subscribe({
      next: async (respuesta: any) => {
        this.reclamando = false;
        const datos = respuesta.ValorRetorno;

        // El saldo local se actualiza con el que devuelve el servidor,
        // que es el único correcto.
        const usuario = this.authService.getUsuario();
        if (usuario && datos?.SaldoNuevo != null) {
          usuario.UsuMonedasTotal = datos.SaldoNuevo;
          localStorage.setItem('usuario', JSON.stringify(usuario));
        }

        await this.mostrarToast(datos?.Mensaje ?? '¡Logros reclamados!', 'success');
        this.cargarLogros();
      },
      error: async (error: any) => {
        this.reclamando = false;
        const mensaje = error?.error?.strMensajeRespuesta ?? 'No se pudo reclamar.';
        await this.mostrarToast(mensaje, 'warning');
      }
    });
  }

  volver(): void {
    this.router.navigate(['/perfil']);
  }

  private async mostrarToast(mensaje: string, color: string): Promise<void> {
    const toast = await this.toastController.create({
      message:  mensaje,
      duration: 2800,
      color,
      position: 'top'
    });
    await toast.present();
  }
}