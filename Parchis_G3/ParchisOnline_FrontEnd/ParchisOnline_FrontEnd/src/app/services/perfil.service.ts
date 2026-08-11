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
  // Trae los datos del usuario autenticado. El ID sale del JWT en
  // el backend, no lo mandamos nosotros.
  obtenerPerfil(): Observable<any> {
    return this.http.get(`${this.apiUrl}/usuario`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── actualizarPerfil ─────────────────────────────────────────
  // Manda el objeto COMPLETO: el backend hace un Modificar() que
  // sobrescribe toda la fila, así que un objeto parcial dejaría los
  // campos faltantes en null o 0.
  actualizarPerfil(usuarioCompleto: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/usuario`, usuarioCompleto, {
      headers: this.authService.getHeaders()
    });
  }

  // ── obtenerEstadisticas ──────────────────────────────────────
  obtenerEstadisticas(): Observable<any> {
    return this.http.get(`${this.apiUrl}/historial/estadisticas`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── obtenerTransacciones ─────────────────────────────────────
  obtenerTransacciones(): Observable<any> {
    return this.http.get(`${this.apiUrl}/transaccion/mias`, {
      headers: this.authService.getHeaders()
    });
  }

  // ================================================================
  // CONFIGURACIÓN DE CUENTA (RF-15)
  // ================================================================
  // Estos endpoints reciben solo los campos que tocan, a diferencia
  // de actualizarPerfil(). Así el cliente no puede mandar monedas ni
  // el hash de la contraseña dentro del mismo objeto.

  // ── actualizarDatos ──────────────────────────────────────────
  // Nombre y avatar
  actualizarDatos(nombre: string, avatar: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/usuario/perfil`, {
      Nombre: nombre,
      Avatar: avatar
    }, {
      headers: this.authService.getHeaders()
    });
  }

  // ── actualizarPreferencias ───────────────────────────────────
  // Sonidos, música y notificaciones
  actualizarPreferencias(
    sonidos: boolean,
    musica: boolean,
    notificaciones: boolean
  ): Observable<any> {
    return this.http.put(`${this.apiUrl}/usuario/preferencias`, {
      SonidosActivos:        sonidos,
      MusicaActiva:          musica,
      NotificacionesActivas: notificaciones
    }, {
      headers: this.authService.getHeaders()
    });
  }

  // ── cambiarPassword ──────────────────────────────────────────
  // El backend verifica la contraseña actual contra el hash antes
  // de aceptar la nueva.
  cambiarPassword(passwordActual: string, passwordNueva: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/usuario/password`, {
      PasswordActual: passwordActual,
      PasswordNueva:  passwordNueva
    }, {
      headers: this.authService.getHeaders()
    });
  }

  // ── eliminarCuenta ───────────────────────────────────────────
  eliminarCuenta(): Observable<any> {
    return this.http.delete(`${this.apiUrl}/usuario`, {
      headers: this.authService.getHeaders()
    });
  }
}