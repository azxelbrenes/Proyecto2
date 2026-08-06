using Microsoft.AspNetCore.SignalR;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using AutoMapper;

namespace Parchis_G3.API.Hubs;

public class PartidaHub : Hub
{
    private readonly IMotorParchisLN _motor;
    private readonly IBotServiceLN _botService;
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;

    // Delay entre jugadas de bots para que se vea natural.
    // Sin esto, 3 bots jugarían instantáneamente y el humano
    // no entendería qué pasó en el tablero.
    private const int DELAY_BOT_MS = 1500;

    public PartidaHub(
        IMotorParchisLN motor,
        IBotServiceLN botService,
        IUnidadTrabajoEF unidadTrabajo,
        IMapper mapper)
    {
        _motor = motor;
        _botService = botService;
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
    }

    private static string GrupoPartida(int parId) => $"partida-{parId}";

    // ── UnirseAPartida ────────────────────────────────────────────
    public async Task UnirseAPartida(int parId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GrupoPartida(parId));

        var estado = _motor.ObtenerEstado(parId, _unidadTrabajo, _mapper);
        await Clients.Caller.SendAsync("EstadoActualizado", estado.ValorRetorno);

        // Si al conectarse resulta que es turno de un bot (porque
        // la partida ya estaba corriendo), lo disparamos
        await ProcesarTurnosDeBots(parId);
    }

    // ── SalirDePartida ────────────────────────────────────────────
    public async Task SalirDePartida(int parId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoPartida(parId));
    }

    // ── TirarDado ────────────────────────────────────────────────
    public async Task TirarDado(int parId, int jpId)
    {
        var resultado = _motor.TirarDado(parId, jpId, _unidadTrabajo, _mapper);

        if (!resultado.blnIndicadorTransaccion)
        {
            await Clients.Caller.SendAsync("Error", resultado.strMensajeRespuesta);
            return;
        }

        await Clients.Group(GrupoPartida(parId)).SendAsync("DadoTirado", resultado.ValorRetorno);

        // Si el motor cedió el turno automáticamente (sin movimientos
        // posibles), puede que ahora le toque a un bot
        if (resultado.ValorRetorno!.SiguienteTurnoJpId != jpId)
        {
            await ProcesarTurnosDeBots(parId);
        }
    }

    // ── MoverFicha ───────────────────────────────────────────────
    public async Task MoverFicha(int parId, int jpId, int numeroFicha, int valorDado)
    {
        var resultado = _motor.MoverFicha(parId, jpId, numeroFicha, valorDado, _unidadTrabajo, _mapper);

        if (!resultado.blnIndicadorTransaccion)
        {
            await Clients.Caller.SendAsync("Error", resultado.strMensajeRespuesta);
            return;
        }

        await Clients.Group(GrupoPartida(parId)).SendAsync("FichaMovida", resultado.ValorRetorno);

        // Si terminó la partida, avisamos y no seguimos con bots
        if (resultado.ValorRetorno!.PartidaFinalizada)
        {
            await Clients.Group(GrupoPartida(parId)).SendAsync("PartidaFinalizada", resultado.ValorRetorno);
            return;
        }

        // Después de mover, revisamos si le toca a un bot
        await ProcesarTurnosDeBots(parId);
    }

    // ================================================================
    // PROCESAR TURNOS DE BOTS (el corazón del automatismo)
    // ================================================================
    // Este método revisa si el turno actual es de un bot. Si lo es,
    // lo juega automáticamente y vuelve a revisar — porque después
    // del bot podría venir OTRO bot. Se repite hasta que le toque
    // a un humano o termine la partida.
    //
    // El límite de 20 iteraciones es una protección contra bucles
    // infinitos por si algo sale mal en la lógica de turnos.
    private async Task ProcesarTurnosDeBots(int parId)
    {
        int iteraciones = 0;

        while (iteraciones < 20)
        {
            iteraciones++;

            // ¿Le toca a un bot?
            if (!_botService.EsTurnoDeBot(parId, _unidadTrabajo, out int jpIdBot))
                break; // Le toca a un humano, salimos del bucle

            // Pausa para que los jugadores humanos vean lo que pasa
            await Task.Delay(DELAY_BOT_MS);

            // El bot juega su turno completo (tira dado + mueve ficha)
            var resultado = _botService.JugarTurnoBot(parId, jpIdBot, _unidadTrabajo, _mapper);

            if (!resultado.blnIndicadorTransaccion)
            {
                // Si el bot falló, avisamos al grupo y cortamos
                // para no quedarnos en bucle infinito
                await Clients.Group(GrupoPartida(parId))
                    .SendAsync("Error", $"Error en turno del bot: {resultado.strMensajeRespuesta}");
                break;
            }

            // Notificamos a todos lo que hizo el bot
            await Clients.Group(GrupoPartida(parId)).SendAsync("FichaMovida", resultado.ValorRetorno);

            // ¿El bot ganó la partida?
            if (resultado.ValorRetorno!.PartidaFinalizada)
            {
                await Clients.Group(GrupoPartida(parId)).SendAsync("PartidaFinalizada", resultado.ValorRetorno);
                break;
            }

            // Si el bot sacó 5, tiene turno extra — el bucle continúa
            // y vuelve a jugar. Si no, el turno pasó al siguiente
            // jugador y el bucle revisa si ese también es bot.
        }
    }

    // ── OnDisconnectedAsync ──────────────────────────────────────
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
