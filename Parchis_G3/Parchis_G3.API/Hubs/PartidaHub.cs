using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using AutoMapper;

namespace Parchis_G3.API.Hubs;

public class PartidaHub : Hub
{
    private readonly IMotorParchisLN _motor;
    private readonly IBotServiceLN _botService;
    private readonly IChatLN _chatLN;
    private readonly IAbandonoLN _abandonoLN;
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;

    // Delay entre jugadas de bots para que se vea natural
    private const int DELAY_BOT_MS = 1500;

    // ConnectionId -> (parId, jpId)
    // Estático porque el Hub se instancia de nuevo en cada llamada
    private static readonly ConcurrentDictionary<string, (int parId, int jpId)> _conexiones = new();

    public PartidaHub(
        IMotorParchisLN motor,
        IBotServiceLN botService,
        IChatLN chatLN,
        IAbandonoLN abandonoLN,
        IUnidadTrabajoEF unidadTrabajo,
        IMapper mapper)
    {
        _motor = motor;
        _botService = botService;
        _chatLN = chatLN;
        _abandonoLN = abandonoLN;
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
    }

    private static string GrupoPartida(int parId) => $"partida-{parId}";

    // ================================================================
    // UNIRSE A LA PARTIDA
    // ================================================================
    public async Task UnirseAPartida(int parId, int jpId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GrupoPartida(parId));

        // Registramos esta conexión para saber quién es al desconectarse
        _conexiones[Context.ConnectionId] = (parId, jpId);

        // Si venía de una desconexión, lo reconectamos (HU-18)
        var reconexion = _abandonoLN.Reconectar(parId, jpId, _unidadTrabajo);

        if (reconexion.blnIndicadorTransaccion && reconexion.ValorRetorno)
        {
            // Avisamos a los demás que el jugador volvió
            await Clients.OthersInGroup(GrupoPartida(parId))
                .SendAsync("JugadorReconectado", new { jpId });
        }

        // Le mandamos el estado actual del tablero
        var estado = _motor.ObtenerEstado(parId, _unidadTrabajo, _mapper);
        await Clients.Caller.SendAsync("EstadoActualizado", estado.ValorRetorno);

        // Y el historial del chat
        var historial = _chatLN.ObtenerHistorial(parId, _unidadTrabajo);
        await Clients.Caller.SendAsync("HistorialChat", historial.ValorRetorno);

        // Por si al conectarse ya era turno de un bot
        await ProcesarTurnosDeBots(parId);
    }

    // ================================================================
    // SALIR DE LA PARTIDA (salida normal, no desconexión)
    // ================================================================
    public async Task SalirDePartida(int parId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoPartida(parId));
        _conexiones.TryRemove(Context.ConnectionId, out _);
    }

    // ================================================================
    // TIRAR DADO
    // ================================================================
    public async Task TirarDado(int parId, int jpId)
    {
        var resultado = _motor.TirarDado(parId, jpId, _unidadTrabajo, _mapper);

        if (!resultado.blnIndicadorTransaccion)
        {
            await Clients.Caller.SendAsync("Error", resultado.strMensajeRespuesta);
            return;
        }

        await Clients.Group(GrupoPartida(parId)).SendAsync("DadoTirado", resultado.ValorRetorno);

        // Si el motor cedió el turno (sin movimientos), puede tocar a un bot
        if (resultado.ValorRetorno!.SiguienteTurnoJpId != jpId)
        {
            await ProcesarTurnosDeBots(parId);
        }
    }

    // ================================================================
    // MOVER FICHA
    // ================================================================
    public async Task MoverFicha(int parId, int jpId, int numeroFicha, int valorDado)
    {
        var resultado = _motor.MoverFicha(parId, jpId, numeroFicha, valorDado, _unidadTrabajo, _mapper);

        if (!resultado.blnIndicadorTransaccion)
        {
            await Clients.Caller.SendAsync("Error", resultado.strMensajeRespuesta);
            return;
        }

        await Clients.Group(GrupoPartida(parId)).SendAsync("FichaMovida", resultado.ValorRetorno);

        if (resultado.ValorRetorno!.PartidaFinalizada)
        {
            await Clients.Group(GrupoPartida(parId)).SendAsync("PartidaFinalizada", resultado.ValorRetorno);
            return;
        }

        await ProcesarTurnosDeBots(parId);
    }

    // ================================================================
    // ENVIAR MENSAJE DE CHAT (HU-14)
    // ================================================================
    // Valida el cooldown de 5 segundos, guarda en BD y retransmite
    // el mensaje a los 4 jugadores de la partida.
    public async Task EnviarMensaje(int parId, int jpId, string contenido, bool esPredefinido)
    {
        var resultado = _chatLN.EnviarMensaje(parId, jpId, contenido, esPredefinido, _unidadTrabajo);

        if (!resultado.blnIndicadorTransaccion)
        {
            // El error (cooldown, mensaje vacío) solo lo ve quien lo mandó
            await Clients.Caller.SendAsync("Error", resultado.strMensajeRespuesta);
            return;
        }

        // El mensaje sí lo ven todos
        await Clients.Group(GrupoPartida(parId))
            .SendAsync("MensajeRecibido", resultado.ValorRetorno);
    }

    // ================================================================
    // ABANDONAR PARTIDA (HU-19)
    // ================================================================
    // El jugador se rinde. Pierde 20% de la entrada, se convierte
    // en bot y la partida continúa para los demás.
    public async Task AbandonarPartida(int parId, int usuId)
    {
        var resultado = _abandonoLN.AbandonarPartida(usuId, parId, _unidadTrabajo, _mapper);

        if (!resultado.blnIndicadorTransaccion)
        {
            await Clients.Caller.SendAsync("Error", resultado.strMensajeRespuesta);
            return;
        }

        // A quien abandonó le mandamos el detalle de su penalización
        await Clients.Caller.SendAsync("AbandonoConfirmado", resultado.ValorRetorno);

        // A los demás les avisamos que ahora ese puesto lo juega un bot
        await Clients.OthersInGroup(GrupoPartida(parId)).SendAsync("JugadorAbandono", new
        {
            jpId = resultado.ValorRetorno!.JpId,
            mensaje = "Un jugador abandonó. Un bot tomó su lugar."
        });

        // Sacamos su conexión del grupo
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoPartida(parId));
        _conexiones.TryRemove(Context.ConnectionId, out _);

        // Mandamos el estado actualizado a los que quedan
        var estado = _motor.ObtenerEstado(parId, _unidadTrabajo, _mapper);
        await Clients.Group(GrupoPartida(parId)).SendAsync("EstadoActualizado", estado.ValorRetorno);

        // Si ahora le toca al bot que lo reemplazó, que juegue
        await ProcesarTurnosDeBots(parId);
    }

    // ================================================================
    // DESCONEXIÓN AUTOMÁTICA (HU-18)
    // ================================================================
    // Se dispara solo cuando alguien pierde internet, cierra la app
    // o se le apaga el celular.
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // ¿Sabemos qué jugador era esta conexión?
        if (_conexiones.TryRemove(Context.ConnectionId, out var datos))
        {
            var (parId, jpId) = datos;

            // Lo marcamos como RECONECTANDO y arrancamos los 60 seg
            var resultado = _abandonoLN.MarcarDesconectado(parId, jpId, _unidadTrabajo);

            if (resultado.blnIndicadorTransaccion && resultado.ValorRetorno)
            {
                // Avisamos a los demás para que vean el indicador
                await Clients.Group(GrupoPartida(parId)).SendAsync("JugadorDesconectado", new
                {
                    jpId,
                    segundosParaReconectar = _abandonoLN.SegundosRestantesReconexion(jpId)
                });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ================================================================
    // VERIFICAR RECONEXIONES VENCIDAS (HU-18)
    // ================================================================
    // El frontend llama a esto periódicamente. A quienes se les
    // vencieron los 60 segundos, se les aplica abandono automático.
    public async Task VerificarReconexiones(int parId)
    {
        var resultado = _abandonoLN.VerificarDesconexionesVencidas(parId, _unidadTrabajo, _mapper);

        if (resultado.blnIndicadorTransaccion && resultado.ValorRetorno!.Any())
        {
            await Clients.Group(GrupoPartida(parId)).SendAsync("JugadoresReemplazados", new
            {
                jpIds = resultado.ValorRetorno,
                mensaje = resultado.strMensajeRespuesta
            });

            var estado = _motor.ObtenerEstado(parId, _unidadTrabajo, _mapper);
            await Clients.Group(GrupoPartida(parId)).SendAsync("EstadoActualizado", estado.ValorRetorno);

            await ProcesarTurnosDeBots(parId);
        }
    }

    // ================================================================
    // PROCESAR TURNOS DE BOTS
    // ================================================================
    private async Task ProcesarTurnosDeBots(int parId)
    {
        int iteraciones = 0;

        while (iteraciones < 20)
        {
            iteraciones++;

            if (!_botService.EsTurnoDeBot(parId, _unidadTrabajo, out int jpIdBot))
                break;

            await Task.Delay(DELAY_BOT_MS);

            var resultado = _botService.JugarTurnoBot(parId, jpIdBot, _unidadTrabajo, _mapper);

            if (!resultado.blnIndicadorTransaccion)
            {
                await Clients.Group(GrupoPartida(parId))
                    .SendAsync("Error", $"Error en turno del bot: {resultado.strMensajeRespuesta}");
                break;
            }

            await Clients.Group(GrupoPartida(parId)).SendAsync("FichaMovida", resultado.ValorRetorno);

            if (resultado.ValorRetorno!.PartidaFinalizada)
            {
                await Clients.Group(GrupoPartida(parId)).SendAsync("PartidaFinalizada", resultado.ValorRetorno);
                break;
            }
        }
    }
}
