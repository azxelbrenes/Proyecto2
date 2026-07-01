using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Servicios;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/historial")]
public class HistorialPartidaController : ControllerBase
{
    private readonly IHistorialPartidaLN _historialLN;
    private readonly JwtService _jwtService;
    private readonly ILogger<HistorialPartidaController> _logger;

    public HistorialPartidaController(IHistorialPartidaLN historialLN, JwtService jwtService, ILogger<HistorialPartidaController> logger)
    {
        _historialLN = historialLN;
        _jwtService = jwtService;
        _logger = logger;
    }

    // ── GET /api/historial ────────────────────────────────────────
    // Devuelve todos los registros disponibles.
    [HttpGet]
    public IActionResult Listar()
    {
        try
        {
            var respuesta = _historialLN.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en HistorialPartidaController.Listar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/historial/{id} ───────────────────────────────────
    // Busca un registro específico por su ID.
    [HttpGet("{id}")]
    public IActionResult Buscar(int id)
    {
        try
        {
            var entidad = new THistorialPartida();
            entidad.HpId = id;
            var respuesta = _historialLN.Buscar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return NotFound(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en HistorialPartidaController.Buscar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/historial ───────────────────────────────────────
    // Inserta un nuevo registro. La LN valida los datos antes de guardar.
    [HttpPost]
    public IActionResult Insertar([FromBody] THistorialPartida entidad)
    {
        try
        {
            var respuesta = _historialLN.Insertar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en HistorialPartidaController.Insertar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── PUT /api/historial ────────────────────────────────────────
    // Modifica un registro existente.
    [HttpPut]
    public IActionResult Modificar([FromBody] THistorialPartida entidad)
    {
        try
        {
            var respuesta = _historialLN.Modificar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en HistorialPartidaController.Modificar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── DELETE /api/historial/{id} ────────────────────────────────
    // Elimina un registro por su ID.
    [HttpDelete("{id}")]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var entidad = new THistorialPartida();
            entidad.HpId = id;
            var respuesta = _historialLN.Eliminar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en HistorialPartidaController.Eliminar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }


    // ── GET /api/historial/estadisticas ─────────────────────────
    // Devuelve el resumen de estadísticas del jugador autenticado:
    // partidas jugadas, ganadas, perdidas y porcentaje de victoria.
    [HttpGet("estadisticas")]
    public IActionResult Estadisticas()
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0) return Unauthorized("Token inválido.");

            var respuesta = _historialLN.Obtener(new THistorialPartida { UsuId = usuId });
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            var lista = respuesta.ValorRetorno!.ToList();
            var jugadas = lista.Count;
            var ganadas = lista.Count(h => h.HpResultado == "VICTORIA");
            var perdidas = lista.Count(h => h.HpResultado == "DERROTA");
            var abandono = lista.Count(h => h.HpResultado == "ABANDONO");
            var porcentaje = jugadas > 0
                ? Math.Round((double)ganadas / jugadas * 100, 2)
                : 0;

            return Ok(new
            {
                jugadas,
                ganadas,
                perdidas,
                abandono,
                porcentajeVictoria = porcentaje,
                historial = lista
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en HistorialPartidaController.Estadisticas");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
