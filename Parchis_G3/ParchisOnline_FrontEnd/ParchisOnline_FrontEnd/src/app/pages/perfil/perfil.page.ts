import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  IonContent, IonIcon, IonButton, IonSpinner,
  IonInput, ToastController, AlertController
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  personCircleOutline, createOutline, checkmarkCircle, closeCircle,
  logoBitcoin, statsChartOutline, trophyOutline, logOutOutline,
  home, storefrontOutline, personOutline
} from 'ionicons/icons';
import { PerfilService } from '../../services/perfil.service';
import { AuthService } from '../../services/auth';

@Component({
  selector:    'app-perfil',
  templateUrl: './perfil.page.html',
  styleUrls:   ['./perfil.page.scss'],
  standalone:  true,
  imports: [
    CommonModule, FormsModule,
    IonContent, IonIcon, IonButton, IonSpinner, IonInput
  ]
})
export class PerfilPage implements OnInit {

  // ── Variables ─────────────────────────────────────────────────
  usuario:        any     = null; // Objeto COMPLETO traído de la API
  estadisticas:   any     = null;
  cargando:       boolean = true;
  editando:       boolean = false;
  nombreEditado:  string  = '';

  constructor(
    private perfilService:    PerfilService,
    private authService:      AuthService,
    private router:           Router,
    private toastController:  ToastController,
    private alertController:  AlertController
  ) {
    addIcons({
      personCircleOutline, createOutline, checkmarkCircle, closeCircle,
      logoBitcoin, statsChartOutline, trophyOutline, logOutOutline,
      home, storefrontOutline, personOutline
    });
  }

  ngOnInit(): void {
    this.cargarPerfil();
  }

  // ── cargarPerfil ─────────────────────────────────────────────
  // Trae el perfil completo y las estadísticas en paralelo
  cargarPerfil(): void {
    this.cargando = true;

    // Traemos el perfil completo desde la API (no solo localStorage)
    // para tener SIEMPRE todos los campos antes de permitir editar
    this.perfilService.obtenerPerfil().subscribe({
      next: (respuesta: any) => {
        this.usuario = respuesta.ValorRetorno;

        // Sincronizamos localStorage con los datos frescos
        localStorage.setItem('usuario', JSON.stringify(this.usuario));

        // Ahora cargamos las estadísticas
        this.cargarEstadisticas();
      },
      error: async () => {
        this.cargando = false;
        this.mostrarToast('No se pudo cargar el perfil.', 'danger');
      }
    });
  }

  // ── cargarEstadisticas ───────────────────────────────────────
  private cargarEstadisticas(): void {
    this.perfilService.obtenerEstadisticas().subscribe({
      next: (respuesta: any) => {
        this.cargando = false;
        this.estadisticas = respuesta;
      },
      error: () => {
        this.cargando = false;
        // Si falla estadísticas no bloqueamos el perfil completo
        this.estadisticas = { jugadas: 0, ganadas: 0, perdidas: 0, porcentajeVictoria: 0, historial: [] };
      }
    });
  }

  // ── activarEdicion ───────────────────────────────────────────
  activarEdicion(): void {
    this.nombreEditado = this.usuario.UsuNombre;
    this.editando = true;
  }

  // ── cancelarEdicion ──────────────────────────────────────────
  cancelarEdicion(): void {
    this.editando = false;
  }

  // ── guardarNombre ────────────────────────────────────────────
  // Manda el objeto COMPLETO del usuario (con el nombre ya cambiado)
  // para que el backend no sobreescriba otros campos con nulls.
  guardarNombre(): void {
    if (!this.nombreEditado.trim()) {
      this.mostrarToast('El nombre no puede estar vacío.', 'warning');
      return;
    }

    // Clonamos el usuario completo y solo cambiamos el nombre
    const usuarioActualizado = { ...this.usuario, UsuNombre: this.nombreEditado.trim() };

    this.perfilService.actualizarPerfil(usuarioActualizado).subscribe({
      next: (respuesta: any) => {
        this.usuario = respuesta.ValorRetorno;
        localStorage.setItem('usuario', JSON.stringify(this.usuario));
        this.editando = false;
        this.mostrarToast('Nombre actualizado correctamente.', 'success');
      },
      error: () => {
        this.mostrarToast('No se pudo actualizar el nombre.', 'danger');
      }
    });
  }

  // ── cerrarSesion ─────────────────────────────────────────────
  async cerrarSesion(): Promise<void> {
    const alert = await this.alertController.create({
      header:  'Cerrar sesión',
      message: '¿Estás seguro que querés cerrar sesión?',
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        {
          text: 'Cerrar sesión',
          role: 'destructive',
          handler: () => {
            this.authService.logout();
            this.router.navigate(['/login'], { replaceUrl: true });
          }
        }
      ]
    });
    await alert.present();
  }

  // ── mostrarToast ─────────────────────────────────────────────
  private async mostrarToast(mensaje: string, color: string): Promise<void> {
    const toast = await this.toastController.create({
      message:  mensaje,
      duration: 2200,
      color,
      position: 'top'
    });
    await toast.present();
  }

  // ── Navegación ───────────────────────────────────────────────
  irAHome(): void {
    this.router.navigate(['/home']);
  }

  irATienda(): void {
    this.router.navigate(['/tienda']);
  }
}
