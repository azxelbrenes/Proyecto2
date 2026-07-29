// ================================================================
// registro.page.ts — Lógica de la pantalla de Registro
// ================================================================
// ¿QUÉ HACE ESTE ARCHIVO?
// Controla el formulario de registro:
// - Valida todos los campos antes de enviar
// - Verifica que las contraseñas coincidan
// - Llama al AuthService para crear la cuenta en la API
// - Al registrarse redirige al home automáticamente
// ================================================================

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  IonContent, IonItem, IonInput, IonButton,
  IonIcon, IonSpinner, ToastController
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  personOutline, mailOutline, lockClosedOutline,
  shieldCheckmarkOutline, eyeOutline, eyeOffOutline,
  alertCircleOutline, checkmarkCircleOutline, walletOutline,
  arrowBackOutline
} from 'ionicons/icons';
import { AuthService } from '../../services/auth.service';

@Component({
  selector:     'app-registro',
  templateUrl:  './registro.page.html',
  styleUrls:    ['./registro.page.scss'],
  standalone:   true,
  imports: [
    CommonModule, FormsModule,
    IonContent, IonItem, IonInput, IonButton,
    IonIcon, IonSpinner
  ]
})
export class RegistroPage {

  // ── Variables del formulario ─────────────────────────────────
  nombre:            string  = '';
  correo:            string  = '';
  password:          string  = '';
  confirmarPassword: string  = '';
  mostrarPassword:   boolean = false;
  mostrarConfirmar:  boolean = false;
  cargando:          boolean = false;
  errorMsg:          string  = '';
  exitoMsg:          string  = '';

  constructor(
    private authService:     AuthService,
    private router:          Router,
    private toastController: ToastController
  ) {
    // Registramos todos los íconos que usamos en el HTML
    addIcons({
      personOutline, mailOutline, lockClosedOutline,
      shieldCheckmarkOutline, eyeOutline, eyeOffOutline,
      alertCircleOutline, checkmarkCircleOutline,
      walletOutline, arrowBackOutline
    });
  }

  // ── togglePassword ───────────────────────────────────────────
  togglePassword(): void {
    this.mostrarPassword = !this.mostrarPassword;
  }

  // ── toggleConfirmar ──────────────────────────────────────────
  toggleConfirmar(): void {
    this.mostrarConfirmar = !this.mostrarConfirmar;
  }

  // ── crearCuenta ──────────────────────────────────────────────
  // Valida el formulario y llama a la API para crear la cuenta
  async crearCuenta(): Promise<void> {
    this.errorMsg = '';
    this.exitoMsg = '';

    // ── Validaciones ─────────────────────────────────────────
    if (!this.nombre.trim()) {
      this.errorMsg = 'El nombre es requerido.';
      return;
    }

    if (!this.correo.trim()) {
      this.errorMsg = 'El correo electrónico es requerido.';
      return;
    }

    // Validación de formato de correo
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.correo)) {
      this.errorMsg = 'El formato del correo no es válido.';
      return;
    }

    if (!this.password.trim()) {
      this.errorMsg = 'La contraseña es requerida.';
      return;
    }

    // Mínimo 6 caracteres para la contraseña
    if (this.password.length < 6) {
      this.errorMsg = 'La contraseña debe tener al menos 6 caracteres.';
      return;
    }

    // Las contraseñas deben coincidir
    if (this.password !== this.confirmarPassword) {
      this.errorMsg = 'Las contraseñas no coinciden.';
      return;
    }

    this.cargando = true;

    // ── Llamada a la API ─────────────────────────────────────
    this.authService.registro(this.nombre, this.correo, this.password).subscribe({
      next: async (respuesta) => {
        this.cargando = false;

        // Cuenta creada exitosamente
        const toast = await this.toastController.create({
          message:  `¡Cuenta creada! Recibiste 5,000 monedas 🎲`,
          duration: 2500,
          color:    'success',
          position: 'top'
        });
        await toast.present();

        // Navegamos al home y limpiamos el historial
        this.router.navigate(['/home'], { replaceUrl: true });
      },
      error: (error) => {
        this.cargando = false;

        if (error.status === 400) {
          // Error de validación — correo duplicado u otro error
          this.errorMsg = error.error?.strMensajeRespuesta
            ?? 'El correo ya está registrado.';
        } else if (error.status === 0) {
          this.errorMsg = 'No se puede conectar con el servidor.';
        } else {
          this.errorMsg = 'Ocurrió un error. Intentá de nuevo.';
        }
      }
    });
  }

  // ── volverAlLogin ────────────────────────────────────────────
  volverAlLogin(): void {
    this.router.navigate(['/login']);
  }
}
