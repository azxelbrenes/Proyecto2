using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Services;
using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using AutoMapper;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatLN _chatLN;
    private readonly IAbandonoLN _abandonoLN;
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly JwtService _jwtService;
    private readonly IMapper _mapper;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatLN chatLN,
        IAbandonoLN abandonoLN,
        IUnidadTrabajoEF unidadTrabajo,
        JwtService jwtService,
        IMapper mapper,
        ILogger<ChatController> logger)
    {
        _chatLN = chatLN;
        _abandonoLN = abandonoLN;
        _unidadTrabajo = unidadTrabajo;
        _jwtService = jwtService;
        _mapper = mapper;
        _logger = logger;
    }

    // ── GET /api/chat/predefinidos ───────────────────────────────
    // Los 4 mensajes rápidos de la HU-14
    [HttpGet("predefinidos")]
    public IActionResult ObtenerPredefinidos()
    {
        try
        {
            return Ok(_chatLN.ObtenerMensajesPredefinidos());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ChatController.ObtenerPredefinidos");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/chat/historial/{parId} ──────────────────────────
    [HttpGet("historial/{parId}")]
    public IActionResult ObtenerHistorial(int parId)
    {
        try
        {
            var respuesta = _chatLN.ObtenerHistorial(parId, _unidadTrabajo);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ChatController.ObtenerHistorial");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/chat/enviar ────────────────────────────────────
    // Body: { "ParId": 1, "JpId": 3, "Contenido": "¡Gg!", "EsPredefinido": true }
    [HttpPost("enviar")]
    public IActionResult EnviarMensaje([FromBody] EnviarMensajeRequest request)
    {
        try
        {
            var respuesta = _chatLN.EnviarMensaje(
                request.ParId, request.JpId, request.Contenido,
                request.EsPredefinido, _unidadTrabajo
            );

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ChatController.EnviarMensaje");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/chat/abandonar/{parId} ─────────────────────────
    // HU-19: abandono con penalización del 20%
    [HttpPost("abandonar/{parId}")]
    public IActionResult AbandonarPartida(int parId)
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0)
                return Unauthorized("Token inválido.");

            var respuesta = _abandonoLN.AbandonarPartida(usuId, parId, _unidadTrabajo, _mapper);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ChatController.AbandonarPartida");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/chat/verificar-reconexiones/{parId} ────────────
    // HU-18: revisa si a alguien se le vencieron los 60 segundos
    [HttpPost("verificar-reconexiones/{parId}")]
    public IActionResult VerificarReconexiones(int parId)
    {
        try
        {
            var respuesta = _abandonoLN.VerificarDesconexionesVencidas(parId, _unidadTrabajo, _mapper);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ChatController.VerificarReconexiones");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
