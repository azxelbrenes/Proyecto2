using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Services;
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

    // ================================================================
    // DTO de entrada
    // ================================================================
    // Va acá adentro para no tener que crear archivos nuevos ni tocar
    // otros proyectos. Cuando haya tiempo conviene moverlo a
    // Dominio/DTO/, pero funciona igual.
    //
    // El motivo de existir: el endpoint recibía [FromBody] TSala, y
    // TSala tiene SalNombre y SalEstado como string no-nullable. Con
    // nullable reference types activado, ASP.NET los exige en el body
    // y devuelve 400 antes de entrar al método. El cliente solo manda
    // { "salId": 1 }, así que la validación fallaba siempre.
    public class UnirseSalaRequest
    {
        public int SalId { get; set; }
    }

    // ── GET /api/sala ────────────────────────────────────────────
    // Lista todas las salas activas.
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
    // Cobra la entrada de la sala al jugador.
    //
    // El servidor SIEMPRE lee el costo real desde la BD. Nunca se
    // confía en un precio que venga del cliente.
    [HttpPost("unirse")]
    public IActionResult Unirse([FromBody] UnirseSalaRequest datos)
    {
        try
        {
            if (datos == null || datos.SalId <= 0)
                return BadRequest(new { mensaje = "Debés indicar una sala válida." });

            // El usuario sale del token, nunca del body: si viniera del
            // body, cualquiera podría gastarle las monedas a otro.
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0)
                return Unauthorized(new { mensaje = "Token inválido." });

            var salaReal = _salaLN.Buscar(new TSala { SalId = datos.SalId });
            if (!salaReal.blnIndicadorTransaccion)
                return NotFound(new { mensaje = "Sala no encontrada." });

            var sala = salaReal.ValorRetorno!;

            var usuarioResp = _usuarioLN.Buscar(new TUsuario { UsuId = usuId });
            if (!usuarioResp.blnIndicadorTransaccion)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            var usuario = usuarioResp.ValorRetorno!;

            // RF-14: bloqueo temporal por abandonar 3 partidas seguidas
            if (usuario.UsuBloqueado)
                return BadRequest(new
                {
                    mensaje = "Tu cuenta está bloqueada temporalmente por abandonar partidas."
                });

            // RF-02: verificar saldo suficiente
            if (usuario.UsuMonedasTotal < sala.SalCostoEntrada)
                return BadRequest(new
                {
                    mensaje = $"Saldo insuficiente. Necesitás {sala.SalCostoEntrada} monedas y tenés {usuario.UsuMonedasTotal}.",
                    saldoActual = usuario.UsuMonedasTotal,
                    costoEntrada = sala.SalCostoEntrada
                });

            // Descontamos la entrada
            usuario.UsuMonedasTotal -= sala.SalCostoEntrada;

            var modificar = _usuarioLN.Modificar(usuario);
            if (!modificar.blnIndicadorTransaccion)
                return BadRequest(new { mensaje = "No se pudo descontar la entrada." });

            return Ok(new
            {
                mensaje = $"Te uniste a {sala.SalNombre}.",
                salaId = sala.SalId,
                salaNombre = sala.SalNombre,
                costoEntrada = sala.SalCostoEntrada,
                monedas = usuario.UsuMonedasTotal
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SalaController.Unirse");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}