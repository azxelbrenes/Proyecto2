using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Servicios;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TiposArticuloController : ControllerBase
{
    private readonly ITiposArticuloLN _tiposArticuloLN;
    private readonly ILogger<TiposArticuloController> _logger;

    public TiposArticuloController(ITiposArticuloLN tiposArticuloLN, ILogger<TiposArticuloController> logger)
    {
        _tiposArticuloLN = tiposArticuloLN;
        _logger = logger;
    }

    // Lista todos los tipos de artículo disponibles (Ficha, Tablero, Dado)
    [HttpGet]
    public IActionResult Listar()
    {
        try
        {
            var respuesta = _tiposArticuloLN.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TiposArticuloController.Listar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // Busca un tipo específico por su ID
    [HttpGet("{id}")]
    public IActionResult Buscar(int id)
    {
        try
        {
            var respuesta = _tiposArticuloLN.Buscar(new TTiposArticulo { TipId = id });
            if (!respuesta.blnIndicadorTransaccion)
                return NotFound(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TiposArticuloController.Buscar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // Crea un nuevo tipo de artículo — la LN valida que el nombre no esté vacío
    [HttpPost]
    public IActionResult Insertar([FromBody] TTiposArticulo entidad)
    {
        try
        {
            var respuesta = _tiposArticuloLN.Insertar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TiposArticuloController.Insertar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // Modifica un tipo existente
    [HttpPut]
    public IActionResult Modificar([FromBody] TTiposArticulo entidad)
    {
        try
        {
            var respuesta = _tiposArticuloLN.Modificar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TiposArticuloController.Modificar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // Elimina un tipo — la LN verifica que exista antes de eliminar
    [HttpDelete("{id}")]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var respuesta = _tiposArticuloLN.Eliminar(new TTiposArticulo { TipId = id });
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TiposArticuloController.Eliminar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
