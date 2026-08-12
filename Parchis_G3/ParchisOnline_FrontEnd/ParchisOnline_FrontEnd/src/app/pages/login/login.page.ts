import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  IonContent, IonItem, IonInput, IonButton,
  IonIcon, IonSpinner, ToastController, LoadingController
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  mailOutline, lockClosedOutline,
  eyeOutline, eyeOffOutline, alertCircleOutline
} from 'ionicons/icons';
import { AuthService } from '../../services/auth';
import { InactividadService } from '../../services/inactividad';

@Component({
  selector: 'app-login',
  templateUrl: './login.page.html',
  styleUrls:   ['./login.page.scss'],
  standalone:  true,
  imports: [
    CommonModule, FormsModule,
    IonContent, IonItem, IonInput, IonButton,
    IonIcon, IonSpinner
  ]
})
export class LoginPage {

  // ── Variables del formulario ─────────────────────────────────
  correo:          string  = '';
  password:        string  = '';
  mostrarPassword: boolean = false;
  cargando:        boolean = false;
  errorMsg:        string  = '';

  constructor(
    private authService:     AuthService,
    private inactividad:     InactividadService,
    private router:          Router,
    private toastController: ToastController,
    private loadingCtrl:     LoadingController
  ) {
    addIcons({
      mailOutline, lockClosedOutline,
      eyeOutline, eyeOffOutline, alertCircleOutline
    });
  }

  // ── togglePassword ───────────────────────────────────────────
  togglePassword(): void {
    this.mostrarPassword = !this.mostrarPassword;
  }

  // ── iniciarSesion ────────────────────────────────────────────
  async iniciarSesion(): Promise<void> {
    this.errorMsg = '';

    // ── Validaciones del formulario ──────────────────────────
    // Son solo para ahorrar un request: el servidor valida igual.
    if (!this.correo.trim()) {
      this.errorMsg = 'El correo electrónico es requerido.';
      return;
    }

    if (!this.password.trim()) {
      this.errorMsg = 'La contraseña es requerida.';
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.correo)) {
      this.errorMsg = 'El formato del correo no es válido.';
      return;
    }

    this.cargando = true;

    this.authService.login(this.correo, this.password).subscribe({
      next: async (respuesta) => {
        this.cargando = false;

        // RF-20: arranca el reloj de los 30 minutos de inactividad.
        // Tiene que ser acá y no en AppComponent: el ngOnInit de la
        // app ya corrió antes de que existiera la sesión.
        this.inactividad.iniciar();

        const toast = await this.toastController.create({
          message:  `¡Bienvenido, ${respuesta.usuario.UsuNombre}! 🎲`,
          duration: 2000,
          color:    'success',
          position: 'top'
        });
        await toast.present();

        // RF-16: el tutorial se muestra una sola vez, en el primer
        // ingreso. Después queda accesible desde el perfil.
        const tutorialVisto = respuesta.usuario?.UsuTutorialCompletado === true;
        const destino = tutorialVisto ? '/home' : '/tutorial';

        // replaceUrl para que el botón atrás no vuelva al login
        this.router.navigate([destino], { replaceUrl: true });
      },
      error: (error) => {
        this.cargando = false;

        if (error.status === 401) {
          this.errorMsg = 'Correo o contraseña incorrectos.';
        } else if (error.status === 0) {
          this.errorMsg = 'No se puede conectar con el servidor. Verificá tu conexión.';
        } else if (error.status === 429) {
          // Rate limiting: 5 intentos por minuto por IP
          this.errorMsg = 'Demasiados intentos. Esperá un momento e intentá de nuevo.';
        } else {
          this.errorMsg = error.error?.strMensajeRespuesta
                       ?? error.error?.mensaje
                       ?? 'Ocurrió un error. Intentá de nuevo.';
        }
      }
    });
  }

  // ── irARegistro ──────────────────────────────────────────────
  irARegistro(): void {
    this.router.navigate(['/registro']);
  }
}