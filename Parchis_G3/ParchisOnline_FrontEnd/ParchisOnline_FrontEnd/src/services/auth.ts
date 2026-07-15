
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  // URL base de tu API
  // En el celular físico cambiá localhost por tu IP local (ej: 192.168.1.5)
  private apiUrl = 'http://localhost:5051';

  constructor(private http: HttpClient) {}

  // ── login ────────────────────────────────────────────────────
  // Manda correo y contraseña, guarda el token JWT si es exitoso
  login(correo: string, password: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/login`, {
      Correo: correo,
      Password: password
    }).pipe(
      tap((respuesta: any) => {
        if (respuesta && respuesta.token) {
          localStorage.setItem('token',   respuesta.token);
          localStorage.setItem('usuario', JSON.stringify(respuesta.usuario));
        }
      })
    );
  }

  // ── registro ─────────────────────────────────────────────────
  // Crea cuenta nueva y guarda el token automáticamente
  registro(nombre: string, correo: string, password: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/registro`, {
      UsuNombre:       nombre,
      UsuCorreo:       correo,
      UsuPasswordHash: password
    }).pipe(
      tap((respuesta: any) => {
        if (respuesta && respuesta.token) {
          localStorage.setItem('token',   respuesta.token);
          localStorage.setItem('usuario', JSON.stringify(respuesta.usuario));
        }
      })
    );
  }

  // ── logout ───────────────────────────────────────────────────
  // Limpia el token y datos del usuario
  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('usuario');
  }

  // ── getToken ─────────────────────────────────────────────────
  // Retorna el JWT guardado
  getToken(): string | null {
    return localStorage.getItem('token');
  }

  // ── getUsuario ───────────────────────────────────────────────
  // Retorna los datos del usuario autenticado
  getUsuario(): any {
    const u = localStorage.getItem('usuario');
    return u ? JSON.parse(u) : null;
  }

  // ── estaAutenticado ──────────────────────────────────────────
  // True si hay token — para proteger rutas
  estaAutenticado(): boolean {
    return this.getToken() !== null;
  }

  // ── getHeaders ───────────────────────────────────────────────
  // Headers con JWT para endpoints protegidos con [Authorize]
  getHeaders(): HttpHeaders {
    return new HttpHeaders({
      'Content-Type':  'application/json',
      'Authorization': `Bearer ${this.getToken()}`
    });
  }
}
