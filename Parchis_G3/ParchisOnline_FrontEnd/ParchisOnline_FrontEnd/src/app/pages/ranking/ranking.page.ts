import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import {
  IonContent, IonIcon, IonSpinner,
  ToastController, ViewWillEnter
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  trophy, arrowBackOutline, logoBitcoin, medal,
  peopleOutline, informationCircleOutline
} from 'ionicons/icons';
import { RankingService } from '../../services/ranking.service';
import { AuthService } from '../../services/auth';

@Component({
  selector:    'app-ranking',
  templateUrl: './ranking.page.html',
  styleUrls:   ['./ranking.page.scss'],
  standalone:  true,
  imports: [CommonModule, IonContent, IonIcon, IonSpinner]
})
export class RankingPage implements OnInit, ViewWillEnter {

  // ── Variables ─────────────────────────────────────────────────
  usuario:        any     = null;
  top:            any[]   = [];   // los mejores jugadores
  podio:          any[]   = [];   // los primeros 3, separados
  resto:          any[]   = [];   // del 4 en adelante
  miPosicion:     any     = null;
  totalJugadores: number  = 0;
  cargando:       boolean = true;

  constructor(
    private rankingService:  RankingService,
    private authService:     AuthService,
    private router:          Router,
    private toastController: ToastController
  ) {
    addIcons({
      trophy, arrowBackOutline, logoBitcoin, medal,
      peopleOutline, informationCircleOutline
    });
  }

  ngOnInit(): void {
    this.usuario = this.authService.getUsuario();
    this.cargarRanking();
  }

  // Al volver a esta pantalla refrescamos — el ranking pudo
  // cambiar si el jugador ganó una partida mientras tanto
  ionViewWillEnter(): void {
    this.usuario = this.authService.getUsuario();
    this.cargarRanking();
  }

  // ================================================================
  // CARGAR RANKING
  // ================================================================
  private cargarRanking(): void {
    this.cargando = true;

    this.rankingService.obtenerRanking(50).subscribe({
      next: (respuesta: any) => {
        this.cargando = false;
        const datos = respuesta.ValorRetorno;

        if (!datos) return;

        this.top            = datos.Top ?? [];
        this.miPosicion     = datos.MiPosicion;
        this.totalJugadores = datos.TotalJugadores ?? 0;

        // Separamos el podio del resto para renderizarlos distinto.
        // Los 3 primeros van en un diseño especial arriba.
        this.podio = this.top.slice(0, 3);
        this.resto = this.top.slice(3);
      },
      error: async () => {
        this.cargando = false;
        await this.mostrarToast('No se pudo cargar el ranking.', 'danger');
      }
    });
  }

  // ================================================================
  // HELPERS VISUALES
  // ================================================================

  // ¿El usuario actual está dentro del top mostrado?
  // Si no está, mostramos su posición fija abajo para que
  // siempre pueda verse a sí mismo.
  estaEnElTop(): boolean {
    return this.top.some(j => j.EsElUsuarioActual);
  }

  // Medalla según la posición del podio
  getMedalla(posicion: number): string {
    const medallas: { [key: number]: string } = {
      1: '🥇',
      2: '🥈',
      3: '🥉'
    };
    return medallas[posicion] ?? '';
  }

  // Clase CSS del podio según la posición
  getPodioClass(posicion: number): string {
    const clases: { [key: number]: string } = {
      1: 'podio-oro',
      2: 'podio-plata',
      3: 'podio-bronce'
    };
    return clases[posicion] ?? '';
  }

  // Emoji del avatar según el número guardado en la BD.
  // El campo Usu_Avatar guarda un número del 1 al 20.
  getAvatar(numero: number): string {
    const avatares = [
      '🎮', '🎯', '🎲', '🏆', '⭐', '🔥', '💎', '👑',
      '🚀', '⚡', '🌟', '🎪', '🎨', '🎭', '🎸', '🏅',
      '💫', '🌈', '🦄', '🐉'
    ];
    const indice = ((numero ?? 1) - 1) % avatares.length;
    return avatares[indice] ?? '🎮';
  }

  formatearNumero(valor: number): string {
    return (valor ?? 0).toLocaleString('es-CR');
  }

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
  volver(): void {
    this.router.navigate(['/home']);
  }
}
