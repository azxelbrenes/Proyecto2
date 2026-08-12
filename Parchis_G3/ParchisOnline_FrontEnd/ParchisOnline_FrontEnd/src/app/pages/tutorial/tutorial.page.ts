import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import {
  IonContent, IonIcon, IonButton, ToastController
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  arrowBack, arrowForward, checkmarkCircle, closeOutline
} from 'ionicons/icons';
import { PerfilService } from '../../services/perfil.service';
import { AuthService } from '../../services/auth';

interface PasoTutorial {
  titulo:      string;
  descripcion: string;
  emoji:       string;
  // Cada paso dibuja un mini tablero distinto para ilustrar la regla
  demo: 'tablero' | 'dado' | 'captura' | 'seguro' | 'meta';
}

@Component({
  selector:    'app-tutorial',
  templateUrl: './tutorial.page.html',
  styleUrls:   ['./tutorial.page.scss'],
  standalone:  true,
  imports: [CommonModule, IonContent, IonIcon, IonButton]
})
export class TutorialPage implements OnInit {

  pasoActual: number = 0;

  // Si se entra desde el menú (y no automáticamente en el primer
  // login), el botón de cerrar vuelve a donde estaba en vez de ir
  // siempre al home.
  desdeMenu: boolean = false;

  pasos: PasoTutorial[] = [
    {
      titulo:      'Bienvenido a Parchís Online',
      emoji:       '🎲',
      descripcion: 'Cada jugador tiene 4 fichas de su color. Gana quien lleve las cuatro hasta el centro del tablero antes que los demás.',
      demo:        'tablero'
    },
    {
      titulo:      'Sacá tus fichas con un 5',
      emoji:       '5️⃣',
      descripcion: 'Tus fichas empiezan en casa. Solo salen cuando sacás un 5 en el dado. Si sacás un 6, tirás de nuevo — pero al tercer 6 seguido perdés el turno y una ficha vuelve a casa.',
      demo:        'dado'
    },
    {
      titulo:      'Comé fichas rivales',
      emoji:       '⚔️',
      descripcion: 'Si caés justo donde hay una ficha de otro color, esa ficha vuelve a su casa y tiene que empezar de nuevo. Dos fichas del mismo color en una casilla forman un bloqueo: nadie puede pasar ni caer ahí.',
      demo:        'captura'
    },
    {
      titulo:      'Casillas seguras',
      emoji:       '⭐',
      descripcion: 'Las casillas con estrella y las de salida son seguras: ahí nadie te puede comer. La excepción es tu propia salida — si un rival la ocupa y vos sacás ficha de casa, se va.',
      demo:        'seguro'
    },
    {
      titulo:      'Llegá al centro',
      emoji:       '👑',
      descripcion: 'Al completar la vuelta entrás a tu pasillo de color, donde nadie te puede tocar. Hay que caer exacto en el centro para coronar. Tenés 30 segundos por turno: si se acaban, el sistema juega por vos.',
      demo:        'meta'
    }
  ];

  constructor(
    private perfilService:   PerfilService,
    private authService:     AuthService,
    private router:          Router,
    private route:           ActivatedRoute,
    private toastController: ToastController
  ) {
    addIcons({ arrowBack, arrowForward, checkmarkCircle, closeOutline });
  }

  ngOnInit(): void {
    this.desdeMenu = this.route.snapshot.queryParamMap.get('desdeMenu') === 'true';
  }

  get paso(): PasoTutorial {
    return this.pasos[this.pasoActual];
  }

  get esPrimero(): boolean {
    return this.pasoActual === 0;
  }

  get esUltimo(): boolean {
    return this.pasoActual === this.pasos.length - 1;
  }

  get progreso(): number {
    return ((this.pasoActual + 1) / this.pasos.length) * 100;
  }

  siguiente(): void {
    if (this.esUltimo) {
      this.finalizar();
      return;
    }
    this.pasoActual++;
  }

  anterior(): void {
    if (this.esPrimero) return;
    this.pasoActual--;
  }

  irAPaso(indice: number): void {
    this.pasoActual = indice;
  }

  // ── saltar ───────────────────────────────────────────────────
  // RF-16 pide que el tutorial se pueda saltar. Igual se marca como
  // completado: si no, volvería a aparecer en cada login y sería
  // más molesto que útil.
  saltar(): void {
    this.finalizar();
  }

  private finalizar(): void {
    this.marcarCompletado();

    if (this.desdeMenu) {
      this.router.navigate(['/perfil']);
    } else {
      this.router.navigate(['/home'], { replaceUrl: true });
    }
  }

  // Se guarda en el usuario para no volver a mostrarlo. Si la
  // petición falla no bloqueamos la navegación: como mucho el
  // tutorial reaparece la próxima vez.
  private marcarCompletado(): void {
    const usuario = this.authService.getUsuario();
    if (!usuario) return;

    const actualizado = { ...usuario, UsuTutorialCompletado: true };
    localStorage.setItem('usuario', JSON.stringify(actualizado));

    this.perfilService.actualizarPerfil(actualizado).subscribe({
      next:  () => {},
      error: () => {}
    });
  }
}
