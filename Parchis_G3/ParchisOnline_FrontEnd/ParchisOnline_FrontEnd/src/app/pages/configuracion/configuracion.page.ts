import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  IonContent, IonIcon, IonButton, IonSpinner, IonInput, IonToggle,
  ToastController, AlertController
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  settingsOutline, arrowBack, personOutline, lockClosedOutline,
  volumeHighOutline, musicalNotesOutline, notificationsOutline,
  logOutOutline, trashOutline, saveOutline, eyeOutline, eyeOffOutline
} from 'ionicons/icons';
import { PerfilService } from '../../services/perfil.service';
import { AuthService } from '../../services/auth';
import { InactividadService } from '../../services/inactividad';

@Component({
  selector:    'app-configuracion',
  templateUrl: './configuracion.page.html',
  styleUrls:   ['./configuracion.page.scss'],
  standalone:  true,
  imports: [
    CommonModule, FormsModule,
    IonContent, IonIcon, IonButton, IonSpinner, IonInput, IonToggle
  ]
})
export class ConfiguracionPage implements OnInit {

  usuario:  any     = null;
  cargando: boolean = true;
  guardando: boolean = false;

  // ── Datos personales ──────────────────────────────────────────
  nombre: string = '';
  avatar: number = 1;
  avatares: number[] = [1, 2, 3, 4, 5, 6];

  // ── Preferencias ──────────────────────────────────────────────
  sonidosActivos:        boolean = true;
  musicaActiva:          boolean = true;
  notificacionesActivas: boolean = true;

  // ── Cambio de contraseña ──────────────────────────────────────
  passwordActual:  string = '';
  passwordNueva:   string = '';
  passwordRepetir: string = '';
  mostrarPasswords: boolean = false;

  constructor(
    private perfilService:   PerfilService,
    private authService:     AuthService,
    private inactividad:     InactividadService,
    private router:          Router,
    private toastController: ToastController,
    private alertController: AlertController
  ) {
    addIcons({
      settingsOutline, arrowBack, personOutline, lockClosedOutline,
      volumeHighOutline, musicalNotesOutline, notificationsOutline,
      logOutOutline, trashOutline, saveOutline, eyeOutline, eyeOffOutline
    });
  }

  ngOnInit(): void {
    this.cargarPerfil();
  }

  private cargarPerfil(): void {
    this.cargando = true;

    this.perfilService.obtenerPerfil().subscribe({
      next: (respuesta: any) => {
        this.usuario  = respuesta.ValorRetorno;
        this.cargando = false;

        this.nombre = this.usuario?.UsuNombre ?? '';
        this.avatar = this.usuario?.UsuAvatar ?? 1;

        // ?? y no ||: con || un false guardado se convertiría en true
        this.sonidosActivos        = this.usuario?.UsuSonidosActivos ?? true;
        this.musicaActiva          = this.usuario?.UsuMusicaActiva ?? true;
        this.notificacionesActivas = this.usuario?.UsuNotificacionesActivas ?? true;
      },
      error: async () => {
        this.cargando = false;
        await this.mostrarToast('No se pudo cargar la configuración.', 'danger');
      }
    });
  }

  
  // DATOS PERSONALES
 
  guardarDatos(): void {
    const nombre = this.nombre.trim();

    if (nombre.length < 2) {
      this.mostrarToast('El nombre debe tener al menos 2 caracteres.', 'warning');
      return;
    }

    if (nombre.length > 100) {
      this.mostrarToast('El nombre no puede superar los 100 caracteres.', 'warning');
      return;
    }

    this.guardando = true;

    this.perfilService.actualizarDatos(nombre, this.avatar).subscribe({
      next: async (respuesta: any) => {
        this.guardando = false;
        this.usuario = respuesta.ValorRetorno;
        localStorage.setItem('usuario', JSON.stringify(this.usuario));
        await this.mostrarToast('Datos actualizados.', 'success');
      },
      error: async (error: any) => {
        this.guardando = false;
        await this.mostrarError(error, 'No se pudieron guardar los datos.');
      }
    });
  }

  seleccionarAvatar(numero: number): void {
    this.avatar = numero;
  }


  // PREFERENCIAS
  // Se guardan al instante en vez de con un botón: un toggle que hay
  // que confirmar aparte se siente roto.
  guardarPreferencias(): void {
    this.perfilService.actualizarPreferencias(
      this.sonidosActivos,
      this.musicaActiva,
      this.notificacionesActivas
    ).subscribe({
      next: (respuesta: any) => {
        this.usuario = respuesta.ValorRetorno;
        localStorage.setItem('usuario', JSON.stringify(this.usuario));
      },
      error: async (error: any) => {
        await this.mostrarError(error, 'No se pudieron guardar las preferencias.');
        // Recargamos para que los toggles reflejen lo que hay en la BD
        this.cargarPerfil();
      }
    });
  }


  // CONTRASEÑA
 
  cambiarPassword(): void {
    if (!this.passwordActual || !this.passwordNueva) {
      this.mostrarToast('Completá ambas contraseñas.', 'warning');
      return;
    }

    if (this.passwordNueva.length < 8) {
      this.mostrarToast('La nueva contraseña debe tener al menos 8 caracteres.', 'warning');
      return;
    }

    if (this.passwordNueva !== this.passwordRepetir) {
      this.mostrarToast('Las contraseñas nuevas no coinciden.', 'warning');
      return;
    }

    if (this.passwordActual === this.passwordNueva) {
      this.mostrarToast('La nueva contraseña debe ser distinta de la actual.', 'warning');
      return;
    }

    this.guardando = true;

    this.perfilService.cambiarPassword(this.passwordActual, this.passwordNueva).subscribe({
      next: async () => {
        this.guardando = false;
        this.limpiarCamposPassword();
        await this.mostrarToast('Contraseña actualizada correctamente.', 'success');
      },
      error: async (error: any) => {
        this.guardando = false;
        await this.mostrarError(error, 'No se pudo cambiar la contraseña.');
      }
    });
  }

  private limpiarCamposPassword(): void {
    this.passwordActual  = '';
    this.passwordNueva   = '';
    this.passwordRepetir = '';
  }

  togglePasswords(): void {
    this.mostrarPasswords = !this.mostrarPasswords;
  }


  // SESIÓN Y CUENTA
  
  async cerrarSesion(): Promise<void> {
    const alert = await this.alertController.create({
      cssClass: 'alert-parchis',
      header:   'Cerrar sesión',
      message:  '¿Estás seguro que querés cerrar sesión?',
      buttons: [
        { text: 'Cancelar', role: 'cancel', cssClass: 'btn-cancelar' },
        {
          text: 'Cerrar sesión',
          cssClass: 'btn-peligro',
          handler: () => {
            // Detener el reloj de inactividad antes del logout: si no,
            // sigue corriendo y podría disparar un cierre sobre una
            // sesión que ya no existe.
            this.inactividad.detener();
            this.authService.logout();
            this.router.navigate(['/login'], { replaceUrl: true });
            return true;
          }
        }
      ]
    });

    await alert.present();
  }

  // Doble confirmación: la primera avisa, la segunda pide escribir
  // ELIMINAR. Es destructivo e irreversible.
  async eliminarCuenta(): Promise<void> {
    const primera = await this.alertController.create({
      cssClass: 'alert-parchis',
      header:   'Eliminar cuenta',
      message:  'Se borrarán tus monedas, tu inventario y tu historial. Esta acción no se puede deshacer.',
      buttons: [
        { text: 'Cancelar', role: 'cancel', cssClass: 'btn-cancelar' },
        {
          text: 'Continuar',
          cssClass: 'btn-peligro',
          handler: () => {
            this.confirmarEliminacion();
            return true;
          }
        }
      ]
    });

    await primera.present();
  }

  private async confirmarEliminacion(): Promise<void> {
    const alert = await this.alertController.create({
      cssClass: 'alert-parchis',
      header:   '¿Seguro?',
      message:  'Escribí ELIMINAR para confirmar.',
      inputs: [
        { name: 'confirmacion', type: 'text', placeholder: 'ELIMINAR' }
      ],
      buttons: [
        { text: 'Cancelar', role: 'cancel', cssClass: 'btn-cancelar' },
        {
          text: 'Eliminar cuenta',
          cssClass: 'btn-peligro',
          handler: (datos) => {
            if (datos.confirmacion?.trim().toUpperCase() !== 'ELIMINAR') {
              this.mostrarToast('Confirmación incorrecta.', 'warning');
              return false;
            }

            this.ejecutarEliminacion();
            return true;
          }
        }
      ]
    });

    await alert.present();
  }

  private ejecutarEliminacion(): void {
    this.perfilService.eliminarCuenta().subscribe({
      next: async () => {
        this.inactividad.detener();
        this.authService.logout();
        await this.mostrarToast('Tu cuenta fue eliminada.', 'medium');
        this.router.navigate(['/login'], { replaceUrl: true });
      },
      error: async (error: any) => {
        await this.mostrarError(error, 'No se pudo eliminar la cuenta.');
      }
    });
  }


  // NAVEGACIÓN Y HELPERS

  volver(): void {
    this.router.navigate(['/perfil']);
  }

  private async mostrarError(error: any, respaldo: string): Promise<void> {
    const mensaje = error?.error?.strMensajeRespuesta
                 ?? error?.error?.mensaje
                 ?? respaldo;

    await this.mostrarToast(mensaje, 'danger');
  }

  private async mostrarToast(mensaje: string, color: string): Promise<void> {
    const toast = await this.toastController.create({
      message:  mensaje,
      duration: 2600,
      color,
      position: 'top'
    });
    await toast.present();
  }
}
