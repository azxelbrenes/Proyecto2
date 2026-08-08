import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import {
  IonContent, IonButton, IonIcon, IonSpinner,
  ToastController, AlertController
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import { informationCircleOutline, closeCircleOutline } from 'ionicons/icons';
import { Subscription, interval } from 'rxjs';
import { PartidaService } from '../../services/partida.service';
import { SignalRService } from '../../services/signalr.service';
import { AuthService } from '../../services/auth';

@Component({
  selector:    'app-sala-espera',
  templateUrl: './sala-espera.page.html',
  styleUrls:   ['./sala-espera.page.scss'],
  standalone:  true,
  imports: [CommonModule, IonContent, IonButton, IonIcon, IonSpinner]
})
export class SalaEsperaPage implements OnInit, OnDestroy {

  // ── Datos de la partida ──────────────────────────────────────
  parId: number = 0;
  jpId:  number = 0;
  usuId: number = 0;

  estado: any = null;

  // ── Estado visual ────────────────────────────────────────────
  segundosRestantes: number  = 30;
  jugadoresActuales: number  = 0;
  mensajeEstado:     string  = 'Buscando jugadores...';
  iniciando:         boolean = false;

  // Los 4 slots de la mesa — siempre son 4, ocupados o vacíos
  slots: any[] = [
    { ocupado: false }, { ocupado: false },
    { ocupado: false }, { ocupado: false }
  ];

  // ── Cronómetro circular ──────────────────────────────────────
  // 2 × π × radio(52) = circunferencia del círculo SVG
  readonly circunferencia = 2 * Math.PI * 52;
  offsetProgreso = 0;

  // ── Suscripciones a limpiar ──────────────────────────────────
  private subs: Subscription[] = [];
  private pollingSub?: Subscription;

  constructor(
    private partidaService:  PartidaService,
    private signalR:         SignalRService,
    private authService:     AuthService,
    private router:          Router,
    private route:           ActivatedRoute,
    private toastController: ToastController,
    private alertController: AlertController
  ) {
    addIcons({ informationCircleOutline, closeCircleOutline });
  }

  async ngOnInit(): Promise<void> {
    // Los IDs vienen por query params desde el home
    this.parId = Number(this.route.snapshot.queryParamMap.get('parId'));
    this.jpId  = Number(this.route.snapshot.queryParamMap.get('jpId'));

    const usuario = this.authService.getUsuario();
    this.usuId = usuario?.UsuId ?? 0;

    // Sin IDs válidos no podemos hacer nada
    if (this.parId <= 0 || this.jpId <= 0) {
      await this.mostrarToast('Error al entrar a la partida.', 'danger');
      this.router.navigate(['/home'], { replaceUrl: true });
      return;
    }

    await this.conectarSignalR();
    this.cargarEstado();
    this.iniciarPolling();
  }

  ngOnDestroy(): void {
    // Limpiamos TODAS las suscripciones para evitar memory leaks.
    // Sin esto, el polling seguiría corriendo aunque salgamos.
    this.subs.forEach(s => s.unsubscribe());
    this.pollingSub?.unsubscribe();
  }

  
  // CONECTAR A SIGNALR
  private async conectarSignalR(): Promise<void> {
    const conectado = await this.signalR.conectar();

    if (!conectado) {
      await this.mostrarToast('No se pudo conectar al servidor de juego.', 'warning');
      return;
    }

    // Entramos al grupo de esta partida
    await this.signalR.unirseAPartida(this.parId, this.jpId);

    // ── Escuchamos el estado del tablero ─────────────────────
    // Si llega este evento significa que la partida YA INICIÓ
    // (el motor creó las fichas), así que navegamos al tablero
    this.subs.push(
      this.signalR.onEstadoActualizado$.subscribe((estadoPartida) => {
        if (estadoPartida?.ParEstado === 'EN_JUEGO') {
          this.irAlTablero();
        }
      })
    );

    // ── Errores del servidor ─────────────────────────────────
    this.subs.push(
      this.signalR.onError$.subscribe(async (mensaje) => {
        await this.mostrarToast(mensaje, 'danger');
      })
    );
  }


  // CARGAR ESTADO DE LA SALA
    private cargarEstado(): void {
    this.partidaService.obtenerEstadoEspera(this.parId).subscribe({
      next: (respuesta: any) => {
        this.estado = respuesta.ValorRetorno;

        if (!this.estado) return;

        // Si ya inició mientras cargábamos, vamos directo al tablero
        if (this.estado.PartidaIniciada) {
          this.irAlTablero();
          return;
        }

        this.jugadoresActuales = this.estado.JugadoresActuales ?? 0;
        this.segundosRestantes = this.estado.SegundosRestantes ?? 0;

        this.actualizarSlots();
        this.actualizarCronometro();
        this.actualizarMensaje();
      },
      error: async () => {
        await this.mostrarToast('No se pudo cargar el estado de la sala.', 'danger');
      }
    });
  }

  
  // POLLING CADA SEGUNDO
  
  // Refresca el cronómetro y, cuando llega a 0, le pide al backend
  // que complete con bots e inicie la partida.
  private iniciarPolling(): void {
    this.pollingSub = interval(1000).subscribe(() => {
      if (this.iniciando) return;

      // Bajamos el cronómetro localmente para que se vea fluido
      if (this.segundosRestantes > 0) {
        this.segundosRestantes--;
        this.actualizarCronometro();
      }

      // Cada 3 segundos refrescamos la lista real desde el backend
      if (this.segundosRestantes % 3 === 0) {
        this.cargarEstado();
      }

      // Cuando se acaba el tiempo o se llenó, pedimos iniciar
      if (this.segundosRestantes <= 0 || this.jugadoresActuales >= 4) {
        this.verificarEIniciar();
      }
    });
  }

  
  // VERIFICAR E INICIAR
  
  private verificarEIniciar(): void {
    if (this.iniciando) return;

    this.iniciando     = true;
    this.mensajeEstado = 'Iniciando partida...';

    this.partidaService.verificarEIniciar(this.parId).subscribe({
      next: (respuesta: any) => {
        // ValorRetorno true = la partida arrancó en esta llamada
        if (respuesta.ValorRetorno === true) {
          this.irAlTablero();
        } else {
          // Todavía no puede iniciar — reintentamos en el próximo ciclo
          this.iniciando = false;
        }
      },
      error: () => {
        this.iniciando = false;
      }
    });
  }

  
  // ACTUALIZAR LOS 4 SLOTS
  
  private actualizarSlots(): void {
    // Empezamos con los 4 vacíos
    this.slots = [
      { ocupado: false }, { ocupado: false },
      { ocupado: false }, { ocupado: false }
    ];

    const jugadores = this.estado?.Jugadores ?? [];

    for (const jugador of jugadores) {
      // JpPosicion va de 1 a 4, el array de 0 a 3
      const indice = (jugador.Posicion ?? 1) - 1;

      if (indice >= 0 && indice < 4) {
        this.slots[indice] = {
          ocupado: true,
          nombre:  jugador.Nombre,
          color:   jugador.Color,
          esBot:   jugador.EsBot,
          // Marcamos cuál es el jugador actual para resaltarlo
          esYo:    jugador.JpId === this.jpId
        };
      }
    }
  }

  // ACTUALIZAR EL CRONÓMETRO CIRCULAR

  // El offset del stroke-dasharray hace que el círculo se "vacíe".
  // Si quedan 30 seg de 30 → offset 0 (círculo lleno)
  // Si quedan 0 seg de 30  → offset = circunferencia (vacío)
  private actualizarCronometro(): void {
    const proporcion = this.segundosRestantes / 30;
    this.offsetProgreso = this.circunferencia * (1 - proporcion);
  }

  
  // MENSAJE SEGÚN EL ESTADO

  private actualizarMensaje(): void {
    if (this.jugadoresActuales >= 4) {
      this.mensajeEstado = '¡Mesa completa! Iniciando...';
    } else if (this.segundosRestantes <= 10) {
      this.mensajeEstado = 'Completando con bots...';
    } else {
      this.mensajeEstado = 'Buscando jugadores...';
    }
  }

 
  // NAVEGAR AL TABLERO
  
  private irAlTablero(): void {
    // Detenemos el polling antes de salir
    this.pollingSub?.unsubscribe();

    this.router.navigate(['/tablero'], {
      queryParams: { parId: this.parId, jpId: this.jpId },
      replaceUrl:  true   // así el botón atrás no vuelve a la espera
    });
  }

  
  // CANCELAR Y RECUPERAR MONEDAS
   async cancelarEspera(): Promise<void> {
    const alert = await this.alertController.create({
      cssClass: 'alert-parchis',
      header:   'Salir de la sala',
      message:  'Se te devolverán las monedas de entrada.\n\n¿Confirmás?',
      buttons: [
        {
          text: 'Seguir esperando',
          role: 'cancel',
          cssClass: 'btn-cancelar'
        },
        {
          text: 'Salir',
          cssClass: 'btn-peligro',
          handler: () => this.confirmarCancelacion()
        }
      ]
    });
    await alert.present();
  }


  private confirmarCancelacion(): void {
    this.pollingSub?.unsubscribe();

    this.partidaService.abandonarEspera(this.parId).subscribe({
      next: async (respuesta: any) => {
        // Actualizamos el saldo local con el valor real del backend
        const usuario = this.authService.getUsuario();
        if (usuario) {
          usuario.UsuMonedasTotal = respuesta.monedas;
          localStorage.setItem('usuario', JSON.stringify(usuario));
        }

        await this.mostrarToast(respuesta.mensaje, 'success');
        await this.signalR.salirDePartida(this.parId);
        this.router.navigate(['/home'], { replaceUrl: true });
      },
      error: async (error: any) => {
        const mensaje = error.error?.strMensajeRespuesta
          ?? 'No se pudo salir de la sala.';
        await this.mostrarToast(mensaje, 'danger');
        // Reanudamos el polling si falló la salida
        this.iniciarPolling();
      }
    });
  }

 
  // HELPERS VISUALES
  
  getSalaIcono(): string {
    const iconos: { [key: string]: string } = {
      'Sala Bronce':   '🥉',
      'Sala Plata':    '🥈',
      'Sala Oro':      '🥇',
      'Sala Diamante': '💎',
      'Sala Élite':    '👑'
    };
    return iconos[this.estado?.SalaNombre] ?? '🎲';
  }

  getSalaClass(): string {
    const clases: { [key: string]: string } = {
      'Sala Bronce':   'sala-bronce',
      'Sala Plata':    'sala-plata',
      'Sala Oro':      'sala-oro',
      'Sala Diamante': 'sala-diamante',
      'Sala Élite':    'sala-elite'
    };
    return clases[this.estado?.SalaNombre] ?? '';
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
