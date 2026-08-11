using System.Collections.Concurrent;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Parchis_G3.API.Hubs;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Services;

/// <summary>
/// RF-03: tiempo límite de 30 segundos por turno y movimiento
/// automático cuando se vence.
///
/// Va en la capa API y no en LogicaNegocios a propósito: necesita
/// IHubContext para difundir el resultado, y la capa de negocio no
/// debería conocer SignalR.
///
/// Es Singleton porque mantiene un temporizador vivo por partida.
/// Como los temporizadores corren fuera de cualquier request, no
/// puede inyectar IUnidadTrabajoEF (que es Scoped): abre su propio
/// scope con IServiceScopeFactory cada vez que necesita la BD.
/// </summary>
public class TemporizadorTurnoService
{
    private readonly IHubContext<PartidaHub> _hub;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMotorParchisLN _motor;
    private readonly IBotServiceLN _botService;

    // Segundos que el frontend muestra en la cuenta regresiva
    public const int SEGUNDOS_LIMITE = 30;

    // Margen extra antes de resolver por el jugador. Cubre la latencia
    // de red: sin esto, alguien que mueve en el segundo 29.8 podría
    // ver su jugada pisada por la automática.
    private const int MARGEN_GRACIA_MS = 700;

    private const int DELAY_BOT_MS = 1500;
    private const int MAX_TURNOS_BOT_SEGUIDOS = 20;

    // parId -> token para cancelar el temporizador en curso
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _temporizadores = new();

    // parId -> candado. Evita que el temporizador y el turno de un
    // bot resuelvan la misma partida al mismo tiempo.
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _candados = new();

    public TemporizadorTurnoService(
        IHubContext<PartidaHub> hub,
        IServiceScopeFactory scopeFactory,
        IMotorParchisLN motor,
        IBotServiceLN botService)
    {
        _hub = hub;
        _scopeFactory = scopeFactory;
        _motor = motor;
        _botService = botService;
    }

    private static string GrupoPartida(int parId) => $"partida-{parId}";

    private SemaphoreSlim Candado(int parId)
        => _candados.GetOrAdd(parId, _ => new SemaphoreSlim(1, 1));

    // ================================================================
    // INICIAR EL TURNO
    // ================================================================
    // Se llama cada vez que cambia el turno. Cancela el temporizador
    // anterior y arranca uno nuevo, pero solo si el jugador es humano:
    // los bots juegan solos y no necesitan reloj.
    public void IniciarTurno(int parId)
    {
        Detener(parId);

        var cts = new CancellationTokenSource();
        _temporizadores[parId] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                int jpIdTurno = ObtenerJugadorDelTurno(parId, out bool esBot);

                // Sin turno asignado o es un bot: no hay nada que cronometrar
                if (jpIdTurno <= 0 || esBot) return;

                // Avisamos al frontend para que muestre la cuenta regresiva
                await _hub.Clients.Group(GrupoPartida(parId)).SendAsync(
                    "TurnoIniciado",
                    new { JpId = jpIdTurno, Segundos = SEGUNDOS_LIMITE },
                    cts.Token);

                await Task.Delay(SEGUNDOS_LIMITE * 1000 + MARGEN_GRACIA_MS, cts.Token);

                if (cts.Token.IsCancellationRequested) return;

                await ResolverTurnoVencido(parId, jpIdTurno);
            }
            catch (OperationCanceledException)
            {
                // El jugador actuó a tiempo. Es el caso normal.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Temporizador] Error en partida {parId}: {ex.Message}");
            }
        }, cts.Token);
    }

    // ================================================================
    // DETENER
    // ================================================================
    // Se llama cuando el jugador actúa a tiempo, cuando la partida
    // termina y cuando alguien abandona.
    public void Detener(int parId)
    {
        if (_temporizadores.TryRemove(parId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    // Libera todo lo que la partida dejó en memoria
    public void Limpiar(int parId)
    {
        Detener(parId);

        if (_candados.TryRemove(parId, out var candado))
            candado.Dispose();
    }

    // ================================================================
    // RESOLVER UN TURNO VENCIDO
    // ================================================================
    private async Task ResolverTurnoVencido(int parId, int jpId)
    {
        var candado = Candado(parId);
        await candado.WaitAsync();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unidadTrabajo = scope.ServiceProvider.GetRequiredService<IUnidadTrabajoEF>();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

            // Verificamos de nuevo: entre que venció el reloj y llegamos
            // acá, el jugador pudo haber movido.
            if (!_motor.TurnoVencido(parId)) return;

            var resultado = _motor.EjecutarMovimientoAutomatico(parId, jpId, unidadTrabajo, mapper);

            if (!resultado.blnIndicadorTransaccion)
            {
                Console.WriteLine($"[Temporizador] No se pudo resolver el turno de {jpId}: {resultado.strMensajeRespuesta}");
                return;
            }

            await _hub.Clients.Group(GrupoPartida(parId))
                .SendAsync("TurnoAutomatico", new
                {
                    JpId = jpId,
                    Mensaje = "Se acabó el tiempo. El sistema jugó por ese jugador."
                });

            await _hub.Clients.Group(GrupoPartida(parId))
                .SendAsync("FichaMovida", resultado.ValorRetorno);

            if (resultado.ValorRetorno!.PartidaFinalizada)
            {
                await _hub.Clients.Group(GrupoPartida(parId))
                    .SendAsync("PartidaFinalizada", resultado.ValorRetorno);

                Limpiar(parId);
                return;
            }
        }
        finally
        {
            candado.Release();
        }

        // Fuera del candado: los bots lo toman de nuevo por su cuenta
        await ProcesarTurnosDeBots(parId);
        IniciarTurno(parId);
    }

    // ================================================================
    // TURNOS DE BOTS
    // ================================================================
    // Estaba duplicado dentro del Hub. Al vivir acá lo pueden usar
    // tanto el Hub como el temporizador, con una sola implementación.
    public async Task ProcesarTurnosDeBots(int parId)
    {
        var candado = Candado(parId);
        await candado.WaitAsync();

        try
        {
            int iteraciones = 0;

            while (iteraciones < MAX_TURNOS_BOT_SEGUIDOS)
            {
                iteraciones++;

                using var scope = _scopeFactory.CreateScope();
                var unidadTrabajo = scope.ServiceProvider.GetRequiredService<IUnidadTrabajoEF>();
                var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

                if (!_botService.EsTurnoDeBot(parId, unidadTrabajo, out int jpIdBot))
                    break;

                await Task.Delay(DELAY_BOT_MS);

                var resultado = _botService.JugarTurnoBot(parId, jpIdBot, unidadTrabajo, mapper);

                if (!resultado.blnIndicadorTransaccion)
                {
                    await _hub.Clients.Group(GrupoPartida(parId))
                        .SendAsync("Error", $"Error en turno del bot: {resultado.strMensajeRespuesta}");
                    break;
                }

                await _hub.Clients.Group(GrupoPartida(parId))
                    .SendAsync("FichaMovida", resultado.ValorRetorno);

                if (resultado.ValorRetorno!.PartidaFinalizada)
                {
                    await _hub.Clients.Group(GrupoPartida(parId))
                        .SendAsync("PartidaFinalizada", resultado.ValorRetorno);

                    Limpiar(parId);
                    return;
                }
            }
        }
        finally
        {
            candado.Release();
        }
    }

    // ================================================================
    // HELPER
    // ================================================================
    private int ObtenerJugadorDelTurno(int parId, out bool esBot)
    {
        esBot = false;

        using var scope = _scopeFactory.CreateScope();
        var unidadTrabajo = scope.ServiceProvider.GetRequiredService<IUnidadTrabajoEF>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var estado = _motor.ObtenerEstado(parId, unidadTrabajo, mapper);

        if (!estado.blnIndicadorTransaccion || estado.ValorRetorno == null)
            return 0;

        int jpIdTurno = estado.ValorRetorno.TurnoActualJpId;

        var jugador = estado.ValorRetorno.Jugadores.FirstOrDefault(j => j.JpId == jpIdTurno);
        esBot = jugador?.EsBot ?? false;

        return jpIdTurno;
    }
}