import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import {
  IonContent, IonIcon, IonButton, IonSpinner,
  ToastController, ViewWillEnter
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  storefront, storefrontOutline, logoBitcoin, home,
  personOutline, cubeOutline, checkmarkCircle, sparkles,
  chevronForward
} from 'ionicons/icons';
import { TiendaService } from '../../services/tienda.service';
import { InventarioService } from '../../services/inventario.service';
import { AuthService } from '../../services/auth';

@Component({
  selector:    'app-tienda',
  templateUrl: './tienda.page.html',
  styleUrls:   ['./tienda.page.scss'],
  standalone:  true,
  imports: [CommonModule, IonContent, IonIcon, IonButton, IonSpinner]
})
export class TiendaPage implements OnInit, ViewWillEnter {

  // ── Variables ─────────────────────────────────────────────────
  usuario:          any     = null;
  articulos:        any[]   = [];   // catálogo de la categoría actual
  misArticulos:     any[]   = [];   // lo que el jugador ya tiene
  cargando:         boolean = true;
  comprando:        number | null = null;
  equipando:        number | null = null;
  tipoSeleccionado: number  = 1;    // 1=Ficha por defecto

  // Las 3 categorías según el orden de TiposArticulo en la BD
  categorias = [
    { id: 1, nombre: 'Fichas',   emoji: '🔴' },
    { id: 2, nombre: 'Tableros', emoji: '🎲' },
    { id: 3, nombre: 'Dados',    emoji: '🎯' }
  ];

  constructor(
    private tiendaService:     TiendaService,
    private inventarioService: InventarioService,
    private authService:       AuthService,
    private router:            Router,
    private toastController:   ToastController
  ) {
    addIcons({
      storefront, storefrontOutline, logoBitcoin, home,
      personOutline, cubeOutline, checkmarkCircle, sparkles,
      chevronForward
    });
  }

  ngOnInit(): void {
    this.usuario = this.authService.getUsuario();
    this.cargarTodo();
  }

  // Al volver de otra pantalla (por ejemplo de comprar monedas)
  // refrescamos el saldo y el inventario
  ionViewWillEnter(): void {
    this.usuario = this.authService.getUsuario();
    this.refrescarInventario();
  }


  // CARGAR CATÁLOGO + INVENTARIO
  // Primero traemos el inventario y después el catálogo, así
  // cuando se renderiza la lista ya sabemos qué tiene comprado.
  private cargarTodo(): void {
    this.cargando = true;

    this.inventarioService.obtenerMisArticulos().subscribe({
      next: (respuesta: any) => {
        this.misArticulos = respuesta.ValorRetorno ?? [];
        this.cargarArticulos();
      },
      error: () => {
        // Si falla el inventario igual mostramos el catálogo,
        // solo que sin marcar los comprados
        this.misArticulos = [];
        this.cargarArticulos();
      }
    });
  }

  private cargarArticulos(): void {
    this.tiendaService.listarPorTipo(this.tipoSeleccionado).subscribe({
      next: (respuesta: any) => {
        this.cargando  = false;
        this.articulos = respuesta.ValorRetorno ?? [];
      },
      error: async () => {
        this.cargando = false;
        await this.mostrarToast('No se pudieron cargar los artículos.', 'danger');
      }
    });
  }

  // ── cambiarCategoria ─────────────────────────────────────────
  cambiarCategoria(tipId: number): void {
    if (this.tipoSeleccionado === tipId) return;

    this.tipoSeleccionado = tipId;
    this.cargando = true;
    this.cargarArticulos();
  }

  // ESTADO DE CADA ARTÍCULO

  // ¿El jugador ya tiene este artículo desbloqueado?
  yaLoTiene(articulo: any): boolean {
    return this.misArticulos.some(a => a.ArtId === articulo.ArtId);
  }

  // ¿Lo tiene puesto ahora mismo?
  estaEquipado(articulo: any): boolean {
    const mio = this.misArticulos.find(a => a.ArtId === articulo.ArtId);
    return mio?.EstaEquipado === true;
  }

  // ¿Le alcanzan las monedas?
  puedeComprar(articulo: any): boolean {
    return (this.usuario?.UsuMonedasTotal ?? 0) >= articulo.ArtPrecio;
  }

  
  // SISTEMA DE RAREZA
  // La rareza se calcula según el precio. Le da jerarquía visual
  // al catálogo: los artículos caros se ven claramente más
  // valiosos que los baratos, aunque no leas el precio.
  getRareza(articulo: any): string {
    const precio = articulo.ArtPrecio;

    if (precio === 0)   return 'COMÚN';
    if (precio <= 2500) return 'RARO';
    if (precio <= 6000) return 'ÉPICO';
    return 'LEGENDARIO';
  }

  getRarezaClass(articulo: any): string {
    const precio = articulo.ArtPrecio;

    if (precio === 0)   return 'rareza-comun';
    if (precio <= 2500) return 'rareza-raro';
    if (precio <= 6000) return 'rareza-epico';
    return 'rareza-legendario';
  }
  // IDENTIDAD VISUAL POR ARTÍCULO
  // Este es el arreglo más importante del rediseño. Antes TODOS
  // los artículos se veían idénticos (la misma ficha roja sobre
  // amarillo), así que el jugador no sabía qué estaba comprando.
  //
  // Ahora cada uno tiene su propio gradiente y efecto según su
  // nombre: el dorado brilla, el cristal es translúcido, el neón
  // tiene resplandor, etc.
  getItemClass(articulo: any): string {
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

  
  // COMPRAR
  comprar(articulo: any): void {
    // Los predeterminados no se compran, ya los tiene
    if (this.yaLoTiene(articulo)) return;

    // Validación local rápida — el backend igual la revalida.
    // Si no le alcanza, le sugerimos ir a comprar monedas.
    if (!this.puedeComprar(articulo)) {
      this.mostrarToast(
        'Saldo insuficiente. Podés comprar monedas con dinero real.',
        'warning'
      );
      return;
    }

    this.comprando = articulo.ArtId;

    this.tiendaService.comprarArticulo(articulo.ArtId).subscribe({
      next: (respuesta: any) => {
        this.comprando = null;

        // Actualizamos el saldo con el valor real del backend
        this.usuario.UsuMonedasTotal = respuesta.monedas;
        localStorage.setItem('usuario', JSON.stringify(this.usuario));

        // Refrescamos el inventario para que aparezca como comprado
        this.refrescarInventario();

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

  // EQUIPAR
  // Pone el artículo como activo en su categoría. El backend hace
  // un upsert: si ya tenía otra ficha equipada, la reemplaza.
  equipar(articulo: any): void {
    if (this.estaEquipado(articulo)) return;

    this.equipando = articulo.ArtId;

    this.inventarioService.equiparArticulo(articulo.ArtId).subscribe({
      next: () => {
        this.equipando = null;
        this.refrescarInventario();
        this.mostrarToast(`${articulo.ArtNombre} equipado ✓`, 'success');
      },
      error: (error: any) => {
        this.equipando = null;
        const mensaje = error.error?.strMensajeRespuesta
          ?? 'No se pudo equipar el artículo.';
        this.mostrarToast(mensaje, 'danger');
      }
    });
  }

  // Recarga solo el inventario, sin volver a pedir el catálogo
  private refrescarInventario(): void {
    this.inventarioService.obtenerMisArticulos().subscribe({
      next: (respuesta: any) => {
        this.misArticulos = respuesta.ValorRetorno ?? [];
      }
    });
  }

  // HELPERS
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

  // Lleva a la pantalla de compra de monedas con dinero real
  irATiendaMonedas(): void {
    this.router.navigate(['/tienda-monedas']);
  }

  irAHome(): void {
    this.router.navigate(['/home']);
  }

  irAPerfil(): void {
    this.router.navigate(['/perfil']);
  }
}
