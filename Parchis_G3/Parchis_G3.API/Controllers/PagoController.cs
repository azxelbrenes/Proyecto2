using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Services;
using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/pago")]
public class PagoController : ControllerBase
{
    private readonly IPagoLN _pagoLN;
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly JwtService _jwtService;
    private readonly ILogger<PagoController> _logger;

    public PagoController(
        IPagoLN pagoLN,
        IUnidadTrabajoEF unidadTrabajo,
        JwtService jwtService,
        ILogger<PagoController> logger)
    {
        _pagoLN = pagoLN;
        _unidadTrabajo = unidadTrabajo;
        _jwtService = jwtService;
        _logger = logger;
    }

    // ── GET /api/pago/paquetes ───────────────────────────────────
    // Devuelve los 4 paquetes de monedas disponibles con sus precios.
    // El frontend los muestra en la tienda de monedas.
    [HttpGet("paquetes")]
    public IActionResult ObtenerPaquetes()
    {
        try
        {
            var respuesta = _pagoLN.ObtenerPaquetes();
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PagoController.ObtenerPaquetes");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/pago/crear-orden ───────────────────────────────
    // Body: { "PaqueteId": 2 }
    // Le pide a PayPal crear una orden y devuelve la URL a la que
    // el frontend debe mandar al usuario para que apruebe el pago.
    [HttpPost("crear-orden")]
    public async Task<IActionResult> CrearOrden([FromBody] CrearOrdenRequest request)
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0)
                return Unauthorized("Token inválido.");

            var respuesta = await _pagoLN.CrearOrden(usuId, request.PaqueteId);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PagoController.CrearOrden");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/pago/capturar ──────────────────────────────────
    // Body: { "OrdenId": "5O190127TN364715T", "PaqueteId": 2 }
    // Verifica CON PAYPAL que el pago se completó de verdad y
    // recién ahí acredita las monedas al usuario.
    [HttpPost("capturar")]
    public async Task<IActionResult> CapturarPago([FromBody] CapturarPagoRequest request)
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0)
                return Unauthorized("Token inválido.");

            if (string.IsNullOrWhiteSpace(request.OrdenId))
                return BadRequest("El ID de la orden es requerido.");

            var respuesta = await _pagoLN.CapturarPago(
                usuId, request.OrdenId, request.PaqueteId, _unidadTrabajo
            );

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PagoController.CapturarPago");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
