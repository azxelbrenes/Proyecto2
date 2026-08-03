// ================================================================
// tienda.page.ts — Lógica de la pantalla de Tienda
// ================================================================
// ¿QUÉ HACE ESTE ARCHIVO?
// - Carga los artículos según la categoría seleccionada (tab)
// - Permite comprar un artículo llamando a la API
// - Actualiza el saldo de monedas localmente tras la compra
// ================================================================

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import {
  IonContent, IonIcon, IonButton, IonSpinner, ToastController
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  storefront, storefrontOutline, logoBitcoin,
  home, personOutline, cubeOutline
} from 'ionicons/icons';
import { TiendaService } from '../../services/tienda.service';
import { AuthService } from '../../services/auth';

@Component({
  selector:    'app-tienda',
  templateUrl: './tienda.page.html',
  styleUrls:   ['./tienda.page.scss'],
  standalone:  true,
  imports: [CommonModule, IonContent, IonIcon, IonButton, IonSpinner]
})
export class TiendaPage implements OnInit {

  // ── Variables ─────────────────────────────────────────────────
  usuario:          any     = null;
  articulos:        any[]   = [];
  cargando:         boolean = true;
  comprando:        number | null = null; // ID del artículo comprándose (para el spinner)
  tipoSeleccionado: number  = 1;           // 1=Ficha por defecto

  // Definimos las 3 categorías según el orden de TiposArticulo en BD
  categorias = [
    { id: 1, nombre: 'Fichas',   emoji: '🔴' },
    { id: 2, nombre: 'Tableros', emoji: '🎲' },
    { id: 3, nombre: 'Dados',    emoji: '⚀'  }
  ];

  constructor(
    private tiendaService:   TiendaService,
    private authService:     AuthService,
    private router:          Router,
    private toastController: ToastController
  ) {
    addIcons({
      storefront, storefrontOutline, logoBitcoin,
      home, personOutline, cubeOutline
    });
  }

  ngOnInit(): void {
    this.usuario = this.authService.getUsuario();
    this.cargarArticulos();
  }

  // ── cargarArticulos ──────────────────────────────────────────
  // Trae los artículos de la categoría actualmente seleccionada
  cargarArticulos(): void {
    this.cargando = true;

    this.tiendaService.listarPorTipo(this.tipoSeleccionado).subscribe({
      next: (respuesta: any) => {
        this.cargando  = false;
        this.articulos = respuesta.ValorRetorno ?? [];
      },
      error: async () => {
        this.cargando = false;
        const toast = await this.toastController.create({
          message:  'No se pudieron cargar los artículos.',
          duration: 2500,
          color:    'danger',
          position: 'top'
        });
        await toast.present();
      }
    });
  }

  // ── cambiarCategoria ─────────────────────────────────────────
  // Cambia el tab activo y recarga los artículos de esa categoría
  cambiarCategoria(tipId: number): void {
    this.tipoSeleccionado = tipId;
    this.cargarArticulos();
  }

  // ── getEmojiCategoria ────────────────────────────────────────
  // Retorna el emoji según la categoría seleccionada actualmente
  getEmojiCategoria(): string {
    const categoria = this.categorias.find(c => c.id === this.tipoSeleccionado);
    return categoria?.emoji ?? '🎁';
  }

  // ── getTipoClass ─────────────────────────────────────────────
  // Retorna la clase CSS para el color del ícono según el tipo
  getTipoClass(): string {
    const clases: { [key: number]: string } = {
      1: 'tipo-ficha',
      2: 'tipo-tablero',
      3: 'tipo-dado'
    };
    return clases[this.tipoSeleccionado] ?? '';
  }

  // ── comprar ───────────────────────────────────────────────────
  // Compra el artículo seleccionado — valida saldo en el backend
  comprar(articulo: any): void {
    // Si es gratis (predeterminado) no hacemos nada
    if (articulo.ArtEsPredeterminado) return;

    // Verificamos saldo local antes de llamar a la API (UX rápida)
    if (this.usuario.UsuMonedasTotal < articulo.ArtPrecio) {
      this.mostrarToast('Saldo insuficiente para comprar este artículo.', 'warning');
      return;
    }

    this.comprando = articulo.ArtId;

    this.tiendaService.comprarArticulo(articulo.ArtId).subscribe({
      next: (respuesta: any) => {
        this.comprando = null;

        // Actualizamos el saldo local con el valor real del backend
        this.usuario.UsuMonedasTotal = respuesta.monedas;
        localStorage.setItem('usuario', JSON.stringify(this.usuario));

        this.mostrarToast(respuesta.mensaje, 'success');
      },
      error: (error: any) => {
        this.comprando = null;
        const mensaje = error.error?.mensaje
          ?? error.error?.strMensajeRespuesta
          ?? 'No se pudo completar la compra.';
        this.mostrarToast(mensaje, 'danger');
      }
    });
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

  irAPerfil(): void {
    this.router.navigate(['/perfil']);
  }
}
