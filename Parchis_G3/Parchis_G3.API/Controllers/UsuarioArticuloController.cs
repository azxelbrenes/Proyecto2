using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/usuarioarticulo")]
public class UsuarioArticuloController : ControllerBase
{
    // La LN se inyecta automáticamente por el contenedor de dependencias
    // configurado en Program.cs — no usamos "new" directamente
    private readonly IUsuarioArticuloLN _usuarioarticuloLN;
    private readonly ILogger<UsuarioArticuloController> _logger;

    public UsuarioArticuloController(IUsuarioArticuloLN usuarioarticuloLN, ILogger<UsuarioArticuloController> logger)
    {
        _usuarioarticuloLN = usuarioarticuloLN;
        _logger = logger;
    }

    // ── GET /api/usuarioarticulo ────────────────────────────────────────
    // Devuelve todos los registros. La LN aplica los includes necesarios.
    [HttpGet]
    public IActionResult Listar()
    {
        try
        {
            var respuesta = _usuarioarticuloLN.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioArticuloController.Listar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/usuarioarticulo/{id} ───────────────────────────────────
    // Busca un registro específico por su ID (UartId).
    [HttpGet("{id}")]
    public IActionResult Buscar(int id)
    {
        try
        {
            // Creamos el objeto tipado con solo el PK para que la LN lo busque
            var entidad = new TUsuarioArticulo();
            entidad.UartId = id;

            var respuesta = _usuarioarticuloLN.Buscar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return NotFound(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioArticuloController.Buscar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/usuarioarticulo ───────────────────────────────────────
    // Inserta un nuevo registro.
    // Los datos vienen del body del request en formato JSON.
    // La LN valida todos los campos antes de guardar en BD.
    [HttpPost]
    public IActionResult Insertar([FromBody] TUsuarioArticulo entidad)
    {
        try
        {
            if (entidad == null)
                return BadRequest("Los datos son requeridos.");

            var respuesta = _usuarioarticuloLN.Insertar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            // 201 Created es más semántico que 200 OK para inserciones exitosas
            return CreatedAtAction(nameof(Buscar),
                new { id = entidad.UartId }, respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioArticuloController.Insertar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── PUT /api/usuarioarticulo ────────────────────────────────────────
    // Modifica un registro existente.
    // El ID debe venir en el body — la LN verifica que exista en BD.
    [HttpPut]
    public IActionResult Modificar([FromBody] TUsuarioArticulo entidad)
    {
        try
        {
            if (entidad == null)
                return BadRequest("Los datos son requeridos.");

            var respuesta = _usuarioarticuloLN.Modificar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioArticuloController.Modificar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── DELETE /api/usuarioarticulo/{id} ────────────────────────────────
    // Elimina un registro por su ID (UartId).
    // La LN verifica que exista antes de intentar eliminar.
    [HttpDelete("{id}")]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var entidad = new TUsuarioArticulo();
            entidad.UartId = id;

            var respuesta = _usuarioarticuloLN.Eliminar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioArticuloController.Eliminar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
