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
  InfoCelda, construirMapaCeldas, obtenerCelda, validarAnillo,
  POS_CASA, POS_CORONADA
} from './tablero.config';

@Component({
  selector:    'app-tablero',
  templateUrl: './tablero.page.html',
  styleUrls:   ['./tablero.page.scss'],
  standalone:  true,
  imports: [CommonModule, FormsModule, IonContent, IonIcon]
})
export class TableroPage implements OnInit, OnDestroy {

  // ── Identificadores de la partida ─────────────────────────────
  parId: number = 0;
  jpId:  number = 0;
  usuId: number = 0;

  // ── Estado del juego ──────────────────────────────────────────
  estado:    any     = null;
  cargando:  boolean = true;
  miColor:   string  = '';
  esMiTurno: boolean = false;

  // ── Dado ──────────────────────────────────────────────────────
  valorDado:      number  = 0;
  dadoTirado:     boolean = false;
  girandoDado:    boolean = false;
  esperandoMover: boolean = false;

  // El dado tiene que girar un mínimo de tiempo para que se vea.
  // El Hub responde en ~30 ms, así que sin esto la animación de
  // 450 ms se apagaba antes de completar media vuelta.
  private readonly DURACION_MIN_DADO = 750;
  private tsTiroDado: number = 0;
  private timerDado:  any    = null;

  fichasMovibles: number[] = [];

  // ── Chat ──────────────────────────────────────────────────────
  mostrarChat:      boolean = false;
  mensajes:         any[]   = [];
  mensajeTexto:     string  = '';
  mensajesNoLeidos: number  = 0;

  mensajesRapidos = [
    '¡Buena jugada!',
    '¡Eso te pasa!',
    '¡Nadie me para!',
    '¡Gg!'
  ];

  aviso: string | null = null;

  filas    = Array.from({ length: 15 }, (_, i) => i + 1);
  columnas = Array.from({ length: 15 }, (_, i) => i + 1);

  // ── Mapas precalculados ───────────────────────────────────────
  // El tablero es estático: sus 225 celdas se calculan una vez.
  // Las fichas cambian, pero solo cuando llega un estado nuevo,
  // no en cada ciclo de detección de cambios.
  private mapaCeldas: Map<string, InfoCelda> = new Map();
  private mapaFichas: Map<string, any[]>     = new Map();

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
    this.mapaCeldas = construirMapaCeldas();

    // Autochequeo de la geometría del anillo. Solo en desarrollo:
    // si algún día alguien toca las coordenadas, salta acá y no en
    // la defensa del proyecto.
    const errores = validarAnillo();
    if (errores.length > 0) {
      console.error('Geometría del tablero inválida:', errores);
    }

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
    if (this.timerDado) clearTimeout(this.timerDado);
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

  private registrarEventos(): void {

    this.subs.push(
      this.signalR.onEstadoActualizado$.subscribe((estado) => {
        this.aplicarEstado(estado);
      })
    );

    this.subs.push(
      this.signalR.onDadoTirado$.subscribe((resultado) => {
        this.procesarDado(resultado);
      })
    );

    this.subs.push(
      this.signalR.onFichaMovida$.subscribe((resultado) => {
        this.procesarMovimiento(resultado);
      })
    );

    this.subs.push(
      this.signalR.onPartidaFinalizada$.subscribe((resultado) => {
        this.mostrarVictoria(resultado);
      })
    );

    this.subs.push(
      this.signalR.onMensajeRecibido$.subscribe((mensaje) => {
        this.mensajes.push(mensaje);
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

    const yo = estado.Jugadores?.find((j: any) => j.JpId === this.jpId);
    this.miColor = yo?.Color ?? '';

    this.esMiTurno = estado.TurnoActualJpId === this.jpId;

    if (!this.esMiTurno) {
      this.dadoTirado     = false;
      this.fichasMovibles = [];
    }

    this.reconstruirMapaFichas();
  }

  // ── reconstruirMapaFichas ────────────────────────────────────
  // Agrupa las 16 fichas por celda una sola vez. El template pasa
  // de hacer 225 filtrados por ciclo a 225 lookups O(1).
  private reconstruirMapaFichas(): void {
    const mapa = new Map<string, any[]>();

    const fichas = this.estado?.Fichas ?? [];

    for (const ficha of fichas) {
      const celda = obtenerCelda(ficha.Posicion, ficha.Color, ficha.NumeroFicha);
      const clave = `${celda.fila}-${celda.columna}`;

      const lista = mapa.get(clave);
      if (lista) {
        lista.push(ficha);
      } else {
        mapa.set(clave, [ficha]);
      }
    }

    this.mapaFichas = mapa;
  }

  // ================================================================
  // PROCESAR EL DADO
  // ================================================================
  private procesarDado(resultado: any): void {
    if (!resultado) return;

    // Dejamos que el dado termine de girar antes de mostrar el
    // resultado. Si el servidor tardó más que la animación, no
    // esperamos nada extra.
    const transcurrido = Date.now() - this.tsTiroDado;
    const restante     = Math.max(0, this.DURACION_MIN_DADO - transcurrido);

    if (this.timerDado) clearTimeout(this.timerDado);

    this.timerDado = setTimeout(() => {
      this.girandoDado = false;
      this.valorDado   = resultado.ValorDado;

      if (resultado.Estado) {
        this.aplicarEstado(resultado.Estado);
      }

      if (resultado.SiguienteTurnoJpId === this.jpId && this.esMiTurno) {
        this.dadoTirado = true;
        this.calcularFichasMovibles();
      } else {
        this.dadoTirado     = false;
        this.fichasMovibles = [];
      }

      if (resultado.Mensaje) {
        this.mostrarAviso(resultado.Mensaje);
      }
    }, restante);
  }

  // ── calcularFichasMovibles ───────────────────────────────────
  // Solo para resaltar visualmente. El servidor revalida cuando el
  // jugador elige, así que un cliente que mienta no gana nada.
  private calcularFichasMovibles(): void {
    this.fichasMovibles = [];

    for (const ficha of this.getMisFichas()) {
      if (ficha.Posicion >= POS_CORONADA) continue;

      // En casa: solo sale con un 5
      if (ficha.Posicion === POS_CASA) {
        if (this.valorDado === 5) {
          this.fichasMovibles.push(ficha.NumeroFicha);
        }
        continue;
      }

      // En juego: hay que caer exacto en el centro, sin pasarse
      if (ficha.Posicion + this.valorDado <= POS_CORONADA) {
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

    if (resultado.HuboCaptura)   this.mostrarAviso('¡Captura!');
    if (resultado.FichaCoronada) this.mostrarAviso('¡Ficha coronada!');
    if (resultado.Mensaje)       this.mostrarAviso(resultado.Mensaje);
  }

  // ================================================================
  // ACCIONES DEL JUGADOR
  // ================================================================
  async tirarDado(): Promise<void> {
    if (!this.esMiTurno || this.dadoTirado || this.girandoDado) return;

    this.girandoDado = true;
    this.valorDado   = 0;
    this.tsTiroDado  = Date.now();

    await this.signalR.tirarDado(this.parId, this.jpId);

    // Red de seguridad: si el servidor no contesta, el dado no
    // queda girando para siempre.
    setTimeout(() => {
      if (this.girandoDado && Date.now() - this.tsTiroDado >= 3000) {
        this.girandoDado = false;
      }
    }, 3100);
  }

  async moverFicha(ficha: any): Promise<void> {
    if (!this.esMiTurno || !this.dadoTirado || this.esperandoMover) return;
    if (ficha.JpId !== this.jpId) return;
    if (!this.fichasMovibles.includes(ficha.NumeroFicha)) return;

    this.esperandoMover = true;

    await this.signalR.moverFicha(
      this.parId, this.jpId, ficha.NumeroFicha, this.valorDado
    );
  }

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
  // RENDERIZADO DEL TABLERO — todo O(1)
  // ================================================================
  getClaseCelda(fila: number, columna: number): string {
    return this.mapaCeldas.get(`${fila}-${columna}`)?.clases ?? 'celda-vacia';
  }

  tieneEstrella(fila: number, columna: number): boolean {
    return this.mapaCeldas.get(`${fila}-${columna}`)?.estrella ?? false;
  }

  getFichasEnCelda(fila: number, columna: number): any[] {
    return this.mapaFichas.get(`${fila}-${columna}`) ?? [];
  }

  // trackBy para que Angular no destruya y recree los divs de las
  // fichas en cada render. Sin esto la transición CSS nunca corre,
  // porque el elemento animado es siempre uno nuevo.
  trackFicha(_indice: number, ficha: any): string {
    return `${ficha.JpId}-${ficha.NumeroFicha}`;
  }

  trackIndice(indice: number): number {
    return indice;
  }

  getPuntos(): number[] {
    return Array.from({ length: this.valorDado }, (_, i) => i);
  }

  getColorJugadorActual(): string {
    const jugador = this.getJugadores().find(
      (j: any) => j.JpId === this.estado?.TurnoActualJpId
    );
    return jugador?.Color ?? 'AZUL';
  }

  getNombreJugadorActual(): string {
    const jugador = this.getJugadores().find(
      (j: any) => j.JpId === this.estado?.TurnoActualJpId
    );
    if (!jugador) return '...';
    return jugador.EsBot ? `${jugador.Nombre} (bot)` : jugador.Nombre;
  }

  esFichaMovible(ficha: any): boolean {
    return this.esMiTurno
        && this.dadoTirado
        && ficha.JpId === this.jpId
        && this.fichasMovibles.includes(ficha.NumeroFicha);
  }

  getMisFichas(): any[] {
    return this.estado?.Fichas?.filter((f: any) => f.JpId === this.jpId) ?? [];
  }

  getJugadores(): any[] {
    return this.estado?.Jugadores ?? [];
  }

  esTurnoDe(jpId: number): boolean {
    return this.estado?.TurnoActualJpId === jpId;
  }

  // ================================================================
  // HELPERS
  // ================================================================
  private marcarDesconectado(jpId: number, desconectado: boolean): void {
    const jugador = this.getJugadores().find((j: any) => j.JpId === jpId);
    if (jugador) {
      jugador.Desconectado = desconectado;
    }
  }

  private mostrarAviso(texto: string): void {
    this.aviso = texto;
    setTimeout(() => { this.aviso = null; }, 2500);
  }

  private async mostrarVictoria(resultado: any): Promise<void> {
    const gane = resultado.GanadorJpId === this.jpId;

    const ganador = this.getJugadores().find(
      (j: any) => j.JpId === resultado.GanadorJpId
    );

    const alert = await this.alertController.create({
      cssClass:        'alert-parchis',
      header:          gane ? '¡Ganaste!' : 'Partida terminada',
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