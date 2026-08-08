using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Services;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/recompensa")]
public class RecompensaController : ControllerBase
{
    private readonly IRecompensaLN _recompensaLN;
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly JwtService _jwtService;
    private readonly ILogger<RecompensaController> _logger;

    public RecompensaController(
        IRecompensaLN recompensaLN,
        IUnidadTrabajoEF unidadTrabajo,
        JwtService jwtService,
        ILogger<RecompensaController> logger)
    {
        _recompensaLN = recompensaLN;
        _unidadTrabajo = unidadTrabajo;
        _jwtService = jwtService;
        _logger = logger;
    }

    // ── GET /api/recompensa/estado ───────────────────────────────
    // ¿Puede reclamar hoy? ¿Qué racha lleva? ¿Cuánto ganaría?
    // El frontend lo llama al abrir la app para decidir si muestra
    // el modal de recompensa diaria.
    [HttpGet("estado")]
    public IActionResult ObtenerEstado()
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0) return Unauthorized("Token inválido.");

            var respuesta = _recompensaLN.ObtenerEstado(usuId, _unidadTrabajo);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RecompensaController.ObtenerEstado");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/recompensa/reclamar ────────────────────────────
    // Acredita las monedas del día y avanza la racha.
    // El backend valida que no haya reclamado ya hoy — sin esa
    // validación alguien podría llamar 100 veces al endpoint y
    // llenarse de monedas.
    [HttpPost("reclamar")]
    public IActionResult Reclamar()
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0) return Unauthorized("Token inválido.");

            var respuesta = _recompensaLN.Reclamar(usuId, _unidadTrabajo);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RecompensaController.Reclamar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
