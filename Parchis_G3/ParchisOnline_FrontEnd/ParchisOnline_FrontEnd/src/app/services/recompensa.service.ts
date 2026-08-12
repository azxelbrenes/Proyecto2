import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth';

@Injectable({
  providedIn: 'root'
})
export class RecompensaService {

  private apiUrl = 'http://localhost:5051/api';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  // ── obtenerEstado ────────────────────────────────────────────
  // ¿Puede reclamar hoy? ¿Qué racha lleva? ¿Cuánto ganaría?
  // El home lo consulta al abrir para decidir si muestra el modal.
  obtenerEstado(): Observable<any> {
    return this.http.get(`${this.apiUrl}/recompensa/estado`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── reclamar ─────────────────────────────────────────────────
  // Acredita las monedas del día y avanza la racha.
  //
  // El backend valida que no haya reclamado ya hoy — sin esa
  // validación alguien podría llamar 100 veces al endpoint y
  // llenarse de monedas gratis.
  reclamar(): Observable<any> {
    return this.http.post(`${this.apiUrl}/recompensa/reclamar`, {}, {
      headers: this.authService.getHeaders()
    });
  }
}
