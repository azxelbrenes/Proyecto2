using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/estadoficha")]
public class EstadoFichaController : ControllerBase
{
    // La LN se inyecta automáticamente por el contenedor de dependencias
    // configurado en Program.cs — no usamos "new" directamente
    private readonly IEstadoFichaLN _estadofichaLN;
    private readonly ILogger<EstadoFichaController> _logger;

    public EstadoFichaController(IEstadoFichaLN estadofichaLN, ILogger<EstadoFichaController> logger)
    {
        _estadofichaLN = estadofichaLN;
        _logger = logger;
    }

    // ── GET /api/estadoficha ────────────────────────────────────────
    // Devuelve todos los registros. La LN aplica los includes necesarios.
    [HttpGet]
    public IActionResult Listar()
    {
        try
        {
            var respuesta = _estadofichaLN.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EstadoFichaController.Listar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/estadoficha/{id} ───────────────────────────────────
    // Busca un registro específico por su ID (EfId).
    [HttpGet("{id}")]
    public IActionResult Buscar(int id)
    {
        try
        {
            // Creamos el objeto tipado con solo el PK para que la LN lo busque
            var entidad = new TEstadoFicha();
            entidad.EfId = id;

            var respuesta = _estadofichaLN.Buscar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return NotFound(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EstadoFichaController.Buscar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/estadoficha ───────────────────────────────────────
    // Inserta un nuevo registro.
    // Los datos vienen del body del request en formato JSON.
    // La LN valida todos los campos antes de guardar en BD.
    [HttpPost]
    public IActionResult Insertar([FromBody] TEstadoFicha entidad)
    {
        try
        {
            if (entidad == null)
                return BadRequest("Los datos son requeridos.");

            var respuesta = _estadofichaLN.Insertar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            // 201 Created es más semántico que 200 OK para inserciones exitosas
            return CreatedAtAction(nameof(Buscar),
                new { id = entidad.EfId }, respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EstadoFichaController.Insertar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── PUT /api/estadoficha ────────────────────────────────────────
    // Modifica un registro existente.
    // El ID debe venir en el body — la LN verifica que exista en BD.
    [HttpPut]
    public IActionResult Modificar([FromBody] TEstadoFicha entidad)
    {
        try
        {
            if (entidad == null)
                return BadRequest("Los datos son requeridos.");

            var respuesta = _estadofichaLN.Modificar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EstadoFichaController.Modificar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── DELETE /api/estadoficha/{id} ────────────────────────────────
    // Elimina un registro por su ID (EfId).
    // La LN verifica que exista antes de intentar eliminar.
    [HttpDelete("{id}")]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var entidad = new TEstadoFicha();
            entidad.EfId = id;

            var respuesta = _estadofichaLN.Eliminar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EstadoFichaController.Eliminar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}

