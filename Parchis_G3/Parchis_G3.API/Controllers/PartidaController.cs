using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Servicios;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/partida")]
public class PartidaController : ControllerBase
{
    private readonly IPartidaLN _partidaLN;
    private readonly JwtService _jwtService;
    private readonly ILogger<PartidaController> _logger;

    public PartidaController(IPartidaLN partidaLN, JwtService jwtService, ILogger<PartidaController> logger)
    {
        _partidaLN = partidaLN;
        _jwtService = jwtService;
        _logger = logger;
    }

    // ── GET /api/partida ────────────────────────────────────────
    // Devuelve todos los registros disponibles.
    [HttpGet]
    public IActionResult Listar()
    {
        try
        {
            var respuesta = _partidaLN.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PartidaController.Listar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/partida/{id} ───────────────────────────────────
    // Busca un registro específico por su ID.
    [HttpGet("{id}")]
    public IActionResult Buscar(int id)
    {
        try
        {
            var entidad = new TPartida();
            entidad.ParId = id;
            var respuesta = _partidaLN.Buscar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return NotFound(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PartidaController.Buscar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/partida ───────────────────────────────────────
    // Inserta un nuevo registro. La LN valida los datos antes de guardar.
    [HttpPost]
    public IActionResult Insertar([FromBody] TPartida entidad)
    {
        try
        {
            var respuesta = _partidaLN.Insertar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PartidaController.Insertar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── PUT /api/partida ────────────────────────────────────────
    // Modifica un registro existente.
    [HttpPut]
    public IActionResult Modificar([FromBody] TPartida entidad)
    {
        try
        {
            var respuesta = _partidaLN.Modificar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PartidaController.Modificar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── DELETE /api/partida/{id} ────────────────────────────────
    // Elimina un registro por su ID.
    [HttpDelete("{id}")]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var entidad = new TPartida();
            entidad.ParId = id;
            var respuesta = _partidaLN.Eliminar(entidad);
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PartidaController.Eliminar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }


    // ── GET /api/partida/activa ──────────────────────────────────
    // Devuelve la partida activa del jugador autenticado (si tiene una).
    // Android lo usa al reconectarse para retomar la partida.
    [HttpGet("activa")]
    public IActionResult ObtenerActiva()
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0) return Unauthorized("Token inválido.");

            // Buscamos partidas en estado EN_JUEGO — si el jugador está en una
            var respuesta = _partidaLN.Obtener(new TPartida { ParEstado = "EN_JUEGO" });
            if (!respuesta.blnIndicadorTransaccion)
                return NotFound(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PartidaController.ObtenerActiva");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
