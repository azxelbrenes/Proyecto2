// ================================================================
// sala.service.ts — Servicio de Salas de Apuesta
// ================================================================
// ¿QUÉ HACE ESTE SERVICIO?
// Conecta el frontend con los endpoints de SalaController.
// Trae la lista de salas y maneja el proceso de unirse a una.
// ================================================================

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth';

@Injectable({
  providedIn: 'root'
})
export class SalaService {

  private apiUrl = 'http://localhost:5051/api';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  // ── listarSalas ──────────────────────────────────────────────
  // Trae las 5 salas de apuesta desde GET /api/sala
  // Requiere JWT porque el controller tiene [Authorize]
  listarSalas(): Observable<any> {
    return this.http.get(`${this.apiUrl}/sala`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── buscarSala ───────────────────────────────────────────────
  // Trae una sala específica por su ID
  buscarSala(id: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/sala/${id}`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── unirseASala ──────────────────────────────────────────────
  // Une al jugador a la sala — el backend valida el saldo
  // y descuenta las monedas automáticamente
  unirseASala(salId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/sala/unirse`, {
      SalId: salId
    }, {
      headers: this.authService.getHeaders()
    });
  }
}
