using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/mensajeschat")]
public class MensajesChatController : ControllerBase
{
    // La LN se inyecta automáticamente por el contenedor de dependencias
    // configurado en Program.cs — no usamos "new" directamente
    private readonly IMensajesChatLN _mensajeschatLN;
    private readonly ILogger<MensajesChatController> _logger;

    public MensajesChatController(IMensajesChatLN mensajeschatLN, ILogger<MensajesChatController> logger)
    {
        _mensajeschatLN = mensajeschatLN;
        _logger = logger;
    }

    // ── GET /api/mensajeschat ────────────────────────────────────────
    // Devuelve todos los registros. La LN aplica los includes necesarios.
    [HttpGet]
    public IActionResult Listar()
    {
        try
        {
            var respuesta = _mensajeschatLN.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MensajesChatController.Listar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/mensajeschat/{id} ───────────────────────────────────
    // Busca un registro específico por su ID (McId).
    [HttpGet("{id}")]
    public IActionResult Buscar(int id)
    {
        try
        {
            // Creamos el objeto tipado con solo el PK para que la LN lo busque
            var entidad = new TMensajesChat();
            entidad.McId = id;

            var respuesta = _mensajeschatLN.Buscar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return NotFound(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MensajesChatController.Buscar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/mensajeschat ───────────────────────────────────────
    // Inserta un nuevo registro.
    // Los datos vienen del body del request en formato JSON.
    // La LN valida todos los campos antes de guardar en BD.
    [HttpPost]
    public IActionResult Insertar([FromBody] TMensajesChat entidad)
    {
        try
        {
            if (entidad == null)
                return BadRequest("Los datos son requeridos.");

            var respuesta = _mensajeschatLN.Insertar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            // 201 Created es más semántico que 200 OK para inserciones exitosas
            return CreatedAtAction(nameof(Buscar),
                new { id = entidad.McId }, respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MensajesChatController.Insertar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── PUT /api/mensajeschat ────────────────────────────────────────
    // Modifica un registro existente.
    // El ID debe venir en el body — la LN verifica que exista en BD.
    [HttpPut]
    public IActionResult Modificar([FromBody] TMensajesChat entidad)
    {
        try
        {
            if (entidad == null)
                return BadRequest("Los datos son requeridos.");

            var respuesta = _mensajeschatLN.Modificar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MensajesChatController.Modificar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── DELETE /api/mensajeschat/{id} ────────────────────────────────
    // Elimina un registro por su ID (McId).
    // La LN verifica que exista antes de intentar eliminar.
    [HttpDelete("{id}")]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var entidad = new TMensajesChat();
            entidad.McId = id;

            var respuesta = _mensajeschatLN.Eliminar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MensajesChatController.Eliminar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
