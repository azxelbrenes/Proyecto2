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

    // Mismas constantes del tablero que usa el Motor —
    // deben coincidir para que los cálculos sean correctos
    private const int ANILLO_LONGITUD = 64;

    private static readonly Dictionary<string, int> OffsetColor = new()
    {
        { "ROJO",     0 },
        { "AZUL",    16 },
        { "VERDE",   32 },
        { "AMARILLO",48 }
    };

    private static readonly HashSet<int> CasillasSeguras = new(OffsetColor.Values);

    public BotServiceLN(IMotorParchisLN motor)
    {
        _motor = motor;
    }

    // ================================================================
    // ¿ES TURNO DE UN BOT?
    
    // El Hub llama a esto después de cada jugada humana para saber
    // si tiene que disparar el turno automático del bot.
    public bool EsTurnoDeBot(int parId, IUnidadTrabajoEF unidadTrabajo, out int jpIdBot)
    {
        jpIdBot = 0;

        try
        {
            // Consultamos el estado actual para saber de quién es el turno
            var estadoResp = _motor.ObtenerEstado(parId, unidadTrabajo, null!);
            if (!estadoResp.blnIndicadorTransaccion) return false;

            int turnoActual = estadoResp.ValorRetorno!.TurnoActualJpId;
            if (turnoActual <= 0) return false;

            // Buscamos ese jugador y verificamos si es bot
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
            // Usa exactamente el mismo método que un humano — así
            // las reglas se aplican igual y no hay ventaja injusta
            var resultadoDado = _motor.TirarDado(parId, jpIdBot, unidadTrabajo, mapper);

            if (!resultadoDado.blnIndicadorTransaccion)
                return resultadoDado;

            int valorDado = resultadoDado.ValorRetorno!.ValorDado;

            // Si el motor ya cedió el turno (sin movimientos posibles),
            // no hay nada más que hacer
            if (resultadoDado.ValorRetorno.SiguienteTurnoJpId != jpIdBot)
                return resultadoDado;

            // ── PASO 2: Elegir la mejor ficha para mover ─────────
            int? mejorFicha = ElegirMejorFicha(parId, jpIdBot, valorDado, unidadTrabajo);

            if (mejorFicha == null)
            {
                // No debería pasar (el motor ya validó que hay movimientos)
                // pero por seguridad devolvemos el resultado del dado
                return resultadoDado;
            }

            // ── PASO 3: Ejecutar el movimiento ───────────────────
            var resultadoMovimiento = _motor.MoverFicha(
                parId, jpIdBot, mejorFicha.Value, valorDado, unidadTrabajo, mapper
            );

            if (resultadoMovimiento.blnIndicadorTransaccion)
            {
                resultadoMovimiento.ValorRetorno!.Mensaje =
                    $"El bot sacó {valorDado} y movió la ficha {mejorFicha.Value}.";
            }

            return resultadoMovimiento;
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
    private int? ElegirMejorFicha(int parId, int jpIdBot, int valorDado, IUnidadTrabajoEF unidadTrabajo)
    {
        // Traemos las fichas del bot
        var fichasBot = unidadTrabajo.TEstadoFicha
            .Buscar(f => f.JpId == jpIdBot)
            .ValorRetorno?.ToList() ?? new List<EstadoFicha>();

        // Traemos TODAS las fichas de la partida (para detectar capturas)
        var todasLasFichas = unidadTrabajo.TEstadoFicha
            .Buscar(f => f.ParId == parId)
            .ValorRetorno?.ToList() ?? new List<EstadoFicha>();

        // Traemos los jugadores (para saber el color de cada ficha)
        var jugadores = unidadTrabajo.TJugadoresPartida
            .Buscar(j => j.ParId == parId)
            .ValorRetorno?.ToList() ?? new List<JugadoresPartidum>();

        string colorBot = jugadores.FirstOrDefault(j => j.JpId == jpIdBot)?.JpColorFicha ?? "ROJO";

        int? mejorFicha = null;
        int mejorPuntaje = -1;

        foreach (var ficha in fichasBot)
        {
            // Las fichas coronadas ya no se mueven
            if (ficha.EfEstadoFicha == "CORONADA") continue;

            // Calculamos a dónde iría esta ficha con este dado
            int? nuevaPosicion = SimularMovimiento(ficha.EfPosicion, valorDado);
            if (nuevaPosicion == null) continue; // Movimiento inválido, saltamos

            // Le asignamos un puntaje según qué tan buena es la jugada
            int puntaje = EvaluarJugada(
                ficha, nuevaPosicion.Value, colorBot,
                todasLasFichas, jugadores, jpIdBot
            );

            if (puntaje > mejorPuntaje)
            {
                mejorPuntaje = puntaje;
                mejorFicha = ficha.EfNumeroFicha;
            }
        }

        return mejorFicha;
    }

    // ── Simula el movimiento sin ejecutarlo ─────────────────────────
    // Misma lógica que CalcularNuevaPosicion del Motor
    private int? SimularMovimiento(int posicionActual, int dado)
    {
        // En casa: solo sale con 5
        if (posicionActual == 0)
            return dado == 5 ? 1 : null;

        int nueva = posicionActual + dado;

        // No se puede pasar de la meta — hay que caer exacto
        if (nueva > 68) return null;
        if (nueva == 68) return 69; // META

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
        // ── PRIORIDAD 2: Coronar una ficha → 90 puntos ───────────
        if (nuevaPosicion == 69)
            return 90;

        // ── PRIORIDAD 3: Sacar ficha de casa → 70 puntos ─────────
        if (ficha.EfPosicion == 0)
            return 70;

        // ── PRIORIDAD 1: Capturar ficha rival → 100 puntos ───────
        // Solo hay captura si la ficha queda en el anillo compartido
        if (nuevaPosicion is >= 1 and <= ANILLO_LONGITUD)
        {
            int casillaDestino = (OffsetColor[colorBot] + (nuevaPosicion - 1)) % ANILLO_LONGITUD;

            // Si es casilla segura no hay captura posible
            if (!CasillasSeguras.Contains(casillaDestino))
            {
                // Buscamos si hay una ficha rival sola en esa casilla
                var fichasRivalesAhi = todasLasFichas
                    .Where(f => f.JpId != jpIdBot && f.EfPosicion is >= 1 and <= ANILLO_LONGITUD)
                    .Where(f =>
                    {
                        var colorRival = jugadores.FirstOrDefault(j => j.JpId == f.JpId)?.JpColorFicha;
                        if (colorRival == null || !OffsetColor.ContainsKey(colorRival)) return false;

                        int celdaRival = (OffsetColor[colorRival] + (f.EfPosicion - 1)) % ANILLO_LONGITUD;
                        return celdaRival == casillaDestino;
                    })
                    .GroupBy(f => f.JpId)
                    .ToList();

                // Si hay un rival con 2+ fichas, está bloqueado — no podemos ir
                if (fichasRivalesAhi.Any(g => g.Count() >= 2))
                    return -1; // Movimiento imposible

                // Si hay exactamente un rival con 1 ficha → ¡captura!
                if (fichasRivalesAhi.Any())
                    return 100;
            }
        }

        // ── PRIORIDAD 4: Avanzar → 10 a 60 puntos ────────────────
        // Entre más cerca de la meta esté, más valioso es avanzarla.
        // La posición máxima antes de coronar es 68, así que
        // escalamos proporcionalmente hasta 60 puntos.
        int puntajeAvance = 10 + (int)((nuevaPosicion / 68.0) * 50);
        return puntajeAvance;
    }
}
