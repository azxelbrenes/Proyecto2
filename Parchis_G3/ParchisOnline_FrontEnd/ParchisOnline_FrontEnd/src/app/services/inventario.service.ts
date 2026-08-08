import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth';

@Injectable({
  providedIn: 'root'
})
export class InventarioService {

  private apiUrl = 'http://localhost:5051/api';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  // ── obtenerMisArticulos ──────────────────────────────────────
  // Todos los artículos que el jugador desbloqueó (comprados +
  // los predeterminados gratuitos), marcando cuáles tiene puestos.
  // La tienda lo usa para mostrar "En tu inventario" en vez del
  // botón de precio.
  obtenerMisArticulos(): Observable<any> {
    return this.http.get(`${this.apiUrl}/inventario/articulos`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── obtenerMiEquipamiento ────────────────────────────────────
  // Qué ficha, tablero y dado tiene activos ahora mismo.
  // Nunca devuelve null: si el jugador nunca equipó nada, el
  // backend le devuelve los artículos predeterminados.
  obtenerMiEquipamiento(): Observable<any> {
    return this.http.get(`${this.apiUrl}/inventario/equipamiento`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── equiparArticulo ──────────────────────────────────────────
  // Cambia el artículo activo de su categoría. El backend valida
  // que el jugador realmente lo tenga desbloqueado antes de
  // permitir el cambio.
  equiparArticulo(artId: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/inventario/equipar`, {
      ArtId: artId
    }, {
      headers: this.authService.getHeaders()
    });
  }
}
