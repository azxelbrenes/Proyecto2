import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth';

@Injectable({
  providedIn: 'root'
})
export class LogroService {

  private apiUrl = 'http://localhost:5051/api';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  // ── obtenerLogros ────────────────────────────────────────────
  // Trae los 8 logros con su progreso. El usuario sale del token.
  obtenerLogros(): Observable<any> {
    return this.http.get(`${this.apiUrl}/logro`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── reclamar ─────────────────────────────────────────────────
  // Acredita las monedas de todos los logros desbloqueados que
  // todavía no se cobraron.
  reclamar(): Observable<any> {
    return this.http.post(`${this.apiUrl}/logro/reclamar`, {}, {
      headers: this.authService.getHeaders()
    });
  }
}