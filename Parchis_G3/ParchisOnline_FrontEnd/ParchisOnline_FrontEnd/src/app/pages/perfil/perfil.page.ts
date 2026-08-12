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
  home, storefrontOutline, personOutline,
  shirtOutline, trophy, chevronForward
} from 'ionicons/icons';
import { PerfilService } from '../../services/perfil.service';
import { InventarioService } from '../../services/inventario.service';
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
  usuario:       any     = null;  // Objeto COMPLETO traído de la API
  estadisticas:  any     = null;
  equipamiento:  any     = null;  // Ficha, tablero y dado equipados
  cargando:      boolean = true;
  editando:      boolean = false;
  nombreEditado: string  = '';

  constructor(
    private perfilService:     PerfilService,
    private inventarioService: InventarioService,
    private authService:       AuthService,
    private router:            Router,
    private toastController:   ToastController,
    private alertController:   AlertController
  ) {
    addIcons({
      personCircleOutline, createOutline, checkmarkCircle, closeCircle,
      logoBitcoin, statsChartOutline, trophyOutline, logOutOutline,
      home, storefrontOutline, personOutline,
      shirtOutline, trophy, chevronForward
    });
  }

  ngOnInit(): void {
    this.cargarPerfil();
  }


  // CARGAR PERFIL
  
  // Trae el perfil completo desde la API. Después dispara la carga
  // del equipamiento y las estadísticas en paralelo.
  cargarPerfil(): void {
    this.cargando = true;

    this.perfilService.obtenerPerfil().subscribe({
      next: (respuesta: any) => {
        this.usuario = respuesta.ValorRetorno;

        // Sincronizamos localStorage con los datos frescos
        localStorage.setItem('usuario', JSON.stringify(this.usuario));

        this.cargarEquipamiento();
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
        this.cargando     = false;
        this.estadisticas = respuesta;
      },
      error: () => {
        this.cargando = false;
        // Si falla estadísticas no bloqueamos el perfil completo:
        // mostramos ceros en vez de dejar la pantalla vacía
        this.estadisticas = {
          jugadas: 0, ganadas: 0, perdidas: 0,
          porcentajeVictoria: 0, historial: []
        };
      }
    });
  }

 
  // EQUIPAMIENTO

  // Trae qué ficha, tablero y dado tiene el jugador equipados.
  // El backend nunca devuelve null: si nunca equipó nada, le
  // manda los artículos predeterminados gratuitos.
  private cargarEquipamiento(): void {
    this.inventarioService.obtenerMiEquipamiento().subscribe({
      next: (respuesta: any) => {
        this.equipamiento = respuesta.ValorRetorno;
      },
      error: () => {
        // Si falla, no bloqueamos el perfil — simplemente
        // no se muestra la sección de equipamiento
        this.equipamiento = null;
      }
    });
  }

  // ── getItemClass ─────────────────────────────────────────────
  // Devuelve la clase visual del artículo según su nombre, para
  // que la vista previa se vea como el artículo real (el dorado
  // brilla, el cristal es translúcido, el neón resplandece).
  // Es la misma lógica que usa la tienda, así se ven idénticos.
  getItemClass(articulo: any): string {
    if (!articulo) return 'item-clasico';

    const nombre = (articulo.ArtNombre ?? '').toLowerCase();

    if (nombre.includes('clásic') || nombre.includes('clasic')) return 'item-clasico';
    if (nombre.includes('dorad'))                                return 'item-dorado';
    if (nombre.includes('cristal'))                              return 'item-cristal';
    if (nombre.includes('neón')   || nombre.includes('neon'))    return 'item-neon';
    if (nombre.includes('diamante'))                             return 'item-diamante';
    if (nombre.includes('madera'))                               return 'item-madera';
    if (nombre.includes('galaxia'))                              return 'item-galaxia';

    return 'item-clasico';
  }

 
  // EDICIÓN DEL NOMBRE
 
  activarEdicion(): void {
    this.nombreEditado = this.usuario.UsuNombre;
    this.editando = true;
  }

  cancelarEdicion(): void {
    this.editando = false;
  }

  // ── guardarNombre ────────────────────────────────────────────
  // Manda el objeto COMPLETO del usuario con el nombre cambiado.
  // Si mandáramos solo { UsuNombre: "..." }, el backend haría un
  // Modificar() que sobrescribiría la fila entera y perderíamos
  // las monedas, la racha y todos los demás campos.
  guardarNombre(): void {
    const nombre = this.nombreEditado.trim();

    if (!nombre) {
      this.mostrarToast('El nombre no puede estar vacío.', 'warning');
      return;
    }

    if (nombre.length < 2) {
      this.mostrarToast('El nombre debe tener al menos 2 caracteres.', 'warning');
      return;
    }

    if (nombre.length > 100) {
      this.mostrarToast('El nombre no puede superar los 100 caracteres.', 'warning');
      return;
    }

    // Clonamos el usuario completo y solo cambiamos el nombre
    const usuarioActualizado = { ...this.usuario, UsuNombre: nombre };

    this.perfilService.actualizarPerfil(usuarioActualizado).subscribe({
      next: (respuesta: any) => {
        this.usuario = respuesta.ValorRetorno;
        localStorage.setItem('usuario', JSON.stringify(this.usuario));
        this.editando = false;
        this.mostrarToast('Nombre actualizado correctamente.', 'success');
      },
      error: (error: any) => {
        const mensaje = error.error?.strMensajeRespuesta
          ?? 'No se pudo actualizar el nombre.';
        this.mostrarToast(mensaje, 'danger');
      }
    });
  }

  
  // CERRAR SESIÓN
 
  async cerrarSesion(): Promise<void> {
    const alert = await this.alertController.create({
      cssClass: 'alert-parchis',
      header:   'Cerrar sesión',
      message:  '¿Estás seguro que querés cerrar sesión?',
      buttons: [
        {
          text: 'Cancelar',
          role: 'cancel',
          cssClass: 'btn-cancelar'
        },
        {
          text: 'Cerrar sesión',
          cssClass: 'btn-peligro',
          handler: () => {
            this.authService.logout();

            // Verificación extra por si el logout del servicio
            // no limpiara bien — sin esto el guard seguiría
            // dejando pasar al usuario
            localStorage.removeItem('token');
            localStorage.removeItem('usuario');

            this.router.navigate(['/login'], { replaceUrl: true });
          }
        }
      ]
    });
    await alert.present();
  }

  // ================================================================
  // HELPERS
  // ================================================================
  private async mostrarToast(mensaje: string, color: string): Promise<void> {
    const toast = await this.toastController.create({
      message:  mensaje,
      duration: 2200,
      color,
      position: 'top'
    });
    await toast.present();
  }

  // ── getAvatar ────────────────────────────────────────────────
  // Traduce el número guardado en UsuAvatar al emoji que le toca.
  // El HTML tenía el 🎮 escrito a mano, así que el avatar se guardaba
  // bien en la BD pero el perfil siempre mostraba el mismo.
  //
  // Este array está duplicado en configuracion.page.html. Con más
  // tiempo iría a un archivo compartido.
  getAvatar(): string {
    const emojis = ['🎮', '🎲', '👑', '🚀', '🐉', '⭐'];
    const indice = (this.usuario?.UsuAvatar ?? 1) - 1;
    return emojis[indice] ?? '🎮';
  }

  // ── Navegación ───────────────────────────────────────────────
  irARanking(): void {
    this.router.navigate(['/ranking']);
  }
  irAConfiguracion(): void {
  this.router.navigate(['/configuracion']);
}

  // Mandamos al jugador a la tienda para cambiar el equipamiento
  // porque ahí ya está toda la lógica de equipar. No la duplicamos
  // acá — sería mantener el mismo código en dos lugares.
  irACambiarEquipo(): void {
    this.router.navigate(['/tienda']);
  }

  irAHome(): void {
    this.router.navigate(['/home']);
  }

  irATienda(): void {
    this.router.navigate(['/tienda']);
  }
  irATutorial(): void {
  this.router.navigate(['/tutorial'], { queryParams: { desdeMenu: 'true' } });
}
irALogros(): void {
  this.router.navigate(['/logros']);
}
}