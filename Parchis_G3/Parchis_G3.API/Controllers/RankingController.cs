using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Services;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/ranking")]
public class RankingController : ControllerBase
{
    private readonly IRankingLN _rankingLN;
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly JwtService _jwtService;
    private readonly ILogger<RankingController> _logger;

    public RankingController(
        IRankingLN rankingLN,
        IUnidadTrabajoEF unidadTrabajo,
        JwtService jwtService,
        ILogger<RankingController> logger)
    {
        _rankingLN = rankingLN;
        _unidadTrabajo = unidadTrabajo;
        _jwtService = jwtService;
        _logger = logger;
    }

    // ── GET /api/ranking?top=50 ──────────────────────────────────
    // Devuelve los mejores jugadores ordenados por monedas ganadas
    // EN PARTIDAS (nunca por saldo total — eso permitiría comprar
    // el primer puesto con PayPal).
    //
    // Incluye la posición del usuario actual aunque esté fuera del
    // top, para que siempre pueda verse a sí mismo.
    [HttpGet]
    public IActionResult ObtenerRanking([FromQuery] int top = 50)
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0) return Unauthorized("Token inválido.");

            var respuesta = _rankingLN.ObtenerRanking(usuId, top, _unidadTrabajo);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RankingController.ObtenerRanking");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
