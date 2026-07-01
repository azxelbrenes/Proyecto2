using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/turnospartida")]
public class TurnosPartidaController : ControllerBase
{
    // La LN se inyecta automáticamente por el contenedor de dependencias
    // configurado en Program.cs — no usamos "new" directamente
    private readonly ITurnosPartidaLN _turnospartidaLN;
    private readonly ILogger<TurnosPartidaController> _logger;

    public TurnosPartidaController(ITurnosPartidaLN turnospartidaLN, ILogger<TurnosPartidaController> logger)
    {
        _turnospartidaLN = turnospartidaLN;
        _logger = logger;
    }

    // ── GET /api/turnospartida ────────────────────────────────────────
    // Devuelve todos los registros. La LN aplica los includes necesarios.
    [HttpGet]
    public IActionResult Listar()
    {
        try
        {
            var respuesta = _turnospartidaLN.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TurnosPartidaController.Listar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/turnospartida/{id} ───────────────────────────────────
    // Busca un registro específico por su ID (TurId).
    [HttpGet("{id}")]
    public IActionResult Buscar(int id)
    {
        try
        {
            // Creamos el objeto tipado con solo el PK para que la LN lo busque
            var entidad = new TTurnosPartidum();
            entidad.TurId = id;

            var respuesta = _turnospartidaLN.Buscar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return NotFound(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TurnosPartidaController.Buscar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/turnospartida ───────────────────────────────────────
    // Inserta un nuevo registro.
    // Los datos vienen del body del request en formato JSON.
    // La LN valida todos los campos antes de guardar en BD.
    [HttpPost]
    public IActionResult Insertar([FromBody] TTurnosPartidum entidad)
    {
        try
        {
            if (entidad == null)
                return BadRequest("Los datos son requeridos.");

            var respuesta = _turnospartidaLN.Insertar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            // 201 Created es más semántico que 200 OK para inserciones exitosas
            return CreatedAtAction(nameof(Buscar),
                new { id = entidad.TurId }, respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TurnosPartidaController.Insertar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── PUT /api/turnospartida ────────────────────────────────────────
    // Modifica un registro existente.
    // El ID debe venir en el body — la LN verifica que exista en BD.
    [HttpPut]
    public IActionResult Modificar([FromBody] TTurnosPartidum entidad)
    {
        try
        {
            if (entidad == null)
                return BadRequest("Los datos son requeridos.");

            var respuesta = _turnospartidaLN.Modificar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TurnosPartidaController.Modificar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── DELETE /api/turnospartida/{id} ────────────────────────────────
    // Elimina un registro por su ID (TurId).
    // La LN verifica que exista antes de intentar eliminar.
    [HttpDelete("{id}")]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var entidad = new TTurnosPartidum();
            entidad.TurId = id;

            var respuesta = _turnospartidaLN.Eliminar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TurnosPartidaController.Eliminar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
