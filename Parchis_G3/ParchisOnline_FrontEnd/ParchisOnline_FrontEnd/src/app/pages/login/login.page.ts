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
  mostrarPassword: boolean = false;  // Toggle ojo de contraseña
  cargando:        boolean = false;  // Spinner mientras carga
  errorMsg:        string  = '';     // Mensaje de error visible

  constructor(
    private authService:     AuthService,
    private router:          Router,
    private toastController: ToastController,
    private loadingCtrl:     LoadingController
  ) {
    // Registramos los íconos que usamos en el HTML
    addIcons({
      mailOutline, lockClosedOutline,
      eyeOutline, eyeOffOutline, alertCircleOutline
    });
  }

  // ── togglePassword ───────────────────────────────────────────
  // Muestra u oculta la contraseña al tocar el ícono del ojo
  togglePassword(): void {
    this.mostrarPassword = !this.mostrarPassword;
  }

  // ── iniciarSesion ────────────────────────────────────────────
  // Valida el formulario y llama a la API de login
  async iniciarSesion(): Promise<void> {
    // Limpiamos el error anterior
    this.errorMsg = '';

    // ── Validaciones del formulario ──────────────────────────
    if (!this.correo.trim()) {
      this.errorMsg = 'El correo electrónico es requerido.';
      return;
    }

    if (!this.password.trim()) {
      this.errorMsg = 'La contraseña es requerida.';
      return;
    }

    // Validación básica de formato de correo
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.correo)) {
      this.errorMsg = 'El formato del correo no es válido.';
      return;
    }

    this.cargando = true;

    // ── Llamada a la API ─────────────────────────────────────
    this.authService.login(this.correo, this.password).subscribe({
      next: async (respuesta) => {
        this.cargando = false;

        // Login exitoso — mostramos toast y navegamos al home
        const toast = await this.toastController.create({
          message:  `¡Bienvenido, ${respuesta.usuario.UsuNombre}! 🎲`,
          duration: 2000,
          color:    'success',
          position: 'top'
        });
        await toast.present();

        // Navegamos al home y limpiamos el historial
        // para que el usuario no pueda volver al login con el botón atrás
        this.router.navigate(['/home'], { replaceUrl: true });
      },
      error: (error) => {
        this.cargando = false;

        // Manejamos los diferentes tipos de error de la API
        if (error.status === 401) {
          this.errorMsg = 'Correo o contraseña incorrectos.';
        } else if (error.status === 0) {
          this.errorMsg = 'No se puede conectar con el servidor. Verificá tu conexión.';
        } else {
          this.errorMsg = error.error?.mensaje ?? 'Ocurrió un error. Intentá de nuevo.';
        }
      }
    });
  }

  // ── irARegistro ──────────────────────────────────────────────
  // Navega a la pantalla de registro
  irARegistro(): void {
    this.router.navigate(['/registro']);
  }
}
