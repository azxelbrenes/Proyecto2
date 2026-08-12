using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Services;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
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
    private readonly IInventarioLN _inventarioLN;
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly JwtService _jwtService;
    private readonly ILogger<ArticuloController> _logger;

    public ArticuloController(
        IArticuloLN articuloLN,
        IUsuarioLN usuarioLN,
        IUsuarioArticuloLN usuarioArticuloLN,
        IInventarioLN inventarioLN,
        IUnidadTrabajoEF unidadTrabajo,
        JwtService jwtService,
        ILogger<ArticuloController> logger)
    {
        _articuloLN = articuloLN;
        _usuarioLN = usuarioLN;
        _usuarioArticuloLN = usuarioArticuloLN;
        _inventarioLN = inventarioLN;
        _unidadTrabajo = unidadTrabajo;
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
    // La tienda lo usa para las pestañas de categoría.
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
    //
    // ¿POR QUÉ RECIBE UN DTO Y NO UN TArticulo COMPLETO?
    // Antes este endpoint recibía [FromBody] TArticulo, y ASP.NET
    // Core rechazaba el request con error 400 porque TArticulo
    // exige ArtNombre y ArtEstado, que el cliente no manda.
    //
    // Y hace bien en no mandarlos: si el cliente enviara el nombre
    // y el precio, alguien podría manipularlos. El servidor SIEMPRE
    // busca el precio real en la base de datos usando solo el ID.
    [HttpPost("comprar")]
    public IActionResult Comprar([FromBody] ComprarArticuloRequest request)
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0)
                return Unauthorized("Token inválido.");

            if (request == null || request.ArtId <= 0)
                return BadRequest(new { mensaje = "El artículo es requerido." });

            // ── Evitar compras duplicadas ────────────────────────
            // Sin esta validación, comprar dos veces el mismo
            // artículo rompe el constraint UQ_UArt_UsuarioArticulo
            // de la base de datos y devuelve un error de SQL feo
            // en vez de un mensaje claro para el usuario.
            if (_inventarioLN.YaLoTiene(usuId, request.ArtId, _unidadTrabajo))
            {
                return BadRequest(new
                {
                    mensaje = "Ya tenés este artículo en tu inventario."
                });
            }

            // Precio real desde BD — el cliente no puede alterarlo
            var artReal = _articuloLN.Buscar(new TArticulo { ArtId = request.ArtId });
            if (!artReal.blnIndicadorTransaccion)
                return NotFound(new { mensaje = "Artículo no encontrado." });

            // Saldo actual del jugador
            var usuario = _usuarioLN.Buscar(new TUsuario { UsuId = usuId });
            if (!usuario.blnIndicadorTransaccion)
                return NotFound(new { mensaje = "Usuario no encontrado." });

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


// DTO de compra

// Solo recibe el ID del artículo. El nombre, el precio y el estado
// los busca el servidor en la base de datos.
//
// Este es el mismo principio que aplicamos en el matchmaking (donde
// solo se manda el SalId) y en los pagos (donde solo se manda el
// PaqueteId): el cliente identifica QUÉ quiere, el servidor decide
// CUÁNTO cuesta.
public class ComprarArticuloRequest
{
    public int ArtId { get; set; }
}