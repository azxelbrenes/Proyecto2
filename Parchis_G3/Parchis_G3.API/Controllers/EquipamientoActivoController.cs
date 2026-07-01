using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/equipamientoactivo")]
public class EquipamientoActivoController : ControllerBase
{
    // La LN se inyecta automáticamente por el contenedor de dependencias
    // configurado en Program.cs — no usamos "new" directamente
    private readonly IEquipamientoActivoLN _equipamientoactivoLN;
    private readonly ILogger<EquipamientoActivoController> _logger;

    public EquipamientoActivoController(IEquipamientoActivoLN equipamientoactivoLN, ILogger<EquipamientoActivoController> logger)
    {
        _equipamientoactivoLN = equipamientoactivoLN;
        _logger = logger;
    }

    // ── GET /api/equipamientoactivo ────────────────────────────────────────
    // Devuelve todos los registros. La LN aplica los includes necesarios.
    [HttpGet]
    public IActionResult Listar()
    {
        try
        {
            var respuesta = _equipamientoactivoLN.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EquipamientoActivoController.Listar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/equipamientoactivo/{id} ───────────────────────────────────
    // Busca un registro específico por su ID (EquId).
    [HttpGet("{id}")]
    public IActionResult Buscar(int id)
    {
        try
        {
            // Creamos el objeto tipado con solo el PK para que la LN lo busque
            var entidad = new TEquipamientoActivo();
            entidad.EquId = id;

            var respuesta = _equipamientoactivoLN.Buscar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return NotFound(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EquipamientoActivoController.Buscar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/equipamientoactivo ───────────────────────────────────────
    // Inserta un nuevo registro.
    // Los datos vienen del body del request en formato JSON.
    // La LN valida todos los campos antes de guardar en BD.
    [HttpPost]
    public IActionResult Insertar([FromBody] TEquipamientoActivo entidad)
    {
        try
        {
            if (entidad == null)
                return BadRequest("Los datos son requeridos.");

            var respuesta = _equipamientoactivoLN.Insertar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            // 201 Created es más semántico que 200 OK para inserciones exitosas
            return CreatedAtAction(nameof(Buscar),
                new { id = entidad.EquId }, respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EquipamientoActivoController.Insertar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── PUT /api/equipamientoactivo ────────────────────────────────────────
    // Modifica un registro existente.
    // El ID debe venir en el body — la LN verifica que exista en BD.
    [HttpPut]
    public IActionResult Modificar([FromBody] TEquipamientoActivo entidad)
    {
        try
        {
            if (entidad == null)
                return BadRequest("Los datos son requeridos.");

            var respuesta = _equipamientoactivoLN.Modificar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EquipamientoActivoController.Modificar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── DELETE /api/equipamientoactivo/{id} ────────────────────────────────
    // Elimina un registro por su ID (EquId).
    // La LN verifica que exista antes de intentar eliminar.
    [HttpDelete("{id}")]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var entidad = new TEquipamientoActivo();
            entidad.EquId = id;

            var respuesta = _equipamientoactivoLN.Eliminar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EquipamientoActivoController.Eliminar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}