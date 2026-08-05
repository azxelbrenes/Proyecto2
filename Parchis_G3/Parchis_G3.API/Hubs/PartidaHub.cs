
using Microsoft.AspNetCore.SignalR;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using AutoMapper;

namespace Parchis_G3.API.Hubs;

public class PartidaHub : Hub
{
    private readonly IMotorParchisLN _motor;
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;

    public PartidaHub(IMotorParchisLN motor, IUnidadTrabajoEF unidadTrabajo, IMapper mapper)
    {
        _motor = motor;
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
    }

    // Nombre del grupo de SignalR para una partida específica
    private static string GrupoPartida(int parId) => $"partida-{parId}";

    // ── UnirseAPartida ────────────────────────────────────────────
    // El cliente Android llama a esto al entrar a la pantalla del
    // tablero. Lo mete al grupo y le manda el estado actual completo.
    public async Task UnirseAPartida(int parId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GrupoPartida(parId));

        var estado = _motor.ObtenerEstado(parId, _unidadTrabajo, _mapper);

        // Solo le mandamos el estado a quien se acaba de conectar,
        // no a todo el grupo — por eso usamos Clients.Caller
        await Clients.Caller.SendAsync("EstadoActualizado", estado.ValorRetorno);
    }

    // ── SalirDePartida ────────────────────────────────────────────
    // Se llama al salir de la pantalla del tablero normalmente
    // (no por desconexión — eso lo maneja OnDisconnectedAsync)
    public async Task SalirDePartida(int parId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoPartida(parId));
    }

    // ── TirarDado ────────────────────────────────────────────────
    // El jugador toca el dado en su celular. Validamos turno,
    // tiramos el dado y le avisamos a TODOS los jugadores del grupo
    // (no solo a quien tiró) para que vean el resultado en su pantalla.
    public async Task TirarDado(int parId, int jpId)
    {
        var resultado = _motor.TirarDado(parId, jpId, _unidadTrabajo, _mapper);

        if (!resultado.blnIndicadorTransaccion)
        {
            // Si hubo error (ej: no es su turno), solo se lo avisamos a él
            await Clients.Caller.SendAsync("Error", resultado.strMensajeRespuesta);
            return;
        }

        // A todos los jugadores de la partida les llega el resultado del dado
        await Clients.Group(GrupoPartida(parId)).SendAsync("DadoTirado", resultado.ValorRetorno);
    }

    // ── MoverFicha ───────────────────────────────────────────────
    // El jugador eligió qué ficha mover con el dado ya tirado.
    // Se valida el movimiento y se notifica el nuevo estado a todos.
    public async Task MoverFicha(int parId, int jpId, int numeroFicha, int valorDado)
    {
        var resultado = _motor.MoverFicha(parId, jpId, numeroFicha, valorDado, _unidadTrabajo, _mapper);

        if (!resultado.blnIndicadorTransaccion)
        {
            await Clients.Caller.SendAsync("Error", resultado.strMensajeRespuesta);
            return;
        }

        await Clients.Group(GrupoPartida(parId)).SendAsync("FichaMovida", resultado.ValorRetorno);

        // Si la partida terminó, mandamos un evento aparte para que
        // el frontend muestre la pantalla de victoria/derrota
        if (resultado.ValorRetorno!.PartidaFinalizada)
        {
            await Clients.Group(GrupoPartida(parId)).SendAsync("PartidaFinalizada", resultado.ValorRetorno);
        }
    }

    // ── OnDisconnectedAsync ──────────────────────────────────────
    // Se dispara automáticamente cuando un jugador pierde conexión
    // (cierra la app, se le va el internet, etc.)
    // NOTA: la lógica completa de reconexión (HU-18: reservar 60s,
    // activar bot si no vuelve) se implementa en un paso posterior,
    // esto es la base sobre la que se construye.
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Acá en el futuro: buscar en qué partida estaba este jugador
        // y marcar su JpEstadoConexion = 'DESCONECTADO' con timestamp
        await base.OnDisconnectedAsync(exception);
    }
}
