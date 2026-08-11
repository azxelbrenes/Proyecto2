using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Parchis_G3.API.Services;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using AutoMapper;

namespace Parchis_G3.API.Hubs;

public class PartidaHub : Hub
{
    private readonly IMotorParchisLN _motor;
    private readonly IChatLN _chatLN;
    private readonly IAbandonoLN _abandonoLN;
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;

    // RF-03: el reloj de 30 segundos y los turnos de bots viven acá.
    // El bucle de bots estaba duplicado dentro del Hub; ahora hay una
    // sola implementación que usan tanto el Hub como el temporizador.
    private readonly TemporizadorTurnoService _temporizador;

    // ConnectionId -> (parId, jpId)
    // Estático porque el Hub se instancia de nuevo en cada llamada
    private static readonly ConcurrentDictionary<string, (int parId, int jpId)> _conexiones = new();

    public PartidaHub(
        IMotorParchisLN motor,
        IChatLN chatLN,
        IAbandonoLN abandonoLN,
        IUnidadTrabajoEF unidadTrabajo,
        IMapper mapper,
        TemporizadorTurnoService temporizador)
    {
        _motor = motor;
        _chatLN = chatLN;
        _abandonoLN = abandonoLN;
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _temporizador = temporizador;
    }

    private static string GrupoPartida(int parId) => $"partida-{parId}";

    // ================================================================
    // UNIRSE A LA PARTIDA
    // ================================================================
    public async Task UnirseAPartida(int parId, int jpId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GrupoPartida(parId));

        _conexiones[Context.ConnectionId] = (parId, jpId);

        // Si venía de una desconexión, lo reconectamos (RF-13)
        var reconexion = _abandonoLN.Reconectar(parId, jpId, _unidadTrabajo);

        if (reconexion.blnIndicadorTransaccion && reconexion.ValorRetorno)
        {
            await Clients.OthersInGroup(GrupoPartida(parId))
                .SendAsync("JugadorReconectado", new { jpId });
        }

        var estado = _motor.ObtenerEstado(parId, _unidadTrabajo, _mapper);
        await Clients.Caller.SendAsync("EstadoActualizado", estado.ValorRetorno);

        var historial = _chatLN.ObtenerHistorial(parId, _unidadTrabajo);
        await Clients.Caller.SendAsync("HistorialChat", historial.ValorRetorno);

        // Le decimos cuántos segundos le quedan al turno en curso, para
        // que quien se reconecta vea la cuenta regresiva correcta y no
        // arranque de 30 otra vez.
        await Clients.Caller.SendAsync("TurnoIniciado", new
        {
            JpId = estado.ValorRetorno?.TurnoActualJpId ?? 0,
            Segundos = _motor.SegundosRestantesTurno(parId)
        });

        await _temporizador.ProcesarTurnosDeBots(parId);
        _temporizador.IniciarTurno(parId);
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
        // El jugador actuó: paramos el reloj antes de nada
        _temporizador.Detener(parId);

        var resultado = _motor.TirarDado(parId, jpId, _unidadTrabajo, _mapper);

        if (!resultado.blnIndicadorTransaccion)
        {
            await Clients.Caller.SendAsync("Error", resultado.strMensajeRespuesta);

            // Si la tirada falló, el turno sigue siendo suyo: le
            // devolvemos el reloj para no dejarlo sin límite
            _temporizador.IniciarTurno(parId);
            return;
        }

        await Clients.Group(GrupoPartida(parId)).SendAsync("DadoTirado", resultado.ValorRetorno);

        // Si el motor cedió el turno (sin movimientos posibles o tercer 6),
        // puede tocarle a un bot
        if (resultado.ValorRetorno!.SiguienteTurnoJpId != jpId)
        {
            await _temporizador.ProcesarTurnosDeBots(parId);
        }

        _temporizador.IniciarTurno(parId);
    }

    // ================================================================
    // MOVER FICHA
    // ================================================================
    public async Task MoverFicha(int parId, int jpId, int numeroFicha, int valorDado)
    {
        _temporizador.Detener(parId);

        var resultado = _motor.MoverFicha(parId, jpId, numeroFicha, valorDado, _unidadTrabajo, _mapper);

        if (!resultado.blnIndicadorTransaccion)
        {
            await Clients.Caller.SendAsync("Error", resultado.strMensajeRespuesta);
            _temporizador.IniciarTurno(parId);
            return;
        }

        await Clients.Group(GrupoPartida(parId)).SendAsync("FichaMovida", resultado.ValorRetorno);

        if (resultado.ValorRetorno!.PartidaFinalizada)
        {
            await Clients.Group(GrupoPartida(parId)).SendAsync("PartidaFinalizada", resultado.ValorRetorno);
            _temporizador.Limpiar(parId);
            return;
        }

        await _temporizador.ProcesarTurnosDeBots(parId);
        _temporizador.IniciarTurno(parId);
    }

    // ================================================================
    // ENVIAR MENSAJE DE CHAT (RF-09)
    // ================================================================
    public async Task EnviarMensaje(int parId, int jpId, string contenido, bool esPredefinido)
    {
        var resultado = _chatLN.EnviarMensaje(parId, jpId, contenido, esPredefinido, _unidadTrabajo);

        if (!resultado.blnIndicadorTransaccion)
        {
            await Clients.Caller.SendAsync("Error", resultado.strMensajeRespuesta);
            return;
        }

        await Clients.Group(GrupoPartida(parId))
            .SendAsync("MensajeRecibido", resultado.ValorRetorno);
    }

    // ================================================================
    // ABANDONAR PARTIDA (RF-14)
    // ================================================================
    public async Task AbandonarPartida(int parId, int usuId)
    {
        var resultado = _abandonoLN.AbandonarPartida(usuId, parId, _unidadTrabajo, _mapper);

        if (!resultado.blnIndicadorTransaccion)
        {
            await Clients.Caller.SendAsync("Error", resultado.strMensajeRespuesta);
            return;
        }

        await Clients.Caller.SendAsync("AbandonoConfirmado", resultado.ValorRetorno);

        await Clients.OthersInGroup(GrupoPartida(parId)).SendAsync("JugadorAbandono", new
        {
            jpId = resultado.ValorRetorno!.JpId,
            mensaje = "Un jugador abandonó. Un bot tomó su lugar."
        });

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoPartida(parId));
        _conexiones.TryRemove(Context.ConnectionId, out _);

        var estado = _motor.ObtenerEstado(parId, _unidadTrabajo, _mapper);
        await Clients.Group(GrupoPartida(parId)).SendAsync("EstadoActualizado", estado.ValorRetorno);

        // El puesto ahora lo juega un bot: puede tocarle de inmediato
        await _temporizador.ProcesarTurnosDeBots(parId);
        _temporizador.IniciarTurno(parId);
    }

    // ================================================================
    // DESCONEXIÓN AUTOMÁTICA (RF-13)
    // ================================================================
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_conexiones.TryRemove(Context.ConnectionId, out var datos))
        {
            var (parId, jpId) = datos;

            var resultado = _abandonoLN.MarcarDesconectado(parId, jpId, _unidadTrabajo);

            if (resultado.blnIndicadorTransaccion && resultado.ValorRetorno)
            {
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
    // VERIFICAR RECONEXIONES VENCIDAS (RF-13)
    // ================================================================
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

            await _temporizador.ProcesarTurnosDeBots(parId);
            _temporizador.IniciarTurno(parId);
        }
    }
}