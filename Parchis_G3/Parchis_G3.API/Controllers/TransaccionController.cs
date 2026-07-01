using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Servicios;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/transaccion")]
public class TransaccionController : ControllerBase
{
    private readonly ITransaccionLN _transaccionLN;
    private readonly JwtService _jwtService;
    private readonly ILogger<TransaccionController> _logger;

    public TransaccionController(ITransaccionLN transaccionLN, JwtService jwtService, ILogger<TransaccionController> logger)
    {
        _transaccionLN = transaccionLN;
        _jwtService = jwtService;
        _logger = logger;
    }

    // ── GET /api/transaccion ────────────────────────────────────────
    // Devuelve todos los registros disponibles.
    [HttpGet]
    public IActionResult Listar()
    {
        try
        {
            var respuesta = _transaccionLN.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TransaccionController.Listar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/transaccion/{id} ───────────────────────────────────
    // Busca un registro específico por su ID.
    [HttpGet("{id}")]
    public IActionResult Buscar(int id)
    {
        try
        {
            var entidad = new TTransaccione();
            entidad.TranId = id;
            var respuesta = _transaccionLN.Buscar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return NotFound(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TransaccionController.Buscar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/transaccion ───────────────────────────────────────
    // Inserta un nuevo registro. La LN valida los datos antes de guardar.
    [HttpPost]
    public IActionResult Insertar([FromBody] TTransaccione entidad)
    {
        try
        {
            var respuesta = _transaccionLN.Insertar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TransaccionController.Insertar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── PUT /api/transaccion ────────────────────────────────────────
    // Modifica un registro existente.
    [HttpPut]
    public IActionResult Modificar([FromBody] TTransaccione entidad)
    {
        try
        {
            var respuesta = _transaccionLN.Modificar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TransaccionController.Modificar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── DELETE /api/transaccion/{id} ────────────────────────────────
    // Elimina un registro por su ID.
    [HttpDelete("{id}")]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var entidad = new TTransaccione();
            entidad.TranId = id;
            var respuesta = _transaccionLN.Eliminar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TransaccionController.Eliminar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }


    // ── GET /api/transaccion/mias ────────────────────────────────
    // Devuelve solo las transacciones del usuario autenticado.
    // Android lo usa para mostrar el historial de monedas en el perfil.
    [HttpGet("mias")]
    public IActionResult ObtenerMias()
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0) return Unauthorized("Token inválido.");

            var respuesta = _transaccionLN.Obtener(new TTransaccione { UsuId = usuId });
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TransaccionController.ObtenerMias");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
