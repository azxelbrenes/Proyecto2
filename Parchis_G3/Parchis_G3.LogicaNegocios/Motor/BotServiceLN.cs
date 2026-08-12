using AutoMapper;
using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Motor;

public class BotServiceLN : IBotServiceLN
{
    private readonly IMotorParchisLN _motor;

    // ================================================================
    // GEOMETRÍA DEL TABLERO
    // ================================================================
    // Estos valores DEBEN coincidir con MotorParchisLN y con
    // tablero.config.ts del frontend. Estaban en 64/69 mientras el
    // motor ya usaba 52/57: el bot calculaba capturas contra casillas
    // que no existen e intentaba coronar en 69, que el motor rechaza.
    private const int ANILLO_LONGITUD = 52;

    private const int POS_CASA = 0;
    private const int POS_ANILLO_MIN = 1;
    private const int POS_ANILLO_MAX = 51;
    private const int META = 57;

    // El recorrido arranca en la salida del AZUL, igual que el motor
    private static readonly Dictionary<string, int> OffsetColor = new()
    {
        { "AZUL",     0  },
        { "VERDE",    13 },
        { "AMARILLO", 26 },
        { "ROJO",     39 }
    };

    // Las 4 salidas más las 4 estrellas intermedias
    private static readonly HashSet<int> CasillasSeguras = new()
    {
        0, 13, 26, 39,     // salidas
        8, 21, 34, 47      // estrellas
    };

    public BotServiceLN(IMotorParchisLN motor)
    {
        _motor = motor;
    }

    // ================================================================
    // ¿ES TURNO DE UN BOT?
    // ================================================================
    // El Hub llama a esto después de cada jugada humana para saber
    // si tiene que disparar el turno automático del bot.
    public bool EsTurnoDeBot(int parId, IUnidadTrabajoEF unidadTrabajo, out int jpIdBot)
    {
        jpIdBot = 0;

        try
        {
            var estadoResp = _motor.ObtenerEstado(parId, unidadTrabajo, null!);
            if (!estadoResp.blnIndicadorTransaccion) return false;

            int turnoActual = estadoResp.ValorRetorno!.TurnoActualJpId;
            if (turnoActual <= 0) return false;

            var jugadorResp = unidadTrabajo.TJugadoresPartida
                .ObtenerEntidad(j => j.JpId == turnoActual);

            if (!jugadorResp.blnIndicadorTransaccion) return false;

            if (jugadorResp.ValorRetorno!.JpEsBot)
            {
                jpIdBot = turnoActual;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    // ================================================================
    // JUGAR TURNO DEL BOT
    // ================================================================
    public Respuesta<ResultadoTurnoDTO> JugarTurnoBot(int parId, int jpIdBot, IUnidadTrabajoEF unidadTrabajo, IMapper mapper)
    {
        try
        {
            // ── PASO 1: El bot tira el dado ──────────────────────
            // Usa exactamente el mismo método que un humano — así las
            // reglas se aplican igual y no hay ventaja injusta.
            var resultadoDado = _motor.TirarDado(parId, jpIdBot, unidadTrabajo, mapper);

            if (!resultadoDado.blnIndicadorTransaccion)
                return resultadoDado;

            int valorDado = resultadoDado.ValorRetorno!.ValorDado;

            // Si el motor ya cedió el turno (sin movimientos posibles
            // o tercer 6 seguido), no hay nada más que hacer
            if (resultadoDado.ValorRetorno.SiguienteTurnoJpId != jpIdBot)
                return resultadoDado;

            // ── PASO 2: Ordenar las fichas por conveniencia ──────
            var candidatas = OrdenarFichasPorPuntaje(parId, jpIdBot, valorDado, unidadTrabajo);

            if (!candidatas.Any())
                return resultadoDado;

            // ── PASO 3: Ejecutar el movimiento ───────────────────
            // Se prueban en orden de preferencia. El motor puede
            // rechazar una jugada por una barrera en el camino, cosa
            // que el bot no evalúa: sin este reintento, el turno se
            // quedaba trabado y la partida no seguía.
            Respuesta<ResultadoTurnoDTO>? ultimoIntento = null;

            foreach (var numeroFicha in candidatas)
            {
                var intento = _motor.MoverFicha(
                    parId, jpIdBot, numeroFicha, valorDado, unidadTrabajo, mapper
                );

                if (intento.blnIndicadorTransaccion)
                {
                    intento.ValorRetorno!.Mensaje =
                        $"El bot sacó {valorDado} y movió la ficha {numeroFicha}.";
                    return intento;
                }

                ultimoIntento = intento;
            }

            // Ninguna jugada resultó válida: devolvemos la tirada para
            // que el Hub siga el flujo normal en vez de cortar.
            return resultadoDado;
        }
        catch (Exception ex)
        {
            return Respuesta<ResultadoTurnoDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // LA "INTELIGENCIA" DEL BOT
    // ================================================================
    // Evalúa cada ficha del bot, simula moverla con el dado actual,
    // le asigna un puntaje, y devuelve el número de la mejor ficha.
    private List<int> OrdenarFichasPorPuntaje(int parId, int jpIdBot, int valorDado, IUnidadTrabajoEF unidadTrabajo)
    {
        var fichasBot = unidadTrabajo.TEstadoFicha
            .Buscar(f => f.ParId == parId && f.JpId == jpIdBot)
            .ValorRetorno?.ToList() ?? new List<EstadoFicha>();

        // Todas las fichas de la partida, para detectar capturas
        var todasLasFichas = unidadTrabajo.TEstadoFicha
            .Buscar(f => f.ParId == parId)
            .ValorRetorno?.ToList() ?? new List<EstadoFicha>();

        var jugadores = unidadTrabajo.TJugadoresPartida
            .Buscar(j => j.ParId == parId)
            .ValorRetorno?.ToList() ?? new List<JugadoresPartidum>();

        string colorBot = jugadores.FirstOrDefault(j => j.JpId == jpIdBot)?.JpColorFicha ?? "AZUL";

        // Si el color no está mapeado no podemos calcular nada
        if (!OffsetColor.ContainsKey(colorBot))
            colorBot = "AZUL";

        var evaluadas = new List<(int NumeroFicha, int Puntaje)>();

        foreach (var ficha in fichasBot)
        {
            if (ficha.EfEstadoFicha == "CORONADA") continue;

            int? nuevaPosicion = SimularMovimiento(ficha.EfPosicion, valorDado);
            if (nuevaPosicion == null) continue;

            int puntaje = EvaluarJugada(
                ficha, nuevaPosicion.Value, colorBot,
                todasLasFichas, jugadores, jpIdBot
            );

            // -1 significa casilla bloqueada por el rival: el motor
            // rechazaría el movimiento, así que ni lo consideramos
            if (puntaje < 0) continue;

            evaluadas.Add((ficha.EfNumeroFicha, puntaje));
        }

        // De mejor a peor, para poder reintentar si la primera falla
        return evaluadas
            .OrderByDescending(e => e.Puntaje)
            .Select(e => e.NumeroFicha)
            .ToList();
    }

    // ── Simula el movimiento sin ejecutarlo ─────────────────────────
    // Misma lógica que CalcularNuevaPosicion del Motor.
    private int? SimularMovimiento(int posicionActual, int dado)
    {
        // De casa solo se sale con un 5
        if (posicionActual == POS_CASA)
            return dado == 5 ? POS_ANILLO_MIN : null;

        int nueva = posicionActual + dado;

        // Hay que caer exacto en el centro
        if (nueva > META) return null;

        return nueva;
    }

    // ── Sistema de puntajes: acá vive la "estrategia" del bot ───────
    private int EvaluarJugada(
        EstadoFicha ficha,
        int nuevaPosicion,
        string colorBot,
        List<EstadoFicha> todasLasFichas,
        List<JugadoresPartidum> jugadores,
        int jpIdBot)
    {
        // ── PRIORIDAD 1: Capturar ficha rival → 100 puntos ───────
        // Va primero: capturar vale más que coronar, porque manda una
        // ficha enemiga de vuelta a casa. Antes este bloque estaba
        // después de coronar y de salir de casa, así que el bot nunca
        // llegaba a evaluarlo si podía hacer cualquiera de las dos.
        if (nuevaPosicion >= POS_ANILLO_MIN && nuevaPosicion <= POS_ANILLO_MAX)
        {
            int casillaDestino = (OffsetColor[colorBot] + (nuevaPosicion - 1)) % ANILLO_LONGITUD;

            if (!CasillasSeguras.Contains(casillaDestino))
            {
                var fichasRivalesAhi = todasLasFichas
                    .Where(f => f.JpId != jpIdBot
                             && f.EfPosicion >= POS_ANILLO_MIN
                             && f.EfPosicion <= POS_ANILLO_MAX)
                    .Where(f =>
                    {
                        var colorRival = jugadores.FirstOrDefault(j => j.JpId == f.JpId)?.JpColorFicha;
                        if (colorRival == null || !OffsetColor.ContainsKey(colorRival)) return false;

                        int celdaRival = (OffsetColor[colorRival] + (f.EfPosicion - 1)) % ANILLO_LONGITUD;
                        return celdaRival == casillaDestino;
                    })
                    .GroupBy(f => f.JpId)
                    .ToList();

                // Rival con 2+ fichas = bloqueo, el motor lo rechaza
                if (fichasRivalesAhi.Any(g => g.Count() >= 2))
                    return -1;

                if (fichasRivalesAhi.Any())
                    return 100;
            }
        }

        // ── PRIORIDAD 2: Coronar una ficha → 90 puntos ───────────
        if (nuevaPosicion == META)
            return 90;

        // ── PRIORIDAD 3: Sacar ficha de casa → 70 puntos ─────────
        if (ficha.EfPosicion == POS_CASA)
            return 70;

        // ── PRIORIDAD 4: Entrar a la recta final → 65 puntos ─────
        // Una ficha en la recta ya no puede ser capturada
        if (nuevaPosicion > POS_ANILLO_MAX)
            return 65;

        // ── PRIORIDAD 5: Avanzar → 10 a 60 puntos ────────────────
        // Entre más cerca de la meta, más valioso es avanzar.
        // Se escala sobre META (57), no sobre 68 como antes.
        return 10 + (int)((nuevaPosicion / (double)META) * 50);
    }
}