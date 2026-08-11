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

  // El Hub responde en ~30 ms; sin este mínimo la animación de giro
  // se apaga antes de completar media vuelta.
  private readonly DURACION_MIN_DADO = 750;
  private tsTiroDado: number = 0;
  private timerDado:  any    = null;

  fichasMovibles: number[] = [];

  // ── Temporizador de turno (RF-03) ─────────────────────────────
  segundosTurno:  number  = 0;
  turnoCronometrado: boolean = false;
  private readonly LIMITE_TURNO = 30;
  private timerCuentaRegresiva: any = null;

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

  // ── Mensajes flotantes sobre las fichas (RF-09) ───────────────
  // jpId -> texto del mensaje. Se limpia solo a los 3 segundos.
  mensajesFlotantes: Map<number, string> = new Map();
  private timersFlotantes: Map<number, any> = new Map();

  filas    = Array.from({ length: 15 }, (_, i) => i + 1);
  columnas = Array.from({ length: 15 }, (_, i) => i + 1);

  // El tablero es estático: sus 225 celdas se calculan una vez.
  // Las fichas se reagrupan solo al llegar un estado nuevo.
  private mapaCeldas: Map<string, InfoCelda> = new Map();
  private mapaFichas: Map<string, any[]>     = new Map();

  private abandonoEnCurso: boolean = false;
  private timerAbandono:   any     = null;

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
    if (this.timerDado)           clearTimeout(this.timerDado);
    if (this.timerAbandono)       clearTimeout(this.timerAbandono);
    if (this.timerCuentaRegresiva) clearInterval(this.timerCuentaRegresiva);

    this.timersFlotantes.forEach(t => clearTimeout(t));
    this.timersFlotantes.clear();

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
        this.detenerCuentaRegresiva();
        this.mostrarVictoria(resultado);
      })
    );

    // ── RF-03: arranca el reloj del turno ────────────────────
    // El Hub manda esto cada vez que cambia el turno, con los
    // segundos que realmente quedan. Quien se reconecta a mitad
    // de turno recibe el remanente, no 30 de nuevo.
    this.subs.push(
      this.signalR.onTurnoIniciado$.subscribe((datos) => {
        this.iniciarCuentaRegresiva(datos?.Segundos ?? this.LIMITE_TURNO);
      })
    );

    this.subs.push(
      this.signalR.onTurnoAutomatico$.subscribe((datos) => {
        this.detenerCuentaRegresiva();
        this.mostrarAviso(datos?.Mensaje ?? 'Se acabó el tiempo del turno');
      })
    );

    this.subs.push(
      this.signalR.onMensajeRecibido$.subscribe((mensaje) => {
        this.mensajes.push(mensaje);

        // RF-09: el mensaje aparece 3 segundos sobre la ficha de quien
        // lo mandó, además de quedar en el historial del chat.
        this.mostrarMensajeFlotante(mensaje);

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
      this.signalR.onAbandonoConfirmado$.subscribe(async (datos) => {
        this.abandonoEnCurso = false;
        if (this.timerAbandono) clearTimeout(this.timerAbandono);
        this.detenerCuentaRegresiva();

        const penalizacion = datos?.MonedasPenalizacion ?? datos?.Penalizacion;
        const mensaje = penalizacion
          ? `Abandonaste la partida. Penalización: ${penalizacion} monedas.`
          : 'Abandonaste la partida.';

        await this.mostrarToast(mensaje, 'medium');
        this.router.navigate(['/home'], { replaceUrl: true });
      })
    );

    this.subs.push(
      this.signalR.onJugadoresReemplazados$.subscribe((datos) => {
        this.mostrarAviso(datos?.mensaje ?? 'Un jugador fue reemplazado por un bot');
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
  // TEMPORIZADOR DE TURNO (RF-03)
  // ================================================================
  // El reloj real corre en el servidor: esto es solo el reflejo
  // visual. Si llega a cero y el servidor todavía no resolvió, el
  // contador se queda en 0 y no hace nada por su cuenta.
  private iniciarCuentaRegresiva(segundos: number): void {
    this.detenerCuentaRegresiva();

    this.segundosTurno = Math.max(0, Math.min(segundos, this.LIMITE_TURNO));
    this.turnoCronometrado = this.segundosTurno > 0;

    if (!this.turnoCronometrado) return;

    this.timerCuentaRegresiva = setInterval(() => {
      this.segundosTurno--;

      if (this.segundosTurno <= 0) {
        this.segundosTurno = 0;
        this.detenerCuentaRegresiva();
      }
    }, 1000);
  }

  private detenerCuentaRegresiva(): void {
    if (this.timerCuentaRegresiva) {
      clearInterval(this.timerCuentaRegresiva);
      this.timerCuentaRegresiva = null;
    }
    this.turnoCronometrado = false;
  }

  // Porcentaje para la barra de tiempo del HTML
  get porcentajeTiempo(): number {
    return (this.segundosTurno / this.LIMITE_TURNO) * 100;
  }

  get tiempoCritico(): boolean {
    return this.turnoCronometrado && this.segundosTurno <= 10;
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

  // Agrupa las 16 fichas por celda una sola vez. El template pasa
  // de 225 filtrados por ciclo a 225 lookups O(1).
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
        // El orden importa: primero calculamos las jugadas y recién
        // después decidimos si pedir que elija ficha.
        //
        // Antes dadoTirado se ponía en true ANTES de este cálculo, así
        // que al sacar un 4 con todas las fichas en casa el banner
        // pedía "Elegí una ficha" sin que hubiera ninguna clicable.
        this.calcularFichasMovibles();

        if (this.fichasMovibles.length > 0) {
          this.dadoTirado = true;
        } else {
          this.dadoTirado = false;
          this.mostrarAviso(`Sacaste ${this.valorDado}: sin movimientos posibles`);
        }
      } else {
        this.dadoTirado     = false;
        this.fichasMovibles = [];
      }

      if (resultado.Mensaje) {
        this.mostrarAviso(resultado.Mensaje);
      }
    }, restante);
  }

  // Solo para resaltar visualmente. El servidor revalida cuando el
  // jugador elige, así que un cliente que mienta no gana nada.
  private calcularFichasMovibles(): void {
    this.fichasMovibles = [];

    for (const ficha of this.getMisFichas()) {
      if (ficha.Posicion >= POS_CORONADA) continue;

      // De casa solo se sale con un 5
      if (ficha.Posicion === POS_CASA) {
        if (this.valorDado === 5) {
          this.fichasMovibles.push(ficha.NumeroFicha);
        }
        continue;
      }

      // Hay que caer exacto en el centro
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

    // Si el servidor no contesta, el dado no queda girando para siempre
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

  // El Hub identifica a quien abandona por usuId. Si el token no lo
  // trajo, lo sacamos del estado de la partida.
  private resolverUsuId(): number {
    if (this.usuId > 0) return this.usuId;

    const yo = this.getJugadores().find((j: any) => j.JpId === this.jpId);
    const desdeEstado = yo?.UsuId ?? 0;

    if (desdeEstado > 0) {
      this.usuId = desdeEstado;
    }

    return desdeEstado;
  }

  async abandonarPartida(): Promise<void> {
    if (this.abandonoEnCurso) return;

    const usuId = this.resolverUsuId();

    if (usuId <= 0) {
      await this.mostrarToast(
        'No se pudo identificar tu usuario. Cerrá sesión y volvé a entrar.',
        'danger'
      );
      return;
    }

    const alert = await this.alertController.create({
      cssClass: 'alert-parchis',
      header:   'Abandonar partida',
      message:  'Perderás el 20% de tu entrada como penalización. Un bot tomará tu lugar. ¿Confirmás?',
      buttons: [
        { text: 'Seguir jugando', role: 'cancel', cssClass: 'btn-cancelar' },
        {
          text: 'Abandonar',
          cssClass: 'btn-peligro',
          handler: () => {
            this.confirmarAbandono(usuId);
            return true;
          }
        }
      ]
    });

    await alert.present();
  }

  // La navegación ocurre cuando llega AbandonoConfirmado. Si el
  // servidor no contesta en 5 segundos, salimos igual.
  private async confirmarAbandono(usuId: number): Promise<void> {
    this.abandonoEnCurso = true;

    this.timerAbandono = setTimeout(async () => {
      if (!this.abandonoEnCurso) return;

      this.abandonoEnCurso = false;
      await this.mostrarToast('El servidor no respondió. Volviendo al inicio.', 'warning');
      this.router.navigate(['/home'], { replaceUrl: true });
    }, 5000);

    await this.signalR.abandonarPartida(this.parId, usuId);
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

  // ── mostrarMensajeFlotante (RF-09) ───────────────────────────
  // La burbuja dura 3 segundos. Si el mismo jugador manda otro antes
  // de que expire, se reinicia el reloj en vez de acumular dos
  // timeouts peleando por la misma clave.
  private mostrarMensajeFlotante(mensaje: any): void {
    const jpId  = mensaje?.JpId;
    const texto = mensaje?.Contenido;

    if (!jpId || !texto) return;

    const anterior = this.timersFlotantes.get(jpId);
    if (anterior) clearTimeout(anterior);

    // Se reasigna el Map completo para que Angular detecte el cambio:
    // mutar un Map no dispara la detección por sí solo.
    this.mensajesFlotantes = new Map(this.mensajesFlotantes).set(jpId, texto);

    const timer = setTimeout(() => {
      const copia = new Map(this.mensajesFlotantes);
      copia.delete(jpId);
      this.mensajesFlotantes = copia;
      this.timersFlotantes.delete(jpId);
    }, 3000);

    this.timersFlotantes.set(jpId, timer);
  }

  // ── getMensajeFlotante ───────────────────────────────────────
  // Devuelve el mensaje activo de la primera ficha que haya en la
  // celda, o null. El template lo usa para decidir si dibuja burbuja.
  getMensajeFlotante(fila: number, columna: number): string | null {
    if (this.mensajesFlotantes.size === 0) return null;

    const fichas = this.getFichasEnCelda(fila, columna);

    for (const ficha of fichas) {
      const texto = this.mensajesFlotantes.get(ficha.JpId);
      if (texto) return texto;
    }

    return null;
  }

  // Color de quien manda el mensaje, para teñir la burbuja
  getColorMensajeFlotante(fila: number, columna: number): string {
    const fichas = this.getFichasEnCelda(fila, columna);

    for (const ficha of fichas) {
      if (this.mensajesFlotantes.has(ficha.JpId)) {
        return (ficha.Color ?? 'azul').toLowerCase();
      }
    }

    return 'azul';
  }

  // ================================================================
  // RENDERIZADO DEL TABLERO
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

  // Sin trackBy, Angular recrea los divs en cada render y la
  // transición CSS nunca llega a correr.
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