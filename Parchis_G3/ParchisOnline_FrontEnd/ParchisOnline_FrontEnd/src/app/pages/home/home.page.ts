import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import {
  IonContent, IonIcon, IonSpinner, IonButton, ToastController,
  AlertController, ViewWillEnter
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  logoBitcoin, lockClosed, chevronForward, trophy,
  home, storefrontOutline, personOutline, giftOutline,
  closeOutline, flame
} from 'ionicons/icons';
import { SalaService } from '../../services/sala.service';
import { PartidaService } from '../../services/partida.service';
import { RecompensaService } from '../../services/recompensa.service';
import { AuthService } from '../../services/auth';

@Component({
  selector:    'app-home',
  templateUrl: './home.page.html',
  styleUrls:   ['./home.page.scss'],
  standalone:  true,
  imports: [CommonModule, IonContent, IonIcon, IonSpinner, IonButton]
})
export class HomePage implements OnInit, ViewWillEnter {

  // ── Variables de las salas ────────────────────────────────────
  usuario:         any     = null;
  salas:           any[]   = [];
  cargando:        boolean = true;
  buscandoPartida: boolean = false;

  // ── Variables de la recompensa diaria ─────────────────────────
  estadoRecompensa: any     = null;
  mostrarModalRecompensa: boolean = false;
  reclamando:       boolean = false;

  // Los 5 días de la racha, para renderizar la barra de progreso
  diasRacha = [
    { dia: 1, monedas: 200  },
    { dia: 2, monedas: 400  },
    { dia: 3, monedas: 600  },
    { dia: 4, monedas: 800  },
    { dia: 5, monedas: 1000 }
  ];

  constructor(
    private salaService:       SalaService,
    private partidaService:    PartidaService,
    private recompensaService: RecompensaService,
    private authService:       AuthService,
    private router:            Router,
    private toastController:   ToastController,
    private alertController:   AlertController
  ) {
    addIcons({
      logoBitcoin, lockClosed, chevronForward, trophy,
      home, storefrontOutline, personOutline, giftOutline,
      closeOutline, flame
    });
  }

  ngOnInit(): void {
    this.usuario = this.authService.getUsuario();
    this.cargarSalas();
    this.verificarRecompensa();
  }

  // Se ejecuta cada vez que volvemos a esta página (del perfil,
  // de la tienda, de cancelar una sala) para refrescar el saldo
  ionViewWillEnter(): void {
    this.usuario = this.authService.getUsuario();
    this.buscandoPartida = false;
  }

  // ================================================================
  // RECOMPENSA DIARIA
  // ================================================================

  // ── verificarRecompensa ──────────────────────────────────────
  // Al abrir el home preguntamos al backend si hay recompensa
  // disponible. Si la hay, mostramos el modal automáticamente.
  private verificarRecompensa(): void {
    this.recompensaService.obtenerEstado().subscribe({
      next: (respuesta: any) => {
        this.estadoRecompensa = respuesta.ValorRetorno;

        // Solo mostramos el modal si realmente puede reclamar.
        // Si ya reclamó hoy, no lo molestamos.
        if (this.estadoRecompensa?.PuedeReclamar) {
          // Pequeño delay para que el home cargue primero y el
          // modal aparezca sobre una pantalla ya renderizada
          setTimeout(() => {
            this.mostrarModalRecompensa = true;
          }, 600);
        }
      },
      error: () => {
        // Si falla, no bloqueamos nada — el jugador simplemente
        // no ve el modal y puede reclamarla la próxima vez
        this.estadoRecompensa = null;
      }
    });
  }

  // ── reclamarRecompensa ───────────────────────────────────────
  reclamarRecompensa(): void {
    if (this.reclamando) return;

    this.reclamando = true;

    this.recompensaService.reclamar().subscribe({
      next: async (respuesta: any) => {
        this.reclamando = false;
        const datos = respuesta.ValorRetorno;

        if (!datos?.Exitoso) {
          await this.mostrarToast(
            datos?.Mensaje ?? 'No se pudo reclamar la recompensa.',
            'warning'
          );
          this.cerrarModalRecompensa();
          return;
        }

        // Actualizamos el saldo con el valor real del backend
        this.usuario.UsuMonedasTotal = datos.SaldoNuevo;
        localStorage.setItem('usuario', JSON.stringify(this.usuario));

        // Actualizamos el estado local de la racha para que el
        // modal muestre el día nuevo antes de cerrarse
        if (this.estadoRecompensa) {
          this.estadoRecompensa.RachaActual   = datos.RachaNueva;
          this.estadoRecompensa.PuedeReclamar = false;
        }

        this.cerrarModalRecompensa();

        await this.mostrarToast(
          `¡Ganaste ${datos.MonedasOtorgadas} monedas! 🎁`,
          'success'
        );
      },
      error: async (error: any) => {
        this.reclamando = false;
        const mensaje = error.error?.strMensajeRespuesta
          ?? 'No se pudo reclamar la recompensa.';
        await this.mostrarToast(mensaje, 'danger');
        this.cerrarModalRecompensa();
      }
    });
  }

  cerrarModalRecompensa(): void {
    this.mostrarModalRecompensa = false;
  }

  // ── esDiaCompletado ──────────────────────────────────────────
  // Marca los días ya alcanzados de la racha con un check
  esDiaCompletado(dia: number): boolean {
    return dia <= (this.estadoRecompensa?.RachaActual ?? 0);
  }

  // ── esDiaActual ──────────────────────────────────────────────
  // El día que está por reclamar ahora mismo. Se resalta en dorado.
  esDiaActual(dia: number): boolean {
    const racha = this.estadoRecompensa?.RachaActual ?? 0;

    // Si puede reclamar, el día actual es el siguiente de su racha
    // (con tope en 5, porque la racha no sube más allá de eso)
    if (this.estadoRecompensa?.PuedeReclamar) {
      return dia === Math.min(racha + 1, 5);
    }

    return dia === racha;
  }

  // ================================================================
  // SALAS
  // ================================================================
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
  async unirseASala(sala: any): Promise<void> {
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
  // Llama al matchmaking. El backend busca o crea una partida,
  // asigna color, cobra la entrada y devuelve ParId + JpId.
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

        // Actualizamos el saldo tras pagar la entrada
        this.usuario.UsuMonedasTotal = datos.MonedasRestantes;
        localStorage.setItem('usuario', JSON.stringify(this.usuario));

        await this.mostrarToast(
          `Te uniste a ${sala.SalNombre} como ${datos.ColorAsignado} 🎲`,
          'success'
        );

        this.router.navigate(['/sala-espera'], {
          queryParams: { parId: datos.ParId, jpId: datos.JpId }
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

  // ================================================================
  // HELPERS
  // ================================================================
  private async mostrarToast(mensaje: string, color: string): Promise<void> {
    const toast = await this.toastController.create({
      message:  mensaje,
      duration: 2500,
      color,
      position: 'top'
    });
    await toast.present();
  }

  irATienda(): void {
    this.router.navigate(['/tienda']);
  }

  irAPerfil(): void {
    this.router.navigate(['/perfil']);
  }
}
