import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import {
  IonContent, IonIcon,
  ToastController, AlertController
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  exitOutline, chatbubbleOutline, closeOutline,
  wifiOutline, trophy, sendOutline
} from 'ionicons/icons';
import { Subscription } from 'rxjs';
import { SignalRService } from '../../services/signalr.service';
import { PartidaService } from '../../services/partida.service';
import { AuthService } from '../../services/auth';
import {
  ANILLO, CASAS, RECTA_FINAL,
  obtenerCelda, esCasillaSegura
} from './tablero.config';

@Component({
  selector:    'app-tablero',
  templateUrl: './tablero.page.html',
  styleUrls:   ['./tablero.page.scss'],
  standalone:  true,
  // FormsModule es necesario para el [(ngModel)] del input del chat.
  // IonSpinner e IonButton se quitaron porque el tablero no los usa:
  // el dado y los botones laterales son divs con estilos propios.
  imports: [CommonModule, FormsModule, IonContent, IonIcon]
})
export class TableroPage implements OnInit, OnDestroy {

  // ── Identificadores de la partida ─────────────────────────────
  parId: number = 0;
  jpId:  number = 0;
  usuId: number = 0;

  // ── Estado del juego ──────────────────────────────────────────
  estado:    any     = null;   // Foto completa del tablero
  cargando:  boolean = true;
  miColor:   string  = '';
  esMiTurno: boolean = false;

  // ── Dado ──────────────────────────────────────────────────────
  valorDado:      number  = 0;
  dadoTirado:     boolean = false;   // ¿ya tiró y falta mover?
  girandoDado:    boolean = false;
  esperandoMover: boolean = false;

  // Fichas que se pueden mover con el dado actual
  fichasMovibles: number[] = [];

  // ── Chat ──────────────────────────────────────────────────────
  mostrarChat:      boolean = false;
  mensajes:         any[]   = [];
  mensajeTexto:     string  = '';
  mensajesNoLeidos: number  = 0;

  // Los 4 mensajes rápidos de la HU-14
  mensajesRapidos = [
    '¡Buena jugada!',
    '¡Eso te pasa!',
    '¡Nadie me para!',
    '¡Gg!'
  ];

  // ── Notificaciones flotantes ─────────────────────────────────
  aviso: string | null = null;

  // Para renderizar el grid de 15×15 en el HTML
  filas    = Array.from({ length: 15 }, (_, i) => i + 1);
  columnas = Array.from({ length: 15 }, (_, i) => i + 1);

  private subs: Subscription[] = [];

  constructor(
    private signalR:         SignalRService,
    private partidaService:  PartidaService,
    private authService:     AuthService,
    private router:          Router,
    private route:           ActivatedRoute,
    private toastController: ToastController,
    private alertController: AlertController
  ) {
    addIcons({
      exitOutline, chatbubbleOutline, closeOutline,
      wifiOutline, trophy, sendOutline
    });
  }

  async ngOnInit(): Promise<void> {
    this.parId = Number(this.route.snapshot.queryParamMap.get('parId'));
    this.jpId  = Number(this.route.snapshot.queryParamMap.get('jpId'));

    const usuario = this.authService.getUsuario();
    this.usuId = usuario?.UsuId ?? 0;

    if (this.parId <= 0 || this.jpId <= 0) {
      await this.mostrarToast('Error al entrar a la partida.', 'danger');
      this.router.navigate(['/home'], { replaceUrl: true });
      return;
    }

    await this.conectar();
  }

  ngOnDestroy(): void {
    // Limpiamos las suscripciones para evitar memory leaks
    this.subs.forEach(s => s.unsubscribe());
  }

  // ================================================================
  // CONEXIÓN A SIGNALR
  // ================================================================
  private async conectar(): Promise<void> {
    const conectado = await this.signalR.conectar();

    if (!conectado) {
      await this.mostrarToast('No se pudo conectar al servidor.', 'danger');
      return;
    }

    this.registrarEventos();
    await this.signalR.unirseAPartida(this.parId, this.jpId);
  }

  // ── registrarEventos ─────────────────────────────────────────
  // Nos suscribimos a todos los eventos que manda el Hub
  private registrarEventos(): void {

    // ── Estado completo del tablero ──────────────────────────
    this.subs.push(
      this.signalR.onEstadoActualizado$.subscribe((estado) => {
        this.aplicarEstado(estado);
      })
    );

    // ── Alguien tiró el dado ─────────────────────────────────
    this.subs.push(
      this.signalR.onDadoTirado$.subscribe((resultado) => {
        this.procesarDado(resultado);
      })
    );

    // ── Alguien movió una ficha ──────────────────────────────
    this.subs.push(
      this.signalR.onFichaMovida$.subscribe((resultado) => {
        this.procesarMovimiento(resultado);
      })
    );

    // ── La partida terminó ───────────────────────────────────
    this.subs.push(
      this.signalR.onPartidaFinalizada$.subscribe((resultado) => {
        this.mostrarVictoria(resultado);
      })
    );

    // ── Chat ─────────────────────────────────────────────────
    this.subs.push(
      this.signalR.onMensajeRecibido$.subscribe((mensaje) => {
        this.mensajes.push(mensaje);

        // Si el chat está cerrado, contamos el mensaje como no leído
        if (!this.mostrarChat) {
          this.mensajesNoLeidos++;
        }
      })
    );

    this.subs.push(
      this.signalR.onHistorialChat$.subscribe((historial) => {
        this.mensajes = historial ?? [];
      })
    );

    // ── Desconexiones ────────────────────────────────────────
    this.subs.push(
      this.signalR.onJugadorDesconectado$.subscribe((datos) => {
        this.marcarDesconectado(datos.jpId, true);
        this.mostrarAviso('Un jugador perdió la conexión');
      })
    );

    this.subs.push(
      this.signalR.onJugadorReconectado$.subscribe((datos) => {
        this.marcarDesconectado(datos.jpId, false);
        this.mostrarAviso('Un jugador se reconectó');
      })
    );

    this.subs.push(
      this.signalR.onJugadorAbandono$.subscribe((datos) => {
        this.mostrarAviso(datos.mensaje ?? 'Un jugador abandonó');
      })
    );

    // ── Errores del servidor ─────────────────────────────────
    this.subs.push(
      this.signalR.onError$.subscribe(async (mensaje) => {
        this.esperandoMover = false;
        await this.mostrarToast(mensaje, 'warning');
      })
    );
  }

  // ================================================================
  // APLICAR EL ESTADO DEL TABLERO
  // ================================================================
  private aplicarEstado(estado: any): void {
    if (!estado) return;

    this.estado   = estado;
    this.cargando = false;

    // Buscamos nuestro color entre los jugadores
    const yo = estado.Jugadores?.find((j: any) => j.JpId === this.jpId);
    this.miColor = yo?.Color ?? '';

    // ¿Es nuestro turno?
    this.esMiTurno = estado.TurnoActualJpId === this.jpId;

    // Si no es nuestro turno, limpiamos el dado
    if (!this.esMiTurno) {
      this.dadoTirado     = false;
      this.fichasMovibles = [];
    }
  }

  // ================================================================
  // PROCESAR EL DADO
  // ================================================================
  private procesarDado(resultado: any): void {
    if (!resultado) return;

    this.girandoDado = false;
    this.valorDado   = resultado.ValorDado;

    // Actualizamos el estado del tablero
    if (resultado.Estado) {
      this.aplicarEstado(resultado.Estado);
    }

    // Si el turno sigue siendo nuestro, hay que mover una ficha
    if (resultado.SiguienteTurnoJpId === this.jpId && this.esMiTurno) {
      this.dadoTirado = true;
      this.calcularFichasMovibles();
    } else {
      this.dadoTirado     = false;
      this.fichasMovibles = [];
    }

    // Mensajes especiales del motor (tres 5's, sin movimientos, etc.)
    if (resultado.Mensaje) {
      this.mostrarAviso(resultado.Mensaje);
    }
  }

  // ── calcularFichasMovibles ───────────────────────────────────
  // Determina qué fichas se pueden mover con el dado actual.
  // Esto es solo para resaltar visualmente — el servidor valida
  // de nuevo cuando el jugador elige. Si el cliente mintiera,
  // el backend lo rechaza.
  private calcularFichasMovibles(): void {
    this.fichasMovibles = [];

    const misFichas = this.getMisFichas();

    for (const ficha of misFichas) {
      if (ficha.Estado === 'CORONADA') continue;

      // En casa: solo sale con 5
      if (ficha.Posicion === 0 && this.valorDado === 5) {
        this.fichasMovibles.push(ficha.NumeroFicha);
        continue;
      }

      // En el tablero: debe caer exacto sin pasarse de 68
      if (ficha.Posicion > 0 && ficha.Posicion + this.valorDado <= 68) {
        this.fichasMovibles.push(ficha.NumeroFicha);
      }
    }
  }

  // ================================================================
  // PROCESAR UN MOVIMIENTO
  // ================================================================
  private procesarMovimiento(resultado: any): void {
    if (!resultado) return;

    this.esperandoMover = false;
    this.dadoTirado     = false;
    this.fichasMovibles = [];

    if (resultado.Estado) {
      this.aplicarEstado(resultado.Estado);
    }

    // Avisos de eventos especiales
    if (resultado.HuboCaptura) {
      this.mostrarAviso('¡Captura! 🎯');
    }

    if (resultado.FichaCoronada) {
      this.mostrarAviso('¡Ficha coronada! 👑');
    }

    if (resultado.Mensaje) {
      this.mostrarAviso(resultado.Mensaje);
    }
  }

  // ================================================================
  // ACCIONES DEL JUGADOR
  // ================================================================

  // ── tirarDado ────────────────────────────────────────────────
  async tirarDado(): Promise<void> {
    if (!this.esMiTurno || this.dadoTirado || this.girandoDado) return;

    this.girandoDado = true;
    await this.signalR.tirarDado(this.parId, this.jpId);

    // Si el servidor no responde en 3 segundos, quitamos la animación
    // para que el botón no quede trabado
    setTimeout(() => { this.girandoDado = false; }, 3000);
  }

  // ── moverFicha ───────────────────────────────────────────────
  async moverFicha(ficha: any): Promise<void> {
    // Solo podemos mover nuestras fichas, en nuestro turno,
    // y si están marcadas como movibles
    if (!this.esMiTurno || !this.dadoTirado || this.esperandoMover) return;
    if (ficha.JpId !== this.jpId) return;
    if (!this.fichasMovibles.includes(ficha.NumeroFicha)) return;

    this.esperandoMover = true;

    await this.signalR.moverFicha(
      this.parId, this.jpId, ficha.NumeroFicha, this.valorDado
    );
  }

  // ── abandonarPartida ─────────────────────────────────────────
  async abandonarPartida(): Promise<void> {
    const alert = await this.alertController.create({
      cssClass: 'alert-parchis',
      header:   'Abandonar partida',
      message:  'Perderás el 20% de tu entrada como penalización.\n\nUn bot tomará tu lugar. ¿Confirmás?',
      buttons: [
        { text: 'Seguir jugando', role: 'cancel', cssClass: 'btn-cancelar' },
        {
          text: 'Abandonar',
          cssClass: 'btn-peligro',
          handler: async () => {
            await this.signalR.abandonarPartida(this.parId, this.usuId);
            this.router.navigate(['/home'], { replaceUrl: true });
          }
        }
      ]
    });
    await alert.present();
  }

  // ================================================================
  // CHAT
  // ================================================================
  toggleChat(): void {
    this.mostrarChat = !this.mostrarChat;

    if (this.mostrarChat) {
      this.mensajesNoLeidos = 0;
    }
  }

  async enviarMensajeRapido(texto: string): Promise<void> {
    await this.signalR.enviarMensaje(this.parId, this.jpId, texto, true);
  }

  async enviarMensaje(): Promise<void> {
    const texto = this.mensajeTexto.trim();
    if (!texto) return;

    await this.signalR.enviarMensaje(this.parId, this.jpId, texto, false);
    this.mensajeTexto = '';
  }

  // ================================================================
  // RENDERIZADO DEL TABLERO
  // ================================================================

  // ── getFichasEnCelda ─────────────────────────────────────────
  // Devuelve las fichas que están en una celda del grid.
  // Puede haber más de una (bloqueo o fichas apiladas en casa).
  getFichasEnCelda(fila: number, columna: number): any[] {
    if (!this.estado?.Fichas) return [];

    return this.estado.Fichas.filter((ficha: any) => {
      const celda = obtenerCelda(ficha.Posicion, ficha.Color, ficha.NumeroFicha);
      return celda.fila === fila && celda.columna === columna;
    });
  }

  // ── getTipoCelda ─────────────────────────────────────────────
  // Determina qué clase CSS lleva cada celda del grid: si es parte
  // del anillo, de una casa, de una recta final, o si está vacía.
  getTipoCelda(fila: number, columna: number): string {
    // ¿Es el centro? (la meta)
    if (fila >= 7 && fila <= 9 && columna >= 7 && columna <= 9) {
      return 'celda-meta';
    }

    // ¿Es una casilla del anillo?
    const indiceAnillo = ANILLO.findIndex(
      c => c.fila === fila && c.columna === columna
    );

    if (indiceAnillo >= 0) {
      // Las casillas de salida son seguras y llevan el color del dueño
      if (esCasillaSegura(indiceAnillo)) {
        const colores = ['rojo', 'azul', 'verde', 'amarillo'];
        const indice  = [0, 16, 32, 48].indexOf(indiceAnillo);
        return `celda-anillo celda-salida celda-salida-${colores[indice]}`;
      }
      return 'celda-anillo';
    }

    // ¿Es parte de una recta final?
    for (const color of Object.keys(RECTA_FINAL)) {
      const esRecta = RECTA_FINAL[color].some(
        c => c.fila === fila && c.columna === columna
      );
      if (esRecta) {
        return `celda-recta celda-recta-${color.toLowerCase()}`;
      }
    }

    // ¿Es parte de una casa?
    for (const color of Object.keys(CASAS)) {
      const esCasa = CASAS[color].some(
        c => c.fila === fila && c.columna === columna
      );
      if (esCasa) {
        return `celda-casa celda-casa-${color.toLowerCase()}`;
      }
    }

    return 'celda-vacia';
  }

  // ── getZonaCasa ──────────────────────────────────────────────
  // Las casas son bloques de 6×6 en las esquinas. Esto detecta
  // si una celda está dentro de esa zona para pintarle el fondo.
  getZonaCasa(fila: number, columna: number): string {
    if (fila >= 10 && fila <= 15 && columna >= 1  && columna <= 4)  return 'zona-rojo';
    if (fila >= 1  && fila <= 6  && columna >= 1  && columna <= 4)  return 'zona-azul';
    if (fila >= 1  && fila <= 6  && columna >= 11 && columna <= 15) return 'zona-verde';
    if (fila >= 10 && fila <= 15 && columna >= 11 && columna <= 15) return 'zona-amarillo';
    return '';
  }

  // ── esFichaMovible ───────────────────────────────────────────
  // Para resaltar visualmente las fichas que se pueden mover
  esFichaMovible(ficha: any): boolean {
    return this.esMiTurno
        && this.dadoTirado
        && ficha.JpId === this.jpId
        && this.fichasMovibles.includes(ficha.NumeroFicha);
  }

  // ── getMisFichas ─────────────────────────────────────────────
  getMisFichas(): any[] {
    return this.estado?.Fichas?.filter((f: any) => f.JpId === this.jpId) ?? [];
  }

  // ── getJugadores ─────────────────────────────────────────────
  getJugadores(): any[] {
    return this.estado?.Jugadores ?? [];
  }

  // ── esTurnoDe ────────────────────────────────────────────────
  esTurnoDe(jpId: number): boolean {
    return this.estado?.TurnoActualJpId === jpId;
  }

  // ── getNombreJugadorActual ───────────────────────────────────
  getNombreJugadorActual(): string {
    const jugador = this.getJugadores().find(
      (j: any) => j.JpId === this.estado?.TurnoActualJpId
    );
    return jugador?.Nombre ?? '...';
  }

  // ================================================================
  // HELPERS
  // ================================================================

  // Marca visualmente a un jugador como desconectado
  private marcarDesconectado(jpId: number, desconectado: boolean): void {
    const jugador = this.getJugadores().find((j: any) => j.JpId === jpId);
    if (jugador) {
      jugador.Desconectado = desconectado;
    }
  }

  // Aviso flotante que desaparece solo
  private mostrarAviso(texto: string): void {
    this.aviso = texto;
    setTimeout(() => { this.aviso = null; }, 2500);
  }

  // ── mostrarVictoria ──────────────────────────────────────────
  private async mostrarVictoria(resultado: any): Promise<void> {
    const gane = resultado.GanadorJpId === this.jpId;

    const ganador = this.getJugadores().find(
      (j: any) => j.JpId === resultado.GanadorJpId
    );

    const alert = await this.alertController.create({
      cssClass:        'alert-parchis',
      header:          gane ? '🏆 ¡Ganaste!' : 'Partida terminada',
      message:         gane
        ? '¡Felicitaciones! El premio ya está en tu cuenta.'
        : `Ganó ${ganador?.Nombre ?? 'otro jugador'}. ¡Mejor suerte la próxima!`,
      backdropDismiss: false,
      buttons: [
        {
          text: 'Volver al inicio',
          cssClass: 'btn-unirme',
          handler: () => {
            this.router.navigate(['/home'], { replaceUrl: true });
          }
        }
      ]
    });
    await alert.present();
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
}