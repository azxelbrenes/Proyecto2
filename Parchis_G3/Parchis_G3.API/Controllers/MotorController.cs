using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using AutoMapper;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/motor")]
public class MotorController : ControllerBase
{
    private readonly IMotorParchisLN _motorLN;
    private readonly IBotServiceLN _botService;
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<MotorController> _logger;

    public MotorController(
        IMotorParchisLN motorLN,
        IBotServiceLN botService,
        IUnidadTrabajoEF unidadTrabajo,
        IMapper mapper,
        ILogger<MotorController> logger)
    {
        _motorLN = motorLN;
        _botService = botService;
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // ── POST /api/motor/iniciar/{parId} ──────────────────────────
    [HttpPost("iniciar/{parId}")]
    public IActionResult Iniciar(int parId)
    {
        try
        {
            var respuesta = _motorLN.IniciarPartida(parId, _unidadTrabajo);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MotorController.Iniciar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/motor/tirar-dado ───────────────────────────────
    // Body: { "parId": 1, "jpId": 3 }
    [HttpPost("tirar-dado")]
    public IActionResult TirarDado([FromBody] TirarDadoRequest request)
    {
        try
        {
            var respuesta = _motorLN.TirarDado(request.ParId, request.JpId, _unidadTrabajo, _mapper);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MotorController.TirarDado");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/motor/mover-ficha ──────────────────────────────
    // Body: { "parId": 1, "jpId": 3, "numeroFicha": 1, "valorDado": 5 }
    [HttpPost("mover-ficha")]
    public IActionResult MoverFicha([FromBody] MoverFichaRequest request)
    {
        try
        {
            var respuesta = _motorLN.MoverFicha(
                request.ParId, request.JpId, request.NumeroFicha, request.ValorDado,
                _unidadTrabajo, _mapper
            );
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MotorController.MoverFicha");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/motor/estado/{parId} ────────────────────────────
    [HttpGet("estado/{parId}")]
    public IActionResult ObtenerEstado(int parId)
    {
        try
        {
            var respuesta = _motorLN.ObtenerEstado(parId, _unidadTrabajo, _mapper);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MotorController.ObtenerEstado");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/motor/turno-bot/{parId} ────────────────────────
    // Juega el turno de UN solo bot (si es que le toca a uno).
    // Útil para debugging paso a paso en Postman.
    [HttpPost("turno-bot/{parId}")]
    public IActionResult TurnoBot(int parId)
    {
        try
        {
            if (!_botService.EsTurnoDeBot(parId, _unidadTrabajo, out int jpIdBot))
                return BadRequest(new { mensaje = "El turno actual no es de un bot." });

            var respuesta = _botService.JugarTurnoBot(parId, jpIdBot, _unidadTrabajo, _mapper);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MotorController.TurnoBot");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/motor/procesar-bots/{parId} ────────────────────
    // Juega TODOS los turnos de bots seguidos hasta que le toque
    // a un jugador humano o termine la partida.
    // Este es el que más vas a usar para probar en Postman.
    [HttpPost("procesar-bots/{parId}")]
    public IActionResult ProcesarBots(int parId)
    {
        try
        {
            var jugadas = new List<object>();
            int iteraciones = 0;

            // Mismo bucle que hace el Hub, pero devolviendo el
            // historial completo para que lo veas en Postman
            while (iteraciones < 20)
            {
                iteraciones++;

                if (!_botService.EsTurnoDeBot(parId, _unidadTrabajo, out int jpIdBot))
                    break;

                var resultado = _botService.JugarTurnoBot(parId, jpIdBot, _unidadTrabajo, _mapper);

                if (!resultado.blnIndicadorTransaccion)
                {
                    jugadas.Add(new { error = resultado.strMensajeRespuesta });
                    break;
                }

                jugadas.Add(new
                {
                    jpIdBot,
                    dado = resultado.ValorRetorno!.ValorDado,
                    huboCaptura = resultado.ValorRetorno.HuboCaptura,
                    fichaCoronada = resultado.ValorRetorno.FichaCoronada,
                    mensaje = resultado.ValorRetorno.Mensaje
                });

                if (resultado.ValorRetorno.PartidaFinalizada)
                    break;
            }

            // Devolvemos el estado final después de todas las jugadas
            var estadoFinal = _motorLN.ObtenerEstado(parId, _unidadTrabajo, _mapper);

            return Ok(new
            {
                totalJugadas = jugadas.Count,
                jugadas,
                estadoFinal = estadoFinal.ValorRetorno
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MotorController.ProcesarBots");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}

// ── DTOs de request ───────────────────────────────────────────────
public class TirarDadoRequest
{
    public int ParId { get; set; }
    public int JpId { get; set; }
}

public class MoverFichaRequest
{
    public int ParId { get; set; }
    public int JpId { get; set; }
    public int NumeroFicha { get; set; }
    public int ValorDado { get; set; }
}
