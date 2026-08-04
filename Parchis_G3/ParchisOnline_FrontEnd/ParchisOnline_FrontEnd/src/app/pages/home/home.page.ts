import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import {
  IonContent, IonIcon, IonSpinner, ToastController,
  AlertController, ViewWillEnter
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  logoBitcoin, lockClosed, chevronForward,
  home, storefrontOutline, personOutline
} from 'ionicons/icons';
import { SalaService } from '../../services/sala.service';
import { AuthService } from '../../services/auth';

@Component({
  selector:    'app-home',
  templateUrl: './home.page.html',
  styleUrls:   ['./home.page.scss'],
  standalone:  true,
  imports: [CommonModule, IonContent, IonIcon, IonSpinner]
})
export class HomePage implements OnInit, ViewWillEnter {

  // ── Variables ─────────────────────────────────────────────────
  usuario:  any   = null;   // Datos del usuario autenticado
  salas:    any[] = [];     // Las 5 salas traídas de la API
  cargando: boolean = true; // Spinner mientras carga

  constructor(
    private salaService:      SalaService,
    private authService:      AuthService,
    private router:           Router,
    private toastController:  ToastController,
    private alertController:  AlertController
  ) {
    // Registramos los íconos que usamos en el HTML
    addIcons({
      logoBitcoin, lockClosed, chevronForward,
      home, storefrontOutline, personOutline
    });
  }

  ngOnInit(): void {
    // Se ejecuta UNA sola vez, cuando Ionic crea la página por primera vez
    this.usuario = this.authService.getUsuario();
    this.cargarSalas();
  }

  // ── ionViewWillEnter ─────────────────────────────────────────
  ionViewWillEnter(): void {
    this.usuario = this.authService.getUsuario();
  }

  // ── cargarSalas ──────────────────────────────────────────────
  cargarSalas(): void {
    this.cargando = true;

    this.salaService.listarSalas().subscribe({
      next: (respuesta: any) => {
        this.cargando = false;
        this.salas = respuesta.ValorRetorno ?? [];
      },
      error: async (error: any) => {
        this.cargando = false;

        const toast = await this.toastController.create({
          message:  'No se pudieron cargar las salas. Verificá tu conexión.',
          duration: 3000,
          color:    'danger',
          position: 'top'
        });
        await toast.present();
      }
    });
  }

  // ── getSalaIcono ─────────────────────────────────────────────
  getSalaIcono(nombreSala: string): string {
    const iconos: { [key: string]: string } = {
      'Sala Bronce':   '🥉',
      'Sala Plata':    '🥈',
      'Sala Oro':      '🥇',
      'Sala Diamante': '💎',
      'Sala Élite':    '👑'
    };
    return iconos[nombreSala] ?? '🎲';
  }

  // ── getSalaClass ─────────────────────────────────────────────
  getSalaClass(nombreSala: string): string {
    const clases: { [key: string]: string } = {
      'Sala Bronce':   'sala-bronce',
      'Sala Plata':    'sala-plata',
      'Sala Oro':      'sala-oro',
      'Sala Diamante': 'sala-diamante',
      'Sala Élite':    'sala-elite'
    };
    return clases[nombreSala] ?? '';
  }

  // ── unirseASala ──────────────────────────────────────────────
  // Verifica el saldo y confirma antes de unirse a la sala
  async unirseASala(sala: any): Promise<void> {
    // Si no tiene monedas suficientes, no dejamos avanzar
    if (this.usuario.UsuMonedasTotal < sala.SalCostoEntrada) {
      const toast = await this.toastController.create({
        message:  `Necesitás ${sala.SalCostoEntrada} monedas para entrar a ${sala.SalNombre}.`,
        duration: 2500,
        color:    'warning',
        position: 'top'
      });
      await toast.present();
      return;
    }

    // Confirmación antes de descontar monedas (con estilo custom)
    const alert = await this.alertController.create({
  cssClass: 'alert-parchis',
  header:  sala.SalNombre,
  message: `¿Querés unirte por ${sala.SalCostoEntrada} monedas?\n\nPremio: ${sala.SalPremioBase} monedas.`,
  buttons: [
    { text: 'Cancelar', role: 'cancel', cssClass: 'btn-cancelar' },
    { text: 'Unirme', cssClass: 'btn-unirme', handler: () => this.confirmarUnion(sala) }
  ]
});
    await alert.present();
  }

  // ── confirmarUnion ───────────────────────────────────────────
  // Llama a la API para unirse a la sala seleccionada
  private confirmarUnion(sala: any): void {
    this.salaService.unirseASala(sala.SalId).subscribe({
      next: async (respuesta: any) => {
        const toast = await this.toastController.create({
          message:  respuesta.mensaje,
          duration: 2000,
          color:    'success',
          position: 'top'
        });
        await toast.present();

        // Actualizamos el saldo local con el nuevo valor
        this.usuario.UsuMonedasTotal = respuesta.monedas;
        localStorage.setItem('usuario', JSON.stringify(this.usuario));

        // Acá luego navegaremos a la sala de espera
        // this.router.navigate(['/sala-espera', sala.SalId]);
      },
      error: async (error: any) => {
        const toast = await this.toastController.create({
          message:  error.error?.mensaje ?? 'No se pudo unir a la sala.',
          duration: 2500,
          color:    'danger',
          position: 'top'
        });
        await toast.present();
      }
    });
  }

  // ── irATienda ────────────────────────────────────────────────
  irATienda(): void {
    this.router.navigate(['/tienda']);
  }

  // ── irAPerfil ────────────────────────────────────────────────
  irAPerfil(): void {
    this.router.navigate(['/perfil']);
  }
}