using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Services;
using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/inventario")]
public class InventarioController : ControllerBase
{
    private readonly IInventarioLN _inventarioLN;
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly JwtService _jwtService;
    private readonly ILogger<InventarioController> _logger;

    public InventarioController(
        IInventarioLN inventarioLN,
        IUnidadTrabajoEF unidadTrabajo,
        JwtService jwtService,
        ILogger<InventarioController> logger)
    {
        _inventarioLN = inventarioLN;
        _unidadTrabajo = unidadTrabajo;
        _jwtService = jwtService;
        _logger = logger;
    }

    // ── GET /api/inventario/articulos ────────────────────────────
    // Todos los artículos que el usuario desbloqueó (comprados +
    // predeterminados), marcando cuáles tiene equipados.
    // La tienda lo usa para mostrar "En tu inventario" en vez del
    // botón de comprar.
    [HttpGet("articulos")]
    public IActionResult ObtenerMisArticulos()
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0) return Unauthorized("Token inválido.");

            var respuesta = _inventarioLN.ObtenerMisArticulos(usuId, _unidadTrabajo);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en InventarioController.ObtenerMisArticulos");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/inventario/equipamiento ─────────────────────────
    // Qué ficha, tablero y dado tiene puestos ahora mismo.
    // El tablero del juego lo usa para renderizar con los diseños
    // que el jugador eligió.
    [HttpGet("equipamiento")]
    public IActionResult ObtenerMiEquipamiento()
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0) return Unauthorized("Token inválido.");

            var respuesta = _inventarioLN.ObtenerMiEquipamiento(usuId, _unidadTrabajo);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en InventarioController.ObtenerMiEquipamiento");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── PUT /api/inventario/equipar ──────────────────────────────
    // Body: { "ArtId": 3 }
    // Cambia el artículo activo de su categoría. El backend valida
    // que el usuario realmente lo tenga desbloqueado.
    [HttpPut("equipar")]
    public IActionResult Equipar([FromBody] EquiparRequest request)
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0) return Unauthorized("Token inválido.");

            if (request == null || request.ArtId <= 0)
                return BadRequest(new { strMensajeRespuesta = "El artículo es requerido." });

            var respuesta = _inventarioLN.EquiparArticulo(usuId, request.ArtId, _unidadTrabajo);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en InventarioController.Equipar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
