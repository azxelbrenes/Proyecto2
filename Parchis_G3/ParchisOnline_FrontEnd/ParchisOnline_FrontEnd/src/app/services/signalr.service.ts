import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, BehaviorSubject } from 'rxjs';
import { AuthService } from './auth';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {

  // URL del Hub — debe coincidir con app.MapHub<PartidaHub>("/hubs/partida")
  private hubUrl = 'http://localhost:5051/hubs/partida';

  private hubConnection?: signalR.HubConnection;

  // ── Estado de la conexión ────────────────────────────────────
  // BehaviorSubject guarda el último valor, así un componente que
  // se suscriba tarde igual sabe si ya estamos conectados
  public conectado$ = new BehaviorSubject<boolean>(false);

  // ── Eventos del juego ────────────────────────────────────────
  // Subject normal porque son eventos puntuales: si te suscribís
  // después de que pasó, no tiene sentido recibirlo
  public onEstadoActualizado$   = new Subject<any>();
  public onDadoTirado$          = new Subject<any>();
  public onFichaMovida$         = new Subject<any>();
  public onPartidaFinalizada$   = new Subject<any>();

  // ── Eventos de chat ──────────────────────────────────────────
  public onMensajeRecibido$     = new Subject<any>();
  public onHistorialChat$       = new Subject<any>();

  // ── Eventos de conexión de jugadores ─────────────────────────
  public onJugadorDesconectado$ = new Subject<any>();
  public onJugadorReconectado$  = new Subject<any>();
  public onJugadorAbandono$     = new Subject<any>();
  public onAbandonoConfirmado$  = new Subject<any>();
  public onJugadoresReemplazados$ = new Subject<any>();

  // ── Errores del servidor ─────────────────────────────────────
  public onError$               = new Subject<string>();

  constructor(private authService: AuthService) {}

  // ================================================================
  // CONECTAR AL HUB
  // ================================================================
  // Se llama una vez al entrar a la sala de espera o al tablero.
  // Si ya hay una conexión activa, no crea otra.
  public async conectar(): Promise<boolean> {
    // Si ya estamos conectados, reutilizamos la conexión
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return true;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        // El token se manda como query string porque los
        // WebSockets no soportan headers personalizados
        accessTokenFactory: () => this.authService.getToken() ?? ''
      })
      // Reintenta automáticamente si se cae la conexión:
      // a los 0s, 2s, 5s y 10s. Si falla todo, avisa al componente.
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Registramos todos los listeners ANTES de iniciar la conexión
    this.registrarEventos();

    try {
      await this.hubConnection.start();
      this.conectado$.next(true);
      console.log('SignalR conectado.');
      return true;
    } catch (error) {
      console.error('Error al conectar con SignalR:', error);
      this.conectado$.next(false);
      return false;
    }
  }

  // ================================================================
  // REGISTRAR LOS EVENTOS QUE MANDA EL BACKEND
  // ================================================================
  // Cada .on() corresponde a un SendAsync() del PartidaHub.
  // Los nombres deben coincidir EXACTAMENTE con los del backend.
  private registrarEventos(): void {
    if (!this.hubConnection) return;

    // ── Estado del tablero ───────────────────────────────────
    this.hubConnection.on('EstadoActualizado', (estado) => {
      this.onEstadoActualizado$.next(estado);
    });

    this.hubConnection.on('DadoTirado', (resultado) => {
      this.onDadoTirado$.next(resultado);
    });

    this.hubConnection.on('FichaMovida', (resultado) => {
      this.onFichaMovida$.next(resultado);
    });

    this.hubConnection.on('PartidaFinalizada', (resultado) => {
      this.onPartidaFinalizada$.next(resultado);
    });

    // ── Chat ─────────────────────────────────────────────────
    this.hubConnection.on('MensajeRecibido', (mensaje) => {
      this.onMensajeRecibido$.next(mensaje);
    });

    this.hubConnection.on('HistorialChat', (historial) => {
      this.onHistorialChat$.next(historial);
    });

    // ── Conexión de jugadores ────────────────────────────────
    this.hubConnection.on('JugadorDesconectado', (datos) => {
      this.onJugadorDesconectado$.next(datos);
    });

    this.hubConnection.on('JugadorReconectado', (datos) => {
      this.onJugadorReconectado$.next(datos);
    });

    this.hubConnection.on('JugadorAbandono', (datos) => {
      this.onJugadorAbandono$.next(datos);
    });

    this.hubConnection.on('AbandonoConfirmado', (datos) => {
      this.onAbandonoConfirmado$.next(datos);
    });

    this.hubConnection.on('JugadoresReemplazados', (datos) => {
      this.onJugadoresReemplazados$.next(datos);
    });

    // ── Errores ──────────────────────────────────────────────
    this.hubConnection.on('Error', (mensaje: string) => {
      this.onError$.next(mensaje);
    });

    // ── Estado de la conexión misma ──────────────────────────
    this.hubConnection.onreconnecting(() => {
      console.warn('SignalR reconectando...');
      this.conectado$.next(false);
    });

    this.hubConnection.onreconnected(() => {
      console.log('SignalR reconectado.');
      this.conectado$.next(true);
    });

    this.hubConnection.onclose(() => {
      console.warn('SignalR desconectado.');
      this.conectado$.next(false);
    });
  }

  // ================================================================
  // MÉTODOS QUE LLAMAN AL BACKEND
  // ================================================================
  // Cada invoke() llama a un método público del PartidaHub.
  // Los nombres deben coincidir EXACTAMENTE.

  // Entra al grupo de la partida y recibe el estado inicial
  public async unirseAPartida(parId: number, jpId: number): Promise<void> {
    await this.invocar('UnirseAPartida', parId, jpId);
  }

  // Sale del grupo (salida normal, no desconexión)
  public async salirDePartida(parId: number): Promise<void> {
    await this.invocar('SalirDePartida', parId);
  }

  // Tira el dado — el servidor valida que sea tu turno
  public async tirarDado(parId: number, jpId: number): Promise<void> {
    await this.invocar('TirarDado', parId, jpId);
  }

  // Mueve una ficha con el dado ya tirado.
  // El valorDado debe ser el que devolvió TirarDado — si mandás
  // otro, el servidor lo rechaza (anti-trampa).
  public async moverFicha(parId: number, jpId: number, numeroFicha: number, valorDado: number): Promise<void> {
    await this.invocar('MoverFicha', parId, jpId, numeroFicha, valorDado);
  }

  // Envía un mensaje de chat (predefinido o libre)
  public async enviarMensaje(parId: number, jpId: number, contenido: string, esPredefinido: boolean): Promise<void> {
    await this.invocar('EnviarMensaje', parId, jpId, contenido, esPredefinido);
  }

  // Abandona la partida con penalización del 20%
  public async abandonarPartida(parId: number, usuId: number): Promise<void> {
    await this.invocar('AbandonarPartida', parId, usuId);
  }

  // Pide al servidor revisar si a alguien se le vencieron
  // los 60 segundos de reconexión
  public async verificarReconexiones(parId: number): Promise<void> {
    await this.invocar('VerificarReconexiones', parId);
  }

  // ================================================================
  // HELPER — invocar con validación de conexión
  // ================================================================
  // Centraliza el manejo de errores: si la conexión se cayó,
  // avisamos al componente en vez de que explote silenciosamente.
  private async invocar(metodo: string, ...args: any[]): Promise<void> {
    if (this.hubConnection?.state !== signalR.HubConnectionState.Connected) {
      this.onError$.next('No hay conexión con el servidor. Reintentando...');

      // Intentamos reconectar una vez antes de rendirnos
      const reconectado = await this.conectar();
      if (!reconectado) return;
    }

    try {
      await this.hubConnection!.invoke(metodo, ...args);
    } catch (error) {
      console.error(`Error al invocar ${metodo}:`, error);
      this.onError$.next(`Error al ejecutar la acción. Intentá de nuevo.`);
    }
  }

  // ================================================================
  // DESCONECTAR
  // ================================================================
  // Se llama al salir definitivamente del juego (logout, cerrar app).
  // NO se llama al navegar entre pantallas — queremos mantener la
  // conexión viva mientras el jugador esté en una partida.
  public async desconectar(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.hubConnection = undefined;
      this.conectado$.next(false);
    }
  }

  // Saber si estamos conectados en este momento
  public estaConectado(): boolean {
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }
}
