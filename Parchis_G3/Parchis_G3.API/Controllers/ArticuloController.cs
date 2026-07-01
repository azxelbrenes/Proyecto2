using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Servicios;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ArticuloController : ControllerBase
{
    private readonly IArticuloLN _articuloLN;
    private readonly IUsuarioLN _usuarioLN;
    private readonly IUsuarioArticuloLN _usuarioArticuloLN;
    private readonly JwtService _jwtService;
    private readonly ILogger<ArticuloController> _logger;

    public ArticuloController(
        IArticuloLN articuloLN,
        IUsuarioLN usuarioLN,
        IUsuarioArticuloLN usuarioArticuloLN,
        JwtService jwtService,
        ILogger<ArticuloController> logger)
    {
        _articuloLN = articuloLN;
        _usuarioLN = usuarioLN;
        _usuarioArticuloLN = usuarioArticuloLN;
        _jwtService = jwtService;
        _logger = logger;
    }

    // ── GET /api/articulo ────────────────────────────────────────
    // Lista todos los artículos activos de la tienda.
    [HttpGet]
    public IActionResult Listar()
    {
        try
        {
            var respuesta = _articuloLN.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ArticuloController.Listar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/articulo/{id} ───────────────────────────────────
    // Obtiene un artículo por su ID para mostrar el detalle.
    [HttpGet("{id}")]
    public IActionResult Buscar(int id)
    {
        try
        {
            var respuesta = _articuloLN.Buscar(new TArticulo { ArtId = id });
            if (!respuesta.blnIndicadorTransaccion)
                return NotFound(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ArticuloController.Buscar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/articulo/tipo/{tipId} ───────────────────────────
    // Filtra artículos por tipo: 1=Ficha, 2=Tablero, 3=Dado.
    // Android lo usa para mostrar las pestañas de la tienda.
    [HttpGet("tipo/{tipId}")]
    public IActionResult ObtenerPorTipo(int tipId)
    {
        try
        {
            var respuesta = _articuloLN.Obtener(
                new TArticulo { TipId = tipId, ArtEstado = "A" }
            );
            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ArticuloController.ObtenerPorTipo");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/articulo/comprar ───────────────────────────────
    // El jugador compra un artículo de la tienda con sus monedas.
    // El servidor verifica el precio real — nunca el del cliente.
    [HttpPost("comprar")]
    public IActionResult Comprar([FromBody] TArticulo articulo)
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0)
                return Unauthorized("Token inválido.");

            // Precio real desde BD — el cliente no puede alterarlo
            var artReal = _articuloLN.Buscar(new TArticulo { ArtId = articulo.ArtId });
            if (!artReal.blnIndicadorTransaccion)
                return NotFound("Artículo no encontrado.");

            // Saldo actual del jugador
            var usuario = _usuarioLN.Buscar(new TUsuario { UsuId = usuId });
            if (!usuario.blnIndicadorTransaccion)
                return NotFound("Usuario no encontrado.");

            // Verificamos que tenga monedas suficientes
            if (usuario.ValorRetorno!.UsuMonedasTotal < artReal.ValorRetorno!.ArtPrecio)
                return BadRequest(new { mensaje = "Saldo insuficiente para comprar este artículo." });

            // Registramos el artículo como desbloqueado para el usuario
            var uaResult = _usuarioArticuloLN.Insertar(new TUsuarioArticulo
            {
                UsuId = usuId,
                ArtId = artReal.ValorRetorno.ArtId,
                UartFechaCompra = DateTime.Now
            });

            if (!uaResult.blnIndicadorTransaccion)
                return BadRequest(uaResult);

            // Descontamos las monedas
            usuario.ValorRetorno.UsuMonedasTotal -= artReal.ValorRetorno.ArtPrecio;
            _usuarioLN.Modificar(usuario.ValorRetorno);

            return Ok(new
            {
                mensaje = $"Compraste {artReal.ValorRetorno.ArtNombre} exitosamente.",
                monedas = usuario.ValorRetorno.UsuMonedasTotal
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ArticuloController.Comprar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
