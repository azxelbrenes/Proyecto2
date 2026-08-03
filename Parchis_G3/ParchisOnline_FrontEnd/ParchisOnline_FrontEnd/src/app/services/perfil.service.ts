import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth';

@Injectable({
  providedIn: 'root'
})
export class PerfilService {

  private apiUrl = 'http://localhost:5051/api';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  // ── obtenerPerfil ────────────────────────────────────────────
  // Trae los datos actuales del usuario autenticado (GET /api/usuario)
  // El ID se obtiene del JWT en el backend, no lo mandamos nosotros
  obtenerPerfil(): Observable<any> {
    return this.http.get(`${this.apiUrl}/usuario`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── actualizarPerfil ─────────────────────────────────────────
  // Actualiza los datos del usuario. IMPORTANTE: mandamos el objeto
  // COMPLETO (no solo el campo que cambió) porque el backend hace
  // un Modificar() que sobreescribe toda la fila — si mandamos un
  // objeto incompleto, los campos faltantes quedarían en null/0.
  actualizarPerfil(usuarioCompleto: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/usuario`, usuarioCompleto, {
      headers: this.authService.getHeaders()
    });
  }

  // ── obtenerEstadisticas ──────────────────────────────────────
  // Trae partidas jugadas, ganadas, perdidas y % de victoria
  obtenerEstadisticas(): Observable<any> {
    return this.http.get(`${this.apiUrl}/historial/estadisticas`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── obtenerTransacciones ─────────────────────────────────────
  // Trae el historial de movimientos de monedas del usuario
  obtenerTransacciones(): Observable<any> {
    return this.http.get(`${this.apiUrl}/transaccion/mias`, {
      headers: this.authService.getHeaders()
    });
  }
}
