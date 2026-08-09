import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth';

@Injectable({
  providedIn: 'root'
})
export class RankingService {

  private apiUrl = 'http://localhost:5051/api';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  // ── obtenerRanking ───────────────────────────────────────────
  // Trae el top de jugadores. El backend incluye la posición del
  // usuario actual aunque esté fuera del top, para que siempre
  // pueda verse a sí mismo.
  obtenerRanking(top: number = 50): Observable<any> {
    return this.http.get(`${this.apiUrl}/ranking?top=${top}`, {
      headers: this.authService.getHeaders()
    });
  }
}
