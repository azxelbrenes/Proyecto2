import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth';

@Injectable({
  providedIn: 'root'
})
export class PagoService {

  private apiUrl = 'http://localhost:5051/api';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  // ── obtenerPaquetes ──────────────────────────────────────────
  // Los 4 paquetes de monedas con sus precios en USD y colones.
  // Vienen del backend (no están hardcodeados en el frontend)
  // para que el precio real siempre lo defina el servidor.
  obtenerPaquetes(): Observable<any> {
    return this.http.get(`${this.apiUrl}/pago/paquetes`, {
      headers: this.authService.getHeaders()
    });
  }

  // ── crearOrden ───────────────────────────────────────────────
  // PASO 1: el backend le pide a PayPal crear una orden.
  // Devuelve el OrdenId y la UrlAprobacion donde el usuario paga.
  //
  // Solo mandamos el PaqueteId — nunca el precio. Así nadie puede
  // manipular cuánto va a pagar desde el cliente.
  crearOrden(paqueteId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/pago/crear-orden`, {
      PaqueteId: paqueteId
    }, {
      headers: this.authService.getHeaders()
    });
  }

  // ── capturarPago ─────────────────────────────────────────────
  // PASO 2: tras aprobar el usuario en PayPal, el backend verifica
  // que el pago esté en estado COMPLETED y acredita las monedas.
  //
  // El backend también valida que esa orden no haya sido procesada
  // antes, para evitar que alguien acredite dos veces el mismo pago.
  capturarPago(ordenId: string, paqueteId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/pago/capturar`, {
      OrdenId:   ordenId,
      PaqueteId: paqueteId
    }, {
      headers: this.authService.getHeaders()
    });
  }
}
