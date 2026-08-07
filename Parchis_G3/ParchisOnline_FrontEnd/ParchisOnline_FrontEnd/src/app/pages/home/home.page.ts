import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import {
  IonContent, IonIcon, IonSpinner, ToastController,
  AlertController, ViewWillEnter
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  logoBitcoin, lockClosed, chevronForward, trophy,
  home, storefrontOutline, personOutline
} from 'ionicons/icons';
import { SalaService } from '../../services/sala.service';
import { PartidaService } from '../../services/partida.service';
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
  usuario:  any     = null;   // Datos del usuario autenticado
  salas:    any[]   = [];     // Las 5 salas traídas de la API
  cargando: boolean = true;   // Spinner mientras carga
  buscandoPartida: boolean = false;  // Evita doble click en una sala

  constructor(
    private salaService:      SalaService,
    private partidaService:   PartidaService,
    private authService:      AuthService,
    private router:           Router,
    private toastController:  ToastController,
    private alertController:  AlertController
  ) {
    // Registramos los íconos que usamos en el HTML
    addIcons({
      logoBitcoin, lockClosed, chevronForward, trophy,
      home, storefrontOutline, personOutline
    });
  }

  ngOnInit(): void {
    // Se ejecuta UNA sola vez, cuando Ionic crea la página
    this.usuario = this.authService.getUsuario();
    this.cargarSalas();
  }

  // ── ionViewWillEnter ─────────────────────────────────────────
  // Se ejecuta CADA VEZ que volvemos a esta página (por ejemplo
  // al regresar del perfil o de cancelar una sala de espera),
  // así el saldo y el nombre siempre están actualizados.
  ionViewWillEnter(): void {
    this.usuario = this.authService.getUsuario();
    this.buscandoPartida = false;  // Reseteamos por si volvimos de una espera
  }

  // ── cargarSalas ──────────────────────────────────────────────
  cargarSalas(): void {
    this.cargando = true;

    this.salaService.listarSalas().subscribe({
      next: (respuesta: any) => {
        this.cargando = false;
        this.salas = respuesta.ValorRetorno ?? [];
      },
      error: async () => {
        this.cargando = false;
        await this.mostrarToast(
          'No se pudieron cargar las salas. Verificá tu conexión.',
          'danger'
        );
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
  // Valida el saldo localmente y pide confirmación antes de
  // llamar al matchmaking (que sí cobra las monedas de verdad).
  async unirseASala(sala: any): Promise<void> {
    // Si ya está buscando partida, ignoramos clicks extra
    if (this.buscandoPartida) return;

    // Validación local rápida — el backend igual la revalida
    if (this.usuario.UsuMonedasTotal < sala.SalCostoEntrada) {
      await this.mostrarToast(
        `Necesitás ${sala.SalCostoEntrada} monedas para entrar a ${sala.SalNombre}.`,
        'warning'
      );
      return;
    }

    const alert = await this.alertController.create({
      cssClass: 'alert-parchis',
      header:   sala.SalNombre,
      message:  `¿Querés unirte por ${sala.SalCostoEntrada} monedas?\n\nPremio: ${sala.SalPremioBase} monedas.`,
      buttons: [
        { text: 'Cancelar', role: 'cancel', cssClass: 'btn-cancelar' },
        {
          text: 'Unirme',
          cssClass: 'btn-unirme',
          handler: () => this.buscarPartida(sala)
        }
      ]
    });
    await alert.present();
  }

  // ── buscarPartida ────────────────────────────────────────────
  // Llama al matchmaking. El backend:
  //   1. Busca una partida ESPERANDO con cupo, o crea una nueva
  //   2. Le asigna el siguiente color libre
  //   3. Le cobra la entrada
  //   4. Devuelve ParId y JpId para conectarse al Hub
  private buscarPartida(sala: any): void {
    this.buscandoPartida = true;

    this.partidaService.buscarPartida(sala.SalId).subscribe({
      next: async (respuesta: any) => {
        const datos = respuesta.ValorRetorno;

        if (!datos) {
          this.buscandoPartida = false;
          await this.mostrarToast('No se pudo crear la partida.', 'danger');
          return;
        }

        // Actualizamos el saldo con el valor real que devolvió el backend
        this.usuario.UsuMonedasTotal = datos.MonedasRestantes;
        localStorage.setItem('usuario', JSON.stringify(this.usuario));

        await this.mostrarToast(
          `Te uniste a ${sala.SalNombre} como ${datos.ColorAsignado} 🎲`,
          'success'
        );

        // Navegamos a la sala de espera con los IDs que necesita
        // para conectarse al Hub de SignalR
        this.router.navigate(['/sala-espera'], {
          queryParams: {
            parId: datos.ParId,
            jpId:  datos.JpId
          }
        });
      },
      error: async (error: any) => {
        this.buscandoPartida = false;

        const mensaje = error.error?.strMensajeRespuesta
          ?? error.error?.mensaje
          ?? 'No se pudo unir a la sala.';

        await this.mostrarToast(mensaje, 'danger');
      }
    });
  }

  // ── mostrarToast ─────────────────────────────────────────────
  private async mostrarToast(mensaje: string, color: string): Promise<void> {
    const toast = await this.toastController.create({
      message:  mensaje,
      duration: 2500,
      color,
      position: 'top'
    });
    await toast.present();
  }

  // ── Navegación ───────────────────────────────────────────────
  irATienda(): void {
    this.router.navigate(['/tienda']);
  }

  irAPerfil(): void {
    this.router.navigate(['/perfil']);
  }
}
