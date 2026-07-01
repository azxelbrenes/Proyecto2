using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Servicios;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalaController : ControllerBase
{
    private readonly ISalaLN _salaLN;
    private readonly IUsuarioLN _usuarioLN;
    private readonly JwtService _jwtService;
    private readonly ILogger<SalaController> _logger;

    public SalaController(ISalaLN salaLN, IUsuarioLN usuarioLN, JwtService jwtService, ILogger<SalaController> logger)
    {
        _salaLN = salaLN;
        _usuarioLN = usuarioLN;
        _jwtService = jwtService;
        _logger = logger;
    }

    // ── GET /api/sala ────────────────────────────────────────────
    // Lista todas las salas activas.
    // Android usa esto para mostrar las 5 opciones al jugador.
    [HttpGet]
    public IActionResult Listar()
    {
        try
        {
            var respuesta = _salaLN.Listar();

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SalaController.Listar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/sala/{id} ───────────────────────────────────────
    // Obtiene los detalles de una sala específica por su ID.
    [HttpGet("{id}")]
    public IActionResult Buscar(int id)
    {
        try
        {
            var respuesta = _salaLN.Buscar(new TSala { SalId = id });

            if (!respuesta.blnIndicadorTransaccion)
                return NotFound(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SalaController.Buscar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/sala/unirse ────────────────────────────────────
    // El jugador intenta unirse a una sala.
    // Aquí validamos que tenga monedas suficientes para la entrada.
    // IMPORTANTE: el servidor siempre verifica el precio real en BD —
    // nunca confiamos en el precio que mande el cliente.
    [HttpPost("unirse")]
    public IActionResult Unirse([FromBody] TSala sala)
    {
        try
        {
            // Obtenemos el ID del jugador desde el token
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0)
                return Unauthorized("Token inválido.");

            // Buscamos la sala en BD para obtener el precio REAL
            // Nunca usamos el precio que manda el cliente — podrían manipularlo
            var salaReal = _salaLN.Buscar(new TSala { SalId = sala.SalId });
            if (!salaReal.blnIndicadorTransaccion)
                return NotFound("Sala no encontrada.");

            // Verificamos el saldo actual del jugador
            var usuario = _usuarioLN.Buscar(new TUsuario { UsuId = usuId });
            if (!usuario.blnIndicadorTransaccion)
                return NotFound("Usuario no encontrado.");

            // Comparamos monedas disponibles vs costo de entrada
            if (usuario.ValorRetorno!.UsuMonedasTotal < salaReal.ValorRetorno!.SalCostoEntrada)
                return BadRequest(new
                {
                    mensaje = "Saldo insuficiente para unirse a esta sala.",
                    saldoActual = usuario.ValorRetorno.UsuMonedasTotal,
                    costoEntrada = salaReal.ValorRetorno.SalCostoEntrada
                });

            // Descontamos las monedas del jugador
            usuario.ValorRetorno.UsuMonedasTotal -= salaReal.ValorRetorno.SalCostoEntrada;
            _usuarioLN.Modificar(usuario.ValorRetorno);

            return Ok(new
            {
                mensaje = $"Te uniste a {salaReal.ValorRetorno.SalNombre} exitosamente.",
                salaId = salaReal.ValorRetorno.SalId,
                monedas = usuario.ValorRetorno.UsuMonedasTotal
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SalaController.Unirse");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
