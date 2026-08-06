using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Services;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using AutoMapper;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/matchmaking")]
public class MatchmakingController : ControllerBase
{
    private readonly IMatchmakingLN _matchmakingLN;
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly JwtService _jwtService;
    private readonly IMapper _mapper;
    private readonly ILogger<MatchmakingController> _logger;

    public MatchmakingController(
        IMatchmakingLN matchmakingLN,
        IUnidadTrabajoEF unidadTrabajo,
        JwtService jwtService,
        IMapper mapper,
        ILogger<MatchmakingController> logger)
    {
        _matchmakingLN = matchmakingLN;
        _unidadTrabajo = unidadTrabajo;
        _jwtService = jwtService;
        _mapper = mapper;
        _logger = logger;
    }

    // ── POST /api/matchmaking/buscar/{salId} ─────────────────────
    // El jugador toca una sala en el home. Este endpoint lo mete
    // a una partida existente o crea una nueva, le cobra la entrada
    // y le asigna color. Devuelve el ParId y JpId que necesita el
    // frontend para conectarse al Hub de SignalR.
    [HttpPost("buscar/{salId}")]
    public IActionResult BuscarPartida(int salId)
    {
        try
        {
            // El usuario sale del token, nunca del body
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0)
                return Unauthorized("Token inválido.");

            var respuesta = _matchmakingLN.BuscarPartida(usuId, salId, _unidadTrabajo, _mapper);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MatchmakingController.BuscarPartida");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/matchmaking/espera/{parId} ──────────────────────
    // El frontend consulta esto mientras muestra la sala de espera
    // para ver quién se ha ido uniendo y cuántos segundos faltan.
    [HttpGet("espera/{parId}")]
    public IActionResult ObtenerEstadoEspera(int parId)
    {
        try
        {
            var respuesta = _matchmakingLN.ObtenerEstadoEspera(parId, _unidadTrabajo);

            if (!respuesta.blnIndicadorTransaccion)
                return NotFound(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MatchmakingController.ObtenerEstadoEspera");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/matchmaking/verificar/{parId} ──────────────────
    // Revisa si ya pasaron los 30 segundos o si la partida se llenó.
    // Si corresponde, completa los cupos con bots e inicia el juego.
    // El frontend lo llama cada segundo mientras espera.
    [HttpPost("verificar/{parId}")]
    public IActionResult VerificarEIniciar(int parId)
    {
        try
        {
            var respuesta = _matchmakingLN.VerificarEInicIar(parId, _unidadTrabajo, _mapper);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MatchmakingController.VerificarEIniciar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── DELETE /api/matchmaking/abandonar/{parId} ────────────────
    // El jugador sale de la sala de espera antes de que arranque.
    // Se le devuelven las monedas completas sin penalización.
    [HttpDelete("abandonar/{parId}")]
    public IActionResult AbandonarEspera(int parId)
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0)
                return Unauthorized("Token inválido.");

            var respuesta = _matchmakingLN.AbandonarEspera(usuId, parId, _unidadTrabajo);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(new
            {
                mensaje = respuesta.strMensajeRespuesta,
                monedas = respuesta.ValorRetorno
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MatchmakingController.AbandonarEspera");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
