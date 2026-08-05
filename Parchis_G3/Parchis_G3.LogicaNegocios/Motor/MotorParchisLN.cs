using System.Collections.Concurrent;
using AutoMapper;
using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Motor;

public class MotorParchisLN : IMotorParchisLN
{
    // ── Constantes del tablero ───────────────────────────────────
    private const int ANILLO_LONGITUD = 64;  // Casillas del anillo compartido
    private const int CASILLAS_POR_COLOR = 16; // 64 / 4 jugadores
    private const int META = 69; // Valor que representa "coronada"

    // Offset de salida de cada color dentro del anillo (0-63)
    private static readonly Dictionary<string, int> OffsetColor = new()
    {
        { "ROJO",     0 },
        { "AZUL",    16 },
        { "VERDE",   32 },
        { "AMARILLO",48 }
    };

    // Las casillas seguras son las 4 casillas de salida
    private static readonly HashSet<int> CasillasSeguras = new(OffsetColor.Values);

    // ── Estado en memoria (esto es lo que justifica el Singleton) ──
    // parId -> jpId de quien tiene el turno actual
    private readonly ConcurrentDictionary<int, int> _turnoActual = new();

    // jpId -> cuántos 5's consecutivos lleva ese jugador
    private readonly ConcurrentDictionary<int, int> _rachaCincos = new();

    // jpId -> valor de dado ya tirado, esperando a que el jugador
    // elija qué ficha mover (evita que el cliente invente su propio valor)
    private readonly ConcurrentDictionary<int, int> _dadoPendiente = new();

    private static readonly Random _random = Random.Shared;

    // ================================================================
    // INICIAR PARTIDA
    // ================================================================
    public Respuesta<bool> IniciarPartida(int parId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            // Traemos los 4 jugadores de esta partida (humanos o bots)
            var jugadoresResp = unidadTrabajo.TJugadoresPartida.Buscar(j => j.ParId == parId);
            if (!jugadoresResp.blnIndicadorTransaccion || !jugadoresResp.ValorRetorno!.Any())
                return Respuesta<bool>.Validacion("La partida no tiene jugadores asignados.");

            var jugadores = jugadoresResp.ValorRetorno!.OrderBy(j => j.JpPosicion).ToList();

            // Creamos las 4 fichas de cada jugador, todas empezando en casa
            foreach (var jugador in jugadores)
            {
                for (int numeroFicha = 1; numeroFicha <= 4; numeroFicha++)
                {
                    unidadTrabajo.TEstadoFicha.Insertar(new EstadoFicha
                    {
                        ParId = parId,
                        JpId = jugador.JpId,
                        EfNumeroFicha = numeroFicha,
                        EfPosicion = 0,
                        EfEstadoFicha = "EN_CASA",
                        EfUltimaActualizacion = DateTime.Now
                    });
                }
            }

            // Marcamos la partida como iniciada
            var partidaResp = unidadTrabajo.TPartida.ObtenerEntidad(p => p.ParId == parId);
            if (partidaResp.blnIndicadorTransaccion)
            {
                var partida = partidaResp.ValorRetorno!;
                partida.ParEstado = "EN_JUEGO";
                partida.ParFechaInicio = DateTime.Now;
                unidadTrabajo.TPartida.Modificar(partida);
            }

            unidadTrabajo.Completar();

            // El primer turno es del jugador en la posición 1
            _turnoActual[parId] = jugadores.First().JpId;

            return Respuesta<bool>.Exito(true, "Partida iniciada correctamente.");
        }
        catch (Exception ex)
        {
            return Respuesta<bool>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // TIRAR DADO
    // ================================================================
    public Respuesta<ResultadoTurnoDTO> TirarDado(int parId, int jpId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper)
    {
        try
        {
            // Validamos que sea el turno de este jugador
            if (!_turnoActual.TryGetValue(parId, out int jpIdTurno) || jpIdTurno != jpId)
                return Respuesta<ResultadoTurnoDTO>.Validacion("No es tu turno.");

            // Tiramos el dado — número aleatorio entre 1 y 6
            int valorDado = _random.Next(1, 7);

            // ── Regla especial: racha de 5's ─────────────────────
            // Si saca 5, sube la racha. Si no, la racha se reinicia.
            if (valorDado == 5)
                _rachaCincos.AddOrUpdate(jpId, 1, (_, actual) => actual + 1);
            else
                _rachaCincos[jpId] = 0;

            bool huboRachaPenalizacion = false;

            // Al tercer 5 consecutivo, una ficha en juego regresa a casa
            if (_rachaCincos.TryGetValue(jpId, out int racha) && racha >= 3)
            {
                var fichasEnJuego = unidadTrabajo.TEstadoFicha
                    .Buscar(f => f.JpId == jpId && f.EfEstadoFicha == "EN_JUEGO")
                    .ValorRetorno?.ToList();

                if (fichasEnJuego != null && fichasEnJuego.Any())
                {
                    // Elegimos una ficha al azar entre las que están en el tablero
                    var fichaPenalizada = fichasEnJuego[_random.Next(fichasEnJuego.Count)];
                    fichaPenalizada.EfPosicion = 0;
                    fichaPenalizada.EfEstadoFicha = "EN_CASA";
                    fichaPenalizada.EfUltimaActualizacion = DateTime.Now;
                    unidadTrabajo.TEstadoFicha.Modificar(fichaPenalizada);
                    unidadTrabajo.Completar();
                    huboRachaPenalizacion = true;
                }

                _rachaCincos[jpId] = 0; // Reiniciamos la racha tras la penalización
            }

            // ── Verificamos si el jugador puede mover con este dado ──
            var todasFichas = unidadTrabajo.TEstadoFicha.Buscar(f => f.JpId == jpId).ValorRetorno?.ToList() ?? new();
            bool puedeMover = TieneMovimientoPosible(todasFichas, valorDado);

            var resultado = new ResultadoTurnoDTO
            {
                ValorDado = valorDado,
                TurnoExtra = valorDado == 5,
                HuboCaptura = false,
                Mensaje = huboRachaPenalizacion
                    ? "¡Tres 5's seguidos! Una ficha regresó a casa."
                    : null
            };

            if (!puedeMover)
            {
                // No hay movimiento posible — el turno pasa automáticamente
                resultado.Mensaje = (resultado.Mensaje ?? "") + " Sin movimientos posibles, turno cedido.";
                int siguienteJp = SiguienteJugador(parId, jpId, unidadTrabajo);
                _turnoActual[parId] = siguienteJp;
                resultado.SiguienteTurnoJpId = siguienteJp;
                resultado.TurnoExtra = false;
            }
            else
            {
                // Guardamos el dado como "pendiente" — MoverFicha lo va a pedir
                _dadoPendiente[jpId] = valorDado;
                resultado.SiguienteTurnoJpId = jpId; // Sigue siendo su turno hasta que mueva
            }

            resultado.Estado = ConstruirEstadoPartida(parId, unidadTrabajo, mapper);

            return Respuesta<ResultadoTurnoDTO>.Exito(resultado, "Dado tirado.");
        }
        catch (Exception ex)
        {
            return Respuesta<ResultadoTurnoDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // MOVER FICHA
    // ================================================================
    public Respuesta<ResultadoTurnoDTO> MoverFicha(int parId, int jpId, int numeroFicha, int valorDado, IUnidadTrabajoEF unidadTrabajo, IMapper mapper)
    {
        try
        {
            // Validamos turno
            if (!_turnoActual.TryGetValue(parId, out int jpIdTurno) || jpIdTurno != jpId)
                return Respuesta<ResultadoTurnoDTO>.Validacion("No es tu turno.");

            // Validamos que el dado que manda el cliente sea el que
            // el servidor realmente tiró — nunca confiamos en el cliente
            if (!_dadoPendiente.TryGetValue(jpId, out int dadoReal) || dadoReal != valorDado)
                return Respuesta<ResultadoTurnoDTO>.Validacion("Dado inválido o ya utilizado.");

            // Buscamos la ficha específica que se quiere mover
            var fichaResp = unidadTrabajo.TEstadoFicha
                .ObtenerEntidad(f => f.JpId == jpId && f.EfNumeroFicha == numeroFicha);
            if (!fichaResp.blnIndicadorTransaccion)
                return Respuesta<ResultadoTurnoDTO>.Validacion("Ficha no encontrada.");

            var ficha = fichaResp.ValorRetorno!;

            // Buscamos el color del jugador (para saber su recorrido)
            var jugadorResp = unidadTrabajo.TJugadoresPartida.ObtenerEntidad(j => j.JpId == jpId);
            if (!jugadorResp.blnIndicadorTransaccion)
                return Respuesta<ResultadoTurnoDTO>.Validacion("Jugador no encontrado en la partida.");
            string colorJugador = jugadorResp.ValorRetorno!.JpColorFicha;

            // Calculamos la nueva posición según las reglas
            int? nuevaPosicion = CalcularNuevaPosicion(ficha.EfPosicion, valorDado);
            if (nuevaPosicion == null)
                return Respuesta<ResultadoTurnoDTO>.Validacion("Movimiento inválido: debés caer exacto o la ficha no puede salir de casa sin un 5.");

            bool huboCaptura = false;
            bool fichaCoronada = (nuevaPosicion == META);

            // Solo revisamos capturas/bloqueos si la ficha queda en el anillo compartido (1-64)
            if (nuevaPosicion is >= 1 and <= ANILLO_LONGITUD)
            {
                int casillaAnillo = (OffsetColor[colorJugador] + (nuevaPosicion.Value - 1)) % ANILLO_LONGITUD;
                bool esSegura = CasillasSeguras.Contains(casillaAnillo);

                if (!esSegura)
                {
                    // Buscamos TODAS las fichas de la partida que estén
                    // actualmente en esa misma casilla del anillo
                    var todasLasFichas = unidadTrabajo.TEstadoFicha.Buscar(f => f.ParId == parId).ValorRetorno!.ToList();
                    var jugadoresPartida = unidadTrabajo.TJugadoresPartida.Buscar(j => j.ParId == parId).ValorRetorno!.ToList();

                    var fichasEnCasilla = todasLasFichas
                        .Where(f => f.JpId != jpId && f.EfPosicion is >= 1 and <= ANILLO_LONGITUD)
                        .Where(f =>
                        {
                            var colorRival = jugadoresPartida.First(j => j.JpId == f.JpId).JpColorFicha;
                            int celdaRival = (OffsetColor[colorRival] + (f.EfPosicion - 1)) % ANILLO_LONGITUD;
                            return celdaRival == casillaAnillo;
                        })
                        .GroupBy(f => f.JpId)
                        .ToList();

                    // Si algún rival tiene 2+ fichas ahí, está BLOQUEADO — no podés aterrizar
                    if (fichasEnCasilla.Any(grupo => grupo.Count() >= 2))
                        return Respuesta<ResultadoTurnoDTO>.Validacion("Esa casilla está bloqueada por el rival.");

                    // Si hay rivales con 1 ficha sola, los capturamos (vuelven a casa)
                    foreach (var grupoRival in fichasEnCasilla)
                    {
                        foreach (var fichaRival in grupoRival)
                        {
                            fichaRival.EfPosicion = 0;
                            fichaRival.EfEstadoFicha = "EN_CASA";
                            fichaRival.EfUltimaActualizacion = DateTime.Now;
                            unidadTrabajo.TEstadoFicha.Modificar(fichaRival);
                            huboCaptura = true;
                        }
                    }
                }
            }

            // Aplicamos el movimiento a la ficha
            ficha.EfPosicion = nuevaPosicion.Value;
            ficha.EfEstadoFicha = fichaCoronada ? "CORONADA" : "EN_JUEGO";
            ficha.EfUltimaActualizacion = DateTime.Now;
            unidadTrabajo.TEstadoFicha.Modificar(ficha);

            // Registramos el turno en el historial (para reconexión y auditoría)
            int numeroTurno = (unidadTrabajo.TTurnoPartida.Buscar(t => t.ParId == parId).ValorRetorno?.Count() ?? 0) + 1;
            unidadTrabajo.TTurnoPartida.Insertar(new TurnosPartidum
            {
                ParId = parId,
                JpId = jpId,
                TurNumeroTurno = numeroTurno,
                TurResultadoDado = valorDado,
                TurFichaMovida = numeroFicha,
                TurPosicionAnterior = ficha.EfPosicion == nuevaPosicion ? 0 : (int?)null, // se recalcula abajo si hace falta
                TurPosicionNueva = nuevaPosicion,
                TurFueAutomatico = false,
                TurHuboCaptura = huboCaptura,
                TurFecha = DateTime.Now
            });

            unidadTrabajo.Completar();

            // Liberamos el dado pendiente — ya se usó
            _dadoPendiente.TryRemove(jpId, out _);

            var resultado = new ResultadoTurnoDTO
            {
                ValorDado = valorDado,
                HuboCaptura = huboCaptura,
                FichaCoronada = fichaCoronada,
                TurnoExtra = valorDado == 5
            };

            // ── ¿Ganó la partida? ─────────────────────────────────
            var fichasDelJugador = unidadTrabajo.TEstadoFicha.Buscar(f => f.JpId == jpId).ValorRetorno!.ToList();
            bool gano = fichasDelJugador.Count == 4 && fichasDelJugador.All(f => f.EfEstadoFicha == "CORONADA");

            if (gano)
            {
                FinalizarPartida(parId, jpId, unidadTrabajo);
                resultado.PartidaFinalizada = true;
                resultado.GanadorJpId = jpId;

                // Limpiamos el estado en memoria de esta partida — ya terminó
                _turnoActual.TryRemove(parId, out _);
                _rachaCincos.TryRemove(jpId, out _);
            }
            else
            {
                // Si sacó 5, sigue jugando (turno extra). Si no, pasa al siguiente.
                if (valorDado != 5)
                {
                    int siguienteJp = SiguienteJugador(parId, jpId, unidadTrabajo);
                    _turnoActual[parId] = siguienteJp;
                    resultado.SiguienteTurnoJpId = siguienteJp;
                }
                else
                {
                    resultado.SiguienteTurnoJpId = jpId;
                }
            }

            resultado.Estado = ConstruirEstadoPartida(parId, unidadTrabajo, mapper);

            return Respuesta<ResultadoTurnoDTO>.Exito(resultado, "Movimiento realizado.");
        }
        catch (Exception ex)
        {
            return Respuesta<ResultadoTurnoDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // OBTENER ESTADO ACTUAL (para reconexión o primera carga)
    // ================================================================
    public Respuesta<EstadoPartidaDTO> ObtenerEstado(int parId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper)
    {
        try
        {
            var estado = ConstruirEstadoPartida(parId, unidadTrabajo, mapper);
            return Respuesta<EstadoPartidaDTO>.Exito(estado, "Estado obtenido.");
        }
        catch (Exception ex)
        {
            return Respuesta<EstadoPartidaDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // HELPERS PRIVADOS
    // ================================================================

    // Calcula la nueva posición de una ficha dado su posición actual
    // y el valor del dado. Devuelve null si el movimiento no es válido.
    private int? CalcularNuevaPosicion(int posicionActual, int dado)
    {
        // Ficha en casa: solo sale con un 5 exacto
        if (posicionActual == 0)
            return dado == 5 ? 1 : null;

        int nueva = posicionActual + dado;

        // No se puede pasar de la meta — hay que caer EXACTO (regla clásica)
        if (nueva > 68)
            return null;

        // Llegó justo a la meta
        if (nueva == 68)
            return META;

        return nueva;
    }

    // Revisa si el jugador tiene AL MENOS un movimiento posible con este dado
    private bool TieneMovimientoPosible(List<EstadoFicha> fichas, int dado)
    {
        foreach (var ficha in fichas)
        {
            if (ficha.EfEstadoFicha == "CORONADA") continue;

            if (ficha.EfPosicion == 0 && dado == 5) return true;
            if (ficha.EfPosicion > 0 && ficha.EfPosicion + dado <= 68) return true;
        }
        return false;
    }

    // Determina quién sigue en el turno, ciclando por JpPosicion (1→2→3→4→1...)
    private int SiguienteJugador(int parId, int jpIdActual, IUnidadTrabajoEF unidadTrabajo)
    {
        var jugadores = unidadTrabajo.TJugadoresPartida
            .Buscar(j => j.ParId == parId)
            .ValorRetorno!
            .OrderBy(j => j.JpPosicion)
            .ToList();

        int indiceActual = jugadores.FindIndex(j => j.JpId == jpIdActual);
        int indiceSiguiente = (indiceActual + 1) % jugadores.Count;
        return jugadores[indiceSiguiente].JpId;
    }

    // Arma la "foto" completa del estado de la partida para mandar al frontend
    private EstadoPartidaDTO ConstruirEstadoPartida(int parId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper)
    {
        var partida = unidadTrabajo.TPartida.ObtenerEntidad(p => p.ParId == parId).ValorRetorno;
        var jugadores = unidadTrabajo.TJugadoresPartida.Buscar(j => j.ParId == parId).ValorRetorno!.OrderBy(j => j.JpPosicion).ToList();
        var fichas = unidadTrabajo.TEstadoFicha.Buscar(f => f.ParId == parId).ValorRetorno!.ToList();

        var dto = new EstadoPartidaDTO
        {
            ParId = parId,
            ParEstado = partida?.ParEstado ?? "DESCONOCIDO",
            TurnoActualJpId = _turnoActual.TryGetValue(parId, out int jp) ? jp : 0
        };

        foreach (var jugador in jugadores)
        {
            string nombre = "Bot";
            if (!jugador.JpEsBot && jugador.UsuId.HasValue)
            {
                var usuario = unidadTrabajo.TUsuario.ObtenerEntidad(u => u.UsuId == jugador.UsuId.Value).ValorRetorno;
                nombre = usuario?.UsuNombre ?? "Jugador";
            }

            dto.Jugadores.Add(new JugadorPartidaDTO
            {
                JpId = jugador.JpId,
                UsuId = jugador.UsuId,
                Nombre = nombre,
                EsBot = jugador.JpEsBot,
                Color = jugador.JpColorFicha,
                EsGanador = jugador.JpEsGanador
            });
        }

        foreach (var ficha in fichas)
        {
            var colorFicha = jugadores.FirstOrDefault(j => j.JpId == ficha.JpId)?.JpColorFicha ?? "";

            dto.Fichas.Add(new FichaDTO
            {
                JpId = ficha.JpId,
                NumeroFicha = ficha.EfNumeroFicha,
                Posicion = ficha.EfPosicion,
                Estado = ficha.EfEstadoFicha,
                Color = colorFicha
            });
        }

        return dto;
    }

    // Cierra la partida: reparte el premio, registra historial y transacciones
    private void FinalizarPartida(int parId, int jpIdGanador, IUnidadTrabajoEF unidadTrabajo)
    {
        var partida = unidadTrabajo.TPartida.ObtenerEntidad(p => p.ParId == parId).ValorRetorno!;
        var sala = unidadTrabajo.TSala.ObtenerEntidad(s => s.SalId == partida.SalId).ValorRetorno!;
        var jugadores = unidadTrabajo.TJugadoresPartida.Buscar(j => j.ParId == parId).ValorRetorno!.ToList();

        partida.ParEstado = "FINALIZADA";
        partida.ParFechaFin = DateTime.Now;
        partida.ParPremioTotal = sala.SalPremioBase;
        unidadTrabajo.TPartida.Modificar(partida);

        foreach (var jugador in jugadores)
        {
            // Marcamos quién ganó en la tabla de jugadores de la partida
            jugador.JpEsGanador = (jugador.JpId == jpIdGanador);
            unidadTrabajo.TJugadoresPartida.Modificar(jugador);

            // Los bots no tienen cuenta de usuario — no reciben nada
            if (jugador.JpEsBot || !jugador.UsuId.HasValue) continue;

            bool esGanador = jugador.JpId == jpIdGanador;
            var usuario = unidadTrabajo.TUsuario.ObtenerEntidad(u => u.UsuId == jugador.UsuId!.Value).ValorRetorno!;

            if (esGanador)
            {
                // Acreditamos el premio — este campo alimenta el ranking global
                usuario.UsuMonedasTotal += sala.SalPremioBase;
                usuario.UsuMonedasGanadasPartida += sala.SalPremioBase;
                unidadTrabajo.TUsuario.Modificar(usuario);

                unidadTrabajo.TTransaccion.Insertar(new Transaccione
                {
                    UsuId = usuario.UsuId,
                    ParId = parId,
                    TranTipo = "PREMIO_PARTIDA",
                    TranConcepto = $"Premio por ganar en {sala.SalNombre}",
                    TranMonto = sala.SalPremioBase,
                    TranSaldoResultante = usuario.UsuMonedasTotal,
                    TranFecha = DateTime.Now
                });
            }

            // Registramos el resultado en el historial (para estadísticas y % victoria)
            unidadTrabajo.THistorialPartida.Insertar(new HistorialPartida
            {
                UsuId = usuario.UsuId,
                ParId = parId,
                SalId = sala.SalId,
                HpResultado = esGanador ? "VICTORIA" : "DERROTA",
                HpMonedasGanadas = esGanador ? sala.SalPremioBase : 0,
                HpFecha = DateTime.Now
            });
        }

        unidadTrabajo.Completar();
    }
}
