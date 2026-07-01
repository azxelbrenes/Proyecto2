using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Servicios;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[Authorize] // JWT requerido en todos los endpoints de este controller
[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioLN _usuarioLN;
    private readonly JwtService _jwtService;
    private readonly ILogger<UsuarioController> _logger;

    public UsuarioController(IUsuarioLN usuarioLN, JwtService jwtService, ILogger<UsuarioController> logger)
    {
        _usuarioLN = usuarioLN;
        _jwtService = jwtService;
        _logger = logger;
    }

    // ── GET /api/usuario ─────────────────────────────────────────
    // Devuelve el perfil del usuario que está autenticado.
    // Extraemos su ID desde el JWT para no depender del cliente.
    // (El cliente no debería decirnos su propio ID — lo sabemos del token)
    [HttpGet]
    public IActionResult ObtenerPerfil()
    {
        try
        {
            // Extraemos el ID del usuario desde el token JWT
            // Así el usuario solo puede ver SU propio perfil
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);

            if (usuId <= 0)
                return Unauthorized("Token inválido.");

            var respuesta = _usuarioLN.Buscar(new TUsuario { UsuId = usuId });

            if (!respuesta.blnIndicadorTransaccion)
                return NotFound(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioController.ObtenerPerfil");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── PUT /api/usuario ─────────────────────────────────────────
    // Actualiza los datos del perfil: nombre, avatar, preferencias.
    // El ID se toma del JWT — el cliente no puede modificar otros usuarios.
    [HttpPut]
    public IActionResult Modificar([FromBody] TUsuario usuario)
    {
        try
        {
            // Forzamos el ID desde el token para mayor seguridad
            // Aunque el cliente mande un ID diferente, lo ignoramos
            usuario.UsuId = _jwtService.ObtenerUsuIdDesdeToken(User);

            if (usuario.UsuId <= 0)
                return Unauthorized("Token inválido.");

            var respuesta = _usuarioLN.Modificar(usuario);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioController.Modificar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── DELETE /api/usuario ──────────────────────────────────────
    // Elimina la cuenta del usuario autenticado.
    // El ID viene del token — nadie puede eliminar la cuenta de otro.
    [HttpDelete]
    public IActionResult Eliminar()
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);

            if (usuId <= 0)
                return Unauthorized("Token inválido.");

            var respuesta = _usuarioLN.Eliminar(new TUsuario { UsuId = usuId });

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioController.Eliminar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── GET /api/usuario/todos ───────────────────────────────────
    // Lista todos los usuarios del sistema.
    // Endpoint administrativo — en el futuro se restringe a rol ADMIN.
    [HttpGet("todos")]
    public IActionResult Listar()
    {
        try
        {
            var respuesta = _usuarioLN.Listar();

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioController.Listar");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
