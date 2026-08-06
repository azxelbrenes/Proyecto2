using System.Collections.Concurrent;
using AutoMapper;
using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class AbandonoLN : IAbandonoLN
{
    // Reglas de la HU-19
    private const double PORCENTAJE_PENALIZACION = 0.20;  // 20% de la entrada
    private const int ABANDONOS_PARA_BLOQUEO = 3;
    private const int MINUTOS_BLOQUEO = 30;

    // Regla de la HU-18
    private const int SEGUNDOS_RECONEXION = 60;

    // jpId -> momento en que se desconectó
    private readonly ConcurrentDictionary<int, DateTime> _desconexiones = new();

    // ================================================================
    // ABANDONAR PARTIDA (HU-19)
    // ================================================================
    public Respuesta<ResultadoAbandonoDTO> AbandonarPartida(int usuId, int parId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper)
    {
        try
        {
            // ── Validar que la partida esté en curso ─────────────
            var partidaResp = unidadTrabajo.TPartida.ObtenerEntidad(p => p.ParId == parId);
            if (!partidaResp.blnIndicadorTransaccion)
                return Respuesta<ResultadoAbandonoDTO>.Validacion("Partida no encontrada.");

            var partida = partidaResp.ValorRetorno!;

            if (partida.ParEstado != "EN_JUEGO")
                return Respuesta<ResultadoAbandonoDTO>.Validacion(
                    "Solo se puede abandonar una partida en curso.");

            // ── Buscar al jugador ────────────────────────────────
            var jugadorResp = unidadTrabajo.TJugadoresPartida
                .ObtenerEntidad(j => j.ParId == parId && j.UsuId == usuId);

            if (!jugadorResp.blnIndicadorTransaccion)
                return Respuesta<ResultadoAbandonoDTO>.Validacion("No estás en esta partida.");

            var jugador = jugadorResp.ValorRetorno!;
            var sala = unidadTrabajo.TSala.ObtenerEntidad(s => s.SalId == partida.SalId).ValorRetorno!;
            var usuario = unidadTrabajo.TUsuario.ObtenerEntidad(u => u.UsuId == usuId).ValorRetorno!;

            // ── Calcular penalización del 20% ────────────────────
            int penalizacion = (int)Math.Round(sala.SalCostoEntrada * PORCENTAJE_PENALIZACION);

            // Nunca dejamos el saldo en negativo
            if (usuario.UsuMonedasTotal < penalizacion)
                penalizacion = usuario.UsuMonedasTotal;

            usuario.UsuMonedasTotal -= penalizacion;

            // ── Contar abandonos consecutivos ────────────────────
            usuario.UsuAbandonosConsecutivos += 1;

            bool bloqueado = false;
            DateTime? fechaDesbloqueo = null;

            if (usuario.UsuAbandonosConsecutivos >= ABANDONOS_PARA_BLOQUEO)
            {
                usuario.UsuBloqueado = true;
                fechaDesbloqueo = DateTime.Now.AddMinutes(MINUTOS_BLOQUEO);
                usuario.UsuFechaDesbloqueo = fechaDesbloqueo;
                bloqueado = true;

                // Reiniciamos el contador tras aplicar el bloqueo
                usuario.UsuAbandonosConsecutivos = 0;
            }

            unidadTrabajo.TUsuario.Modificar(usuario);

            // ── Registrar la transacción de penalización ─────────
            unidadTrabajo.TTransaccion.Insertar(new Transaccione
            {
                UsuId = usuId,
                ParId = parId,
                TranTipo = "PENALIZACION",
                TranConcepto = $"Penalización por abandonar {sala.SalNombre}",
                TranMonto = -penalizacion,   // negativo = salida
                TranSaldoResultante = usuario.UsuMonedasTotal,
                TranFecha = DateTime.Now
            });

            // ── Registrar la derrota en el historial ─────────────
            // Esto afecta el % de victoria del jugador (HU-15)
            unidadTrabajo.THistorialPartida.Insertar(new HistorialPartida
            {
                UsuId = usuId,
                ParId = parId,
                SalId = sala.SalId,
                HpResultado = "ABANDONO",
                HpMonedasGanadas = -penalizacion,
                HpFecha = DateTime.Now
            });

            // ── Convertir al jugador en bot ──────────────────────
            // Así la partida continúa normalmente para los demás.
            // Sus fichas siguen en el tablero, pero ahora las mueve
            // el BotService automáticamente.
            jugador.JpEsBot = true;
            jugador.JpEstadoConexion = "BOT";
            unidadTrabajo.TJugadoresPartida.Modificar(jugador);

            unidadTrabajo.Completar();

            // Limpiamos el registro de desconexión si existía
            _desconexiones.TryRemove(jugador.JpId, out _);

            var resultado = new ResultadoAbandonoDTO
            {
                JpId = jugador.JpId,
                MonedasPenalizadas = penalizacion,
                SaldoNuevo = usuario.UsuMonedasTotal,
                AbandonosConsecutivos = usuario.UsuAbandonosConsecutivos,
                CuentaBloqueada = bloqueado,
                FechaDesbloqueo = fechaDesbloqueo,
                Mensaje = bloqueado
                    ? $"Abandonaste la partida. Perdiste {penalizacion} monedas y tu cuenta quedó bloqueada por {MINUTOS_BLOQUEO} minutos."
                    : $"Abandonaste la partida. Perdiste {penalizacion} monedas de penalización."
            };

            return Respuesta<ResultadoAbandonoDTO>.Exito(resultado, resultado.Mensaje);
        }
        catch (Exception ex)
        {
            return Respuesta<ResultadoAbandonoDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // MARCAR DESCONECTADO (HU-18)
    // ================================================================
    public Respuesta<bool> MarcarDesconectado(int parId, int jpId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            var jugadorResp = unidadTrabajo.TJugadoresPartida
                .ObtenerEntidad(j => j.JpId == jpId && j.ParId == parId);

            if (!jugadorResp.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("Jugador no encontrado.");

            var jugador = jugadorResp.ValorRetorno!;

            // Los bots no se "desconectan"
            if (jugador.JpEsBot)
                return Respuesta<bool>.Exito(false, "Es un bot, no aplica reconexión.");

            jugador.JpEstadoConexion = "RECONECTANDO";
            jugador.JpFechaDesconexion = DateTime.Now;
            unidadTrabajo.TJugadoresPartida.Modificar(jugador);
            unidadTrabajo.Completar();

            // Arrancamos el cronómetro de 60 segundos
            _desconexiones[jpId] = DateTime.Now;

            return Respuesta<bool>.Exito(true,
                $"Jugador desconectado. Tiene {SEGUNDOS_RECONEXION} segundos para volver.");
        }
        catch (Exception ex)
        {
            return Respuesta<bool>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // RECONECTAR (HU-18)
    // ================================================================
    public Respuesta<bool> Reconectar(int parId, int jpId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            var jugadorResp = unidadTrabajo.TJugadoresPartida
                .ObtenerEntidad(j => j.JpId == jpId && j.ParId == parId);

            if (!jugadorResp.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("Jugador no encontrado.");

            var jugador = jugadorResp.ValorRetorno!;

            // Si ya lo convirtieron en bot, se le venció el tiempo
            if (jugador.JpEsBot)
                return Respuesta<bool>.Validacion(
                    "Se te acabó el tiempo de reconexión, un bot tomó tu lugar.");

            jugador.JpEstadoConexion = "CONECTADO";
            jugador.JpFechaDesconexion = null;
            unidadTrabajo.TJugadoresPartida.Modificar(jugador);
            unidadTrabajo.Completar();

            // Limpiamos el cronómetro
            _desconexiones.TryRemove(jpId, out _);

            return Respuesta<bool>.Exito(true, "Reconectado exitosamente.");
        }
        catch (Exception ex)
        {
            return Respuesta<bool>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // VERIFICAR DESCONEXIONES VENCIDAS (HU-18)
    // ================================================================
    // El Hub llama a esto periódicamente. A quienes se les vencieron
    // los 60 segundos, se les aplica el abandono automático.
    public Respuesta<List<int>> VerificarDesconexionesVencidas(int parId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper)
    {
        try
        {
            var convertidos = new List<int>();

            var desconectados = unidadTrabajo.TJugadoresPartida
                .Buscar(j => j.ParId == parId && j.JpEstadoConexion == "RECONECTANDO")
                .ValorRetorno?.ToList() ?? new List<JugadoresPartidum>();

            foreach (var jugador in desconectados)
            {
                // ¿Ya se le vencieron los 60 segundos?
                if (SegundosRestantesReconexion(jugador.JpId) > 0)
                    continue;

                // Se le venció — aplicamos abandono automático
                if (jugador.UsuId.HasValue)
                {
                    AbandonarPartida(jugador.UsuId.Value, parId, unidadTrabajo, mapper);
                }
                else
                {
                    // Sin usuario asociado, solo lo convertimos en bot
                    jugador.JpEsBot = true;
                    jugador.JpEstadoConexion = "BOT";
                    unidadTrabajo.TJugadoresPartida.Modificar(jugador);
                    unidadTrabajo.Completar();
                }

                convertidos.Add(jugador.JpId);
                _desconexiones.TryRemove(jugador.JpId, out _);
            }

            return Respuesta<List<int>>.Exito(convertidos,
                convertidos.Any()
                    ? $"{convertidos.Count} jugador(es) reemplazado(s) por bots."
                    : "Sin desconexiones vencidas.");
        }
        catch (Exception ex)
        {
            return Respuesta<List<int>>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // SEGUNDOS RESTANTES PARA RECONECTARSE
    // ================================================================
    public int SegundosRestantesReconexion(int jpId)
    {
        if (!_desconexiones.TryGetValue(jpId, out DateTime desconexion))
            return 0;  // No está desconectado

        int transcurridos = (int)(DateTime.Now - desconexion).TotalSeconds;
        int restantes = SEGUNDOS_RECONEXION - transcurridos;

        return restantes > 0 ? restantes : 0;
    }
}
