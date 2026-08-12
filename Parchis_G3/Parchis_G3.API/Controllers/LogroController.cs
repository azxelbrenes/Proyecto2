using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Services;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/logro")]
public class LogroController : ControllerBase
{
    private readonly ILogroLN _logroLN;
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly JwtService _jwtService;
    private readonly ILogger<LogroController> _logger;

    public LogroController(
        ILogroLN logroLN,
        IUnidadTrabajoEF unidadTrabajo,
        JwtService jwtService,
        ILogger<LogroController> logger)
    {
        _logroLN = logroLN;
        _unidadTrabajo = unidadTrabajo;
        _jwtService = jwtService;
        _logger = logger;
    }

    // ── GET /api/logro ───────────────────────────────────────────
    // Los 8 logros con su progreso. El usuario sale del token: nadie
    // puede consultar los logros de otro.
    [HttpGet]
    public IActionResult ObtenerLogros()
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0) return Unauthorized("Token inválido.");

            var respuesta = _logroLN.ObtenerLogros(usuId, _unidadTrabajo);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en LogroController.ObtenerLogros");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/logro/reclamar ─────────────────────────────────
    // Acredita las monedas de todos los logros pendientes.
    [HttpPost("reclamar")]
    public IActionResult Reclamar()
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0) return Unauthorized("Token inválido.");

            var respuesta = _logroLN.ReclamarPendientes(usuId, _unidadTrabajo);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en LogroController.Reclamar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}