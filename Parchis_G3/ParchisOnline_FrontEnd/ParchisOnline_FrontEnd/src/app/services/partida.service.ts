import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth';

@Injectable({
  providedIn: 'root'
})
export class PartidaService {

  private apiUrl = 'http://localhost:5051/api';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  // ================================================================
  // MATCHMAKING
  // ================================================================

  // ── buscarPartida ────────────────────────────────────────────
  // El jugador toca una sala en el home. El backend lo mete a una
  // partida existente o crea una nueva, le cobra la entrada y le
  // asigna color. Devuelve ParId y JpId — ambos necesarios para
  // conectarse al Hub de SignalR después.
  buscarPartida(salId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/matchmaking/buscar/${salId}`, {}, {
      headers: this.authService.getHeaders()
    });
  }

  // ── obtenerEstadoEspera ──────────────────────────────────────
  // Consulta quién se ha unido a la sala y cuántos segundos faltan
  // para que arranque. El frontend lo llama cada segundo mientras
  // muestra la pantalla de espera.
  obtenerEstadoEspera(parId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/matchmaking/espera/${parId}`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── verificarEIniciar ────────────────────────────────────────
  // Pide al backend revisar si ya pasaron los 30 segundos o si la
  // partida se llenó. Si corresponde, completa con bots e inicia
  // el juego. Devuelve true si la partida arrancó en esta llamada.
  verificarEIniciar(parId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/matchmaking/verificar/${parId}`, {}, {
      headers: this.authService.getHeaders()
    });
  }

  // ── abandonarEspera ──────────────────────────────────────────
  // El jugador sale ANTES de que arranque la partida.
  // Se le devuelven las monedas completas, sin penalización.
  abandonarEspera(parId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/matchmaking/abandonar/${parId}`, {
      headers: this.authService.getHeaders()
    });
  }

  // ================================================================
  // PARTIDA EN CURSO
  // ================================================================

  // ── obtenerEstado ────────────────────────────────────────────
  // Trae la foto completa del tablero: todas las fichas, sus
  // posiciones y de quién es el turno. Se usa al reconectarse.
  obtenerEstado(parId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/motor/estado/${parId}`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── obtenerPartidaActiva ─────────────────────────────────────
  // ¿El jugador tiene una partida en curso? Se usa al abrir la app
  // para ofrecerle volver a ella.
  obtenerPartidaActiva(): Observable<any> {
    return this.http.get(`${this.apiUrl}/partida/activa`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── abandonarPartida ─────────────────────────────────────────
  // El jugador se rinde DURANTE la partida.
  // Pierde el 20% de la entrada como penalización (HU-19).
  abandonarPartida(parId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/chat/abandonar/${parId}`, {}, {
      headers: this.authService.getHeaders()
    });
  }

  // ================================================================
  // CHAT
  // ================================================================

  // ── obtenerMensajesPredefinidos ──────────────────────────────
  // Los 4 mensajes rápidos: ¡Buena jugada!, ¡Eso te pasa!, etc.
  obtenerMensajesPredefinidos(): Observable<any> {
    return this.http.get(`${this.apiUrl}/chat/predefinidos`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── obtenerHistorialChat ─────────────────────────────────────
  // Todos los mensajes de la partida, ordenados por fecha
  obtenerHistorialChat(parId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/chat/historial/${parId}`, {
      headers: this.authService.getHeaders()
    });
  }
}
