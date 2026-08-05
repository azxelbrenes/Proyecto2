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
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<MotorController> _logger;

    public MotorController(IMotorParchisLN motorLN, IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<MotorController> logger)
    {
        _motorLN = motorLN;
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // ── POST /api/motor/iniciar/{parId} ──────────────────────────
    // Crea las 16 fichas (4 por jugador) y marca la partida como
    // EN_JUEGO. Los 4 jugadores ya deben existir en JugadoresPartida
    // (por ahora insertalos manualmente con el JugadoresPartidaController
    // mientras no tenemos matchmaking automático).
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
    // Body de ejemplo: { "parId": 1, "jpId": 3 }
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
    // Body de ejemplo: { "parId": 1, "jpId": 3, "numeroFicha": 1, "valorDado": 5 }
    // El valorDado DEBE ser el mismo que devolvió tirar-dado —
    // el servidor lo valida y rechaza si no coincide.
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
    // Devuelve la foto completa del tablero: todas las fichas,
    // sus posiciones, y de quién es el turno actual.
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
}

// ── DTOs de request para los endpoints de arriba ──────────────────
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
