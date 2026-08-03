import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth';
@Injectable({
  providedIn: 'root'
})
export class TiendaService {

  private apiUrl = 'http://localhost:5051/api';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  // ── listarTodos ──────────────────────────────────────────────
  // Trae todos los artículos de la tienda sin filtrar
  listarTodos(): Observable<any> {
    return this.http.get(`${this.apiUrl}/articulo`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── listarPorTipo ────────────────────────────────────────────
  // Filtra artículos por tipo: 1=Ficha, 2=Tablero, 3=Dado
  // (según el orden en que se insertaron en TiposArticulo)
  listarPorTipo(tipId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/articulo/tipo/${tipId}`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── comprarArticulo ──────────────────────────────────────────
  // Compra un artículo — el backend valida el precio real y
  // descuenta las monedas del usuario automáticamente
  comprarArticulo(artId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/articulo/comprar`, {
      ArtId: artId
    }, {
      headers: this.authService.getHeaders()
    });
  }
}
