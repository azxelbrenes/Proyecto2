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
    // ================================================================
    // GEOMETRÍA DEL TABLERO
    // ================================================================
    // Estos valores DEBEN coincidir con tablero.config.ts del frontend.
    // La relación es: anillo = 4 × (2L + 1) y grid = 2L + 3.
    // Con un grid de 15×15 → L = 6 → 52 casillas.
    private const int ANILLO_LONGITUD = 52;
    private const int OFFSET_ENTRE_COLORES = 13;   // 52 / 4

    private const int POS_CASA = 0;
    private const int POS_ANILLO_MIN = 1;
    private const int POS_ANILLO_MAX = 51;
    private const int POS_RECTA_MIN = 52;
    private const int POS_RECTA_MAX = 56;
    private const int META = 57;   // ficha coronada

    // ── Reglas de turno (RF-03) ──────────────────────────────────
    private const int DADO_TURNO_EXTRA = 6;
    private const int MAX_TURNOS_CONSECUTIVOS = 3;
    public const int SEGUNDOS_LIMITE_TURNO = 30;

    // El recorrido arranca en la salida del AZUL, igual que el frontend
    private static readonly Dictionary<string, int> OffsetColor = new()
    {
        { "AZUL",     0  },
        { "VERDE",    13 },
        { "AMARILLO", 26 },
        { "ROJO",     39 }
    };

    // Casillas de salida (seguras) más las 4 estrellas intermedias.
    // Coinciden con CASILLAS_SALIDA y CASILLAS_ESTRELLA del frontend.
    private static readonly HashSet<int> CasillasSeguras = new()
    {
        0, 13, 26, 39,     // salidas
        8, 21, 34, 47      // estrellas
    };

    // ── Estado en memoria (esto justifica el Singleton) ───────────
    private readonly ConcurrentDictionary<int, int> _turnoActual = new();

    // jpId -> cuántos 6's consecutivos lleva
    private readonly ConcurrentDictionary<int, int> _rachaSeises = new();

    // jpId -> dado ya tirado esperando que el jugador elija ficha
    private readonly ConcurrentDictionary<int, int> _dadoPendiente = new();

    // parId -> momento en que empezó el turno actual (RF-03)
    private readonly ConcurrentDictionary<int, DateTime> _inicioTurno = new();

    private static readonly Random _random = Random.Shared;

    // ================================================================
    // INICIAR PARTIDA
    // ================================================================
    public Respuesta<bool> IniciarPartida(int parId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            var jugadoresResp = unidadTrabajo.TJugadoresPartida.Buscar(j => j.ParId == parId);
            if (!jugadoresResp.blnIndicadorTransaccion || !jugadoresResp.ValorRetorno!.Any())
                return Respuesta<bool>.Validacion("La partida no tiene jugadores asignados.");

            var jugadores = jugadoresResp.ValorRetorno!.OrderBy(j => j.JpPosicion).ToList();

            foreach (var jugador in jugadores)
            {
                for (int numeroFicha = 1; numeroFicha <= 4; numeroFicha++)
                {
                    unidadTrabajo.TEstadoFicha.Insertar(new EstadoFicha
                    {
                        ParId = parId,
                        JpId = jugador.JpId,
                        EfNumeroFicha = numeroFicha,
                        EfPosicion = POS_CASA,
                        EfEstadoFicha = "EN_CASA",
                        EfUltimaActualizacion = DateTime.Now
                    });
                }
            }

            var partidaResp = unidadTrabajo.TPartida.ObtenerEntidad(p => p.ParId == parId);
            if (partidaResp.blnIndicadorTransaccion)
            {
                var partida = partidaResp.ValorRetorno!;
                partida.ParEstado = "EN_JUEGO";
                partida.ParFechaInicio = DateTime.Now;
                unidadTrabajo.TPartida.Modificar(partida);
            }

            unidadTrabajo.Completar();

            _turnoActual[parId] = jugadores.First().JpId;
            _inicioTurno[parId] = DateTime.Now;

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
            int jpIdTurno = ObtenerJpIdTurno(parId, unidadTrabajo);

            if (jpIdTurno != jpId)
                return Respuesta<ResultadoTurnoDTO>.Validacion("No es tu turno.");

            // Si ya tiró y todavía no movió, no puede volver a tirar
            if (_dadoPendiente.ContainsKey(jpId))
                return Respuesta<ResultadoTurnoDTO>.Validacion("Ya tiraste el dado. Elegí una ficha.");

            int valorDado = _random.Next(1, 7);

            var resultado = EvaluarTirada(parId, jpId, valorDado, unidadTrabajo);
            resultado.Estado = ConstruirEstadoPartida(parId, unidadTrabajo, mapper);

            return Respuesta<ResultadoTurnoDTO>.Exito(resultado, "Dado tirado.");
        }
        catch (Exception ex)
        {
            return Respuesta<ResultadoTurnoDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ── EvaluarTirada ────────────────────────────────────────────
    // Aplica las reglas del 6 y decide si el jugador puede mover o
    // si hay que ceder el turno. Se usa tanto en la tirada manual
    // como en la automática por vencimiento de los 30 segundos.
    private ResultadoTurnoDTO EvaluarTirada(int parId, int jpId, int valorDado, IUnidadTrabajoEF unidadTrabajo)
    {
        var mensajes = new List<string>();

        // ── RF-03: turno extra al sacar 6, máximo 3 seguidos ─────
        if (valorDado == DADO_TURNO_EXTRA)
            _rachaSeises.AddOrUpdate(jpId, 1, (_, actual) => actual + 1);
        else
            _rachaSeises[jpId] = 0;

        int racha = _rachaSeises.TryGetValue(jpId, out int r) ? r : 0;
        bool alcanzoTope = racha >= MAX_TURNOS_CONSECUTIVOS;

        // ── RF-03: al tercer 6, una ficha vuelve a casa ──────────
        // La rúbrica es explícita: solo si está dentro del tablero.
        // Una ficha que ya entró a la recta final NO se penaliza.
        if (alcanzoTope)
        {
            var penalizables = unidadTrabajo.TEstadoFicha
                .Buscar(f => f.JpId == jpId
                          && f.EfPosicion >= POS_ANILLO_MIN
                          && f.EfPosicion <= POS_ANILLO_MAX)
                .ValorRetorno?.ToList();

            if (penalizables != null && penalizables.Any())
            {
                var fichaPenalizada = penalizables[_random.Next(penalizables.Count)];
                fichaPenalizada.EfPosicion = POS_CASA;
                fichaPenalizada.EfEstadoFicha = "EN_CASA";
                fichaPenalizada.EfUltimaActualizacion = DateTime.Now;
                unidadTrabajo.TEstadoFicha.Modificar(fichaPenalizada);
                unidadTrabajo.Completar();

                mensajes.Add("¡Tres 6's seguidos! Una ficha regresó a casa.");
            }
            else
            {
                mensajes.Add("¡Tres 6's seguidos! No hay fichas en el tablero para penalizar.");
            }

            _rachaSeises[jpId] = 0;
        }

        var fichas = unidadTrabajo.TEstadoFicha.Buscar(f => f.JpId == jpId).ValorRetorno?.ToList() ?? new();
        bool puedeMover = ObtenerJugadasPosibles(fichas, valorDado).Any();

        var resultado = new ResultadoTurnoDTO
        {
            ValorDado = valorDado,
            HuboCaptura = false,
            TurnoExtra = valorDado == DADO_TURNO_EXTRA && !alcanzoTope
        };

        // Al tercer 6 se pierde el turno extra, aunque haya jugadas
        if (!puedeMover || alcanzoTope)
        {
            if (!puedeMover)
                mensajes.Add("Sin movimientos posibles, turno cedido.");

            _dadoPendiente.TryRemove(jpId, out _);

            int siguienteJp = SiguienteJugador(parId, jpId, unidadTrabajo);
            _turnoActual[parId] = siguienteJp;
            _inicioTurno[parId] = DateTime.Now;

            resultado.SiguienteTurnoJpId = siguienteJp;
            resultado.TurnoExtra = false;
        }
        else
        {
            _dadoPendiente[jpId] = valorDado;
            resultado.SiguienteTurnoJpId = jpId;
        }

        resultado.Mensaje = mensajes.Any() ? string.Join(" ", mensajes) : null;
        return resultado;
    }

    // ================================================================
    // MOVER FICHA
    // ================================================================
    public Respuesta<ResultadoTurnoDTO> MoverFicha(int parId, int jpId, int numeroFicha, int valorDado, IUnidadTrabajoEF unidadTrabajo, IMapper mapper)
    {
        try
        {
            int jpIdTurno = ObtenerJpIdTurno(parId, unidadTrabajo);
            if (jpIdTurno != jpId)
                return Respuesta<ResultadoTurnoDTO>.Validacion("No es tu turno.");

            // Nunca confiamos en el valor que manda el cliente
            if (!_dadoPendiente.TryGetValue(jpId, out int dadoReal) || dadoReal != valorDado)
                return Respuesta<ResultadoTurnoDTO>.Validacion("Dado inválido o ya utilizado.");

            return AplicarMovimiento(parId, jpId, numeroFicha, valorDado, false, unidadTrabajo, mapper);
        }
        catch (Exception ex)
        {
            return Respuesta<ResultadoTurnoDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ── AplicarMovimiento ────────────────────────────────────────
    // El movimiento en sí, sin validar el turno. Lo comparten el
    // movimiento manual y el automático por vencimiento del tiempo.
    private Respuesta<ResultadoTurnoDTO> AplicarMovimiento(
        int parId, int jpId, int numeroFicha, int valorDado, bool esAutomatico,
        IUnidadTrabajoEF unidadTrabajo, IMapper mapper)
    {
        var fichaResp = unidadTrabajo.TEstadoFicha
            .ObtenerEntidad(f => f.ParId == parId && f.JpId == jpId && f.EfNumeroFicha == numeroFicha);

        if (!fichaResp.blnIndicadorTransaccion)
            return Respuesta<ResultadoTurnoDTO>.Validacion("Ficha no encontrada.");

        var ficha = fichaResp.ValorRetorno!;

        // Guardamos la posición ANTES de tocarla. El código anterior
        // comparaba contra el valor ya sobrescrito, así que el
        // historial siempre quedaba con 0.
        int posicionAnterior = ficha.EfPosicion;

        var jugadorResp = unidadTrabajo.TJugadoresPartida.ObtenerEntidad(j => j.JpId == jpId);
        if (!jugadorResp.blnIndicadorTransaccion)
            return Respuesta<ResultadoTurnoDTO>.Validacion("Jugador no encontrado en la partida.");

        string colorJugador = jugadorResp.ValorRetorno!.JpColorFicha;

        if (!OffsetColor.ContainsKey(colorJugador))
            return Respuesta<ResultadoTurnoDTO>.Validacion($"Color de ficha desconocido: {colorJugador}");

        int? nuevaPosicion = CalcularNuevaPosicion(posicionAnterior, valorDado);
        if (nuevaPosicion == null)
            return Respuesta<ResultadoTurnoDTO>.Validacion("Movimiento inválido: hay que caer exacto, y de casa solo se sale con un 5.");

        bool huboCaptura = false;
        bool fichaCoronada = nuevaPosicion == META;

        // Capturas y bloqueos solo aplican en el anillo compartido
        if (nuevaPosicion >= POS_ANILLO_MIN && nuevaPosicion <= POS_ANILLO_MAX)
        {
            int casillaAnillo = IndiceAnillo(colorJugador, nuevaPosicion.Value);

            if (!CasillasSeguras.Contains(casillaAnillo))
            {
                var todasLasFichas = unidadTrabajo.TEstadoFicha.Buscar(f => f.ParId == parId).ValorRetorno!.ToList();
                var jugadoresPartida = unidadTrabajo.TJugadoresPartida.Buscar(j => j.ParId == parId).ValorRetorno!.ToList();

                var fichasEnCasilla = todasLasFichas
                    .Where(f => f.JpId != jpId
                             && f.EfPosicion >= POS_ANILLO_MIN
                             && f.EfPosicion <= POS_ANILLO_MAX)
                    .Where(f =>
                    {
                        var colorRival = jugadoresPartida.FirstOrDefault(j => j.JpId == f.JpId)?.JpColorFicha;
                        if (colorRival == null || !OffsetColor.ContainsKey(colorRival)) return false;
                        return IndiceAnillo(colorRival, f.EfPosicion) == casillaAnillo;
                    })
                    .GroupBy(f => f.JpId)
                    .ToList();

                // Dos fichas del mismo rival forman bloqueo: no se puede aterrizar
                if (fichasEnCasilla.Any(grupo => grupo.Count() >= 2))
                    return Respuesta<ResultadoTurnoDTO>.Validacion("Esa casilla está bloqueada por el rival.");

                foreach (var fichaRival in fichasEnCasilla.SelectMany(g => g))
                {
                    fichaRival.EfPosicion = POS_CASA;
                    fichaRival.EfEstadoFicha = "EN_CASA";
                    fichaRival.EfUltimaActualizacion = DateTime.Now;
                    unidadTrabajo.TEstadoFicha.Modificar(fichaRival);
                    huboCaptura = true;
                }
            }
        }

        ficha.EfPosicion = nuevaPosicion.Value;
        ficha.EfEstadoFicha = fichaCoronada ? "CORONADA" : "EN_JUEGO";
        ficha.EfUltimaActualizacion = DateTime.Now;
        unidadTrabajo.TEstadoFicha.Modificar(ficha);

        int numeroTurno = (unidadTrabajo.TTurnoPartida.Buscar(t => t.ParId == parId).ValorRetorno?.Count() ?? 0) + 1;

        unidadTrabajo.TTurnoPartida.Insertar(new TurnosPartidum
        {
            ParId = parId,
            JpId = jpId,
            TurNumeroTurno = numeroTurno,
            TurResultadoDado = valorDado,
            TurFichaMovida = numeroFicha,
            TurPosicionAnterior = posicionAnterior,
            TurPosicionNueva = nuevaPosicion,
            TurFueAutomatico = esAutomatico,
            TurHuboCaptura = huboCaptura,
            TurFecha = DateTime.Now
        });

        unidadTrabajo.Completar();

        _dadoPendiente.TryRemove(jpId, out _);

        var resultado = new ResultadoTurnoDTO
        {
            ValorDado = valorDado,
            HuboCaptura = huboCaptura,
            FichaCoronada = fichaCoronada,
            TurnoExtra = false
        };

        if (esAutomatico)
            resultado.Mensaje = "Se acabó el tiempo. El sistema movió por vos.";

        // ── ¿Ganó? ───────────────────────────────────────────────
        var fichasDelJugador = unidadTrabajo.TEstadoFicha
            .Buscar(f => f.ParId == parId && f.JpId == jpId).ValorRetorno!.ToList();

        bool gano = fichasDelJugador.Count == 4 && fichasDelJugador.All(f => f.EfEstadoFicha == "CORONADA");

        if (gano)
        {
            FinalizarPartida(parId, jpId, unidadTrabajo);
            resultado.PartidaFinalizada = true;
            resultado.GanadorJpId = jpId;
            LimpiarEstadoPartida(parId, unidadTrabajo);
        }
        else
        {
            // Turno extra solo si sacó 6 y no llegó al tope de 3
            int racha = _rachaSeises.TryGetValue(jpId, out int r) ? r : 0;
            bool conservaTurno = valorDado == DADO_TURNO_EXTRA && racha < MAX_TURNOS_CONSECUTIVOS;

            if (conservaTurno)
            {
                resultado.SiguienteTurnoJpId = jpId;
                resultado.TurnoExtra = true;
            }
            else
            {
                int siguienteJp = SiguienteJugador(parId, jpId, unidadTrabajo);
                _turnoActual[parId] = siguienteJp;
                resultado.SiguienteTurnoJpId = siguienteJp;
            }

            _inicioTurno[parId] = DateTime.Now;
        }

        resultado.Estado = ConstruirEstadoPartida(parId, unidadTrabajo, mapper);

        return Respuesta<ResultadoTurnoDTO>.Exito(resultado, "Movimiento realizado.");
    }

    // ================================================================
    // RF-03: TIEMPO LÍMITE DE 30 SEGUNDOS
    // ================================================================
    // Devuelve cuántos segundos le quedan al jugador actual. El Hub
    // usa esto para el temporizador visible y para saber cuándo
    // ejecutar el movimiento automático.
    public int SegundosRestantesTurno(int parId)
    {
        if (!_inicioTurno.TryGetValue(parId, out var inicio))
            return SEGUNDOS_LIMITE_TURNO;

        int transcurridos = (int)(DateTime.Now - inicio).TotalSeconds;
        return Math.Max(0, SEGUNDOS_LIMITE_TURNO - transcurridos);
    }

    public bool TurnoVencido(int parId) => SegundosRestantesTurno(parId) <= 0;

    // ── EjecutarMovimientoAutomatico ─────────────────────────────
    // RF-03: "Si el jugador no actúa en 30 segundos, el sistema
    // ejecuta el movimiento aleatoriamente."
    //
    // Cubre los dos casos: que no haya tirado todavía, y que haya
    // tirado pero no elegido ficha.
    public Respuesta<ResultadoTurnoDTO> EjecutarMovimientoAutomatico(
        int parId, int jpId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper)
    {
        try
        {
            int jpIdTurno = ObtenerJpIdTurno(parId, unidadTrabajo);
            if (jpIdTurno != jpId)
                return Respuesta<ResultadoTurnoDTO>.Validacion("El turno ya cambió.");

            // ── Caso 1: no llegó a tirar ─────────────────────────
            if (!_dadoPendiente.TryGetValue(jpId, out int valorDado))
            {
                valorDado = _random.Next(1, 7);
                var tirada = EvaluarTirada(parId, jpId, valorDado, unidadTrabajo);

                // Si la tirada ya cedió el turno, no hay nada que mover
                if (tirada.SiguienteTurnoJpId != jpId)
                {
                    tirada.Mensaje = "Se acabó el tiempo. " + (tirada.Mensaje ?? "Turno cedido.");
                    tirada.Estado = ConstruirEstadoPartida(parId, unidadTrabajo, mapper);
                    return Respuesta<ResultadoTurnoDTO>.Exito(tirada, "Turno automático.");
                }
            }

            // ── Caso 2: tiró pero no eligió ficha ────────────────
            var fichas = unidadTrabajo.TEstadoFicha
                .Buscar(f => f.ParId == parId && f.JpId == jpId).ValorRetorno!.ToList();

            var jugadas = ObtenerJugadasPosibles(fichas, valorDado);

            if (!jugadas.Any())
            {
                // Red de seguridad: si no hay jugadas, cedemos el turno
                _dadoPendiente.TryRemove(jpId, out _);
                int siguienteJp = SiguienteJugador(parId, jpId, unidadTrabajo);
                _turnoActual[parId] = siguienteJp;
                _inicioTurno[parId] = DateTime.Now;

                var cedido = new ResultadoTurnoDTO
                {
                    ValorDado = valorDado,
                    SiguienteTurnoJpId = siguienteJp,
                    Mensaje = "Sin movimientos posibles, turno cedido.",
                    Estado = ConstruirEstadoPartida(parId, unidadTrabajo, mapper)
                };

                return Respuesta<ResultadoTurnoDTO>.Exito(cedido, "Turno cedido.");
            }

            // Elegimos una jugada al azar, como pide la rúbrica
            int fichaElegida = jugadas[_random.Next(jugadas.Count)];

            return AplicarMovimiento(parId, jpId, fichaElegida, valorDado, true, unidadTrabajo, mapper);
        }
        catch (Exception ex)
        {
            return Respuesta<ResultadoTurnoDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // OBTENER ESTADO ACTUAL
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

    // Índice físico dentro del anillo compartido
    private static int IndiceAnillo(string color, int posicionRelativa)
        => (OffsetColor[color] + posicionRelativa - 1) % ANILLO_LONGITUD;

    // ── ObtenerJpIdTurno ─────────────────────────────────────────
    // El turno vive en memoria, así que un reinicio del servidor lo
    // borraba y la partida quedaba muerta: todos recibían "No es tu
    // turno" para siempre. Ahora lo reconstruimos desde la BD.
    private int ObtenerJpIdTurno(int parId, IUnidadTrabajoEF unidadTrabajo)
    {
        if (_turnoActual.TryGetValue(parId, out int jpIdTurno))
            return jpIdTurno;

        var jugadores = unidadTrabajo.TJugadoresPartida
            .Buscar(j => j.ParId == parId).ValorRetorno?
            .OrderBy(j => j.JpPosicion).ToList();

        if (jugadores == null || !jugadores.Any())
            return 0;

        var ultimoTurno = unidadTrabajo.TTurnoPartida
            .Buscar(t => t.ParId == parId).ValorRetorno?
            .OrderByDescending(t => t.TurNumeroTurno).FirstOrDefault();

        int recuperado = ultimoTurno == null
            ? jugadores.First().JpId
            : SiguienteJugador(parId, ultimoTurno.JpId, unidadTrabajo);

        _turnoActual[parId] = recuperado;
        _inicioTurno[parId] = DateTime.Now;

        return recuperado;
    }

    private int? CalcularNuevaPosicion(int posicionActual, int dado)
    {
        // De casa solo se sale con un 5 exacto
        if (posicionActual == POS_CASA)
            return dado == 5 ? POS_ANILLO_MIN : null;

        int nueva = posicionActual + dado;

        // Hay que caer exacto en el centro
        if (nueva > META) return null;

        return nueva;
    }

    // Devuelve los números de ficha que se pueden mover con este dado
    private List<int> ObtenerJugadasPosibles(List<EstadoFicha> fichas, int dado)
    {
        var jugadas = new List<int>();

        foreach (var ficha in fichas)
        {
            if (ficha.EfEstadoFicha == "CORONADA") continue;

            if (CalcularNuevaPosicion(ficha.EfPosicion, dado) != null)
                jugadas.Add(ficha.EfNumeroFicha);
        }

        return jugadas;
    }

    private int SiguienteJugador(int parId, int jpIdActual, IUnidadTrabajoEF unidadTrabajo)
    {
        var jugadores = unidadTrabajo.TJugadoresPartida
            .Buscar(j => j.ParId == parId)
            .ValorRetorno!
            .OrderBy(j => j.JpPosicion)
            .ToList();

        int indiceActual = jugadores.FindIndex(j => j.JpId == jpIdActual);
        if (indiceActual < 0) return jugadores.First().JpId;

        int indiceSiguiente = (indiceActual + 1) % jugadores.Count;
        return jugadores[indiceSiguiente].JpId;
    }

    // Libera todo el estado en memoria de una partida terminada.
    // Antes solo se borraba la racha del ganador y las de los otros
    // tres jugadores quedaban acumulándose en el diccionario.
    private void LimpiarEstadoPartida(int parId, IUnidadTrabajoEF unidadTrabajo)
    {
        _turnoActual.TryRemove(parId, out _);
        _inicioTurno.TryRemove(parId, out _);

        var jugadores = unidadTrabajo.TJugadoresPartida
            .Buscar(j => j.ParId == parId).ValorRetorno?.ToList();

        if (jugadores == null) return;

        foreach (var jugador in jugadores)
        {
            _rachaSeises.TryRemove(jugador.JpId, out _);
            _dadoPendiente.TryRemove(jugador.JpId, out _);
        }
    }

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
            string nombre = $"Bot {jugador.JpPosicion}";

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
            jugador.JpEsGanador = (jugador.JpId == jpIdGanador);
            unidadTrabajo.TJugadoresPartida.Modificar(jugador);

            if (jugador.JpEsBot || !jugador.UsuId.HasValue) continue;

            bool esGanador = jugador.JpId == jpIdGanador;
            var usuario = unidadTrabajo.TUsuario.ObtenerEntidad(u => u.UsuId == jugador.UsuId!.Value).ValorRetorno!;

            if (esGanador)
            {
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