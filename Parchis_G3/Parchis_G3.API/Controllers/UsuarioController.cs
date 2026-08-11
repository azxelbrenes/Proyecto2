using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parchis_G3.API.Services;
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

    // ================================================================
    // DTOs de entrada
    // ================================================================
    // Van acá dentro por simplicidad. No se reciben entidades porque
    // TUsuario exige campos que el cliente no tiene por qué mandar, y
    // porque exponerla dejaría que alguien enviara UsuMonedasTotal.

    public class CambiarPasswordRequest
    {
        public string PasswordActual { get; set; } = string.Empty;
        public string PasswordNueva { get; set; } = string.Empty;
    }

    public class PreferenciasRequest
    {
        public bool SonidosActivos { get; set; }
        public bool MusicaActiva { get; set; }
        public bool NotificacionesActivas { get; set; }
    }

    public class PerfilRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public int Avatar { get; set; }
    }

    // ── GET /api/usuario ─────────────────────────────────────────
    // Devuelve el perfil del usuario autenticado. El ID sale del JWT:
    // el cliente no debería decirnos quién es, lo sabemos del token.
    [HttpGet]
    public IActionResult ObtenerPerfil()
    {
        try
        {
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
    // Actualiza los datos del perfil. El ID se fuerza desde el JWT,
    // así el cliente no puede modificar a otro usuario.
    [HttpPut]
    public IActionResult Modificar([FromBody] TUsuario usuario)
    {
        try
        {
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

    // ── PUT /api/usuario/perfil (RF-15) ──────────────────────────
    // Actualiza solo nombre y avatar. A diferencia de Modificar, no
    // recibe la entidad completa: así el cliente no puede tocar
    // monedas ni el hash de la contraseña.
    [HttpPut("perfil")]
    public IActionResult ActualizarPerfil([FromBody] PerfilRequest datos)
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0) return Unauthorized("Token inválido.");

            if (string.IsNullOrWhiteSpace(datos.Nombre) || datos.Nombre.Trim().Length < 2)
                return BadRequest(new { strMensajeRespuesta = "El nombre debe tener al menos 2 caracteres." });

            if (datos.Nombre.Trim().Length > 100)
                return BadRequest(new { strMensajeRespuesta = "El nombre no puede superar los 100 caracteres." });

            // Traemos el usuario completo y solo tocamos lo permitido
            var actual = _usuarioLN.Buscar(new TUsuario { UsuId = usuId });
            if (!actual.blnIndicadorTransaccion)
                return NotFound(actual);

            var usuario = actual.ValorRetorno!;
            usuario.UsuNombre = datos.Nombre.Trim();

            if (datos.Avatar > 0)
                usuario.UsuAvatar = datos.Avatar;

            var respuesta = _usuarioLN.Modificar(usuario);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioController.ActualizarPerfil");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── PUT /api/usuario/preferencias (RF-15) ────────────────────
    // Sonidos, música y notificaciones.
    [HttpPut("preferencias")]
    public IActionResult ActualizarPreferencias([FromBody] PreferenciasRequest datos)
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0) return Unauthorized("Token inválido.");

            var actual = _usuarioLN.Buscar(new TUsuario { UsuId = usuId });
            if (!actual.blnIndicadorTransaccion)
                return NotFound(actual);

            var usuario = actual.ValorRetorno!;
            usuario.UsuSonidosActivos = datos.SonidosActivos;
            usuario.UsuMusicaActiva = datos.MusicaActiva;
            usuario.UsuNotificacionesActivas = datos.NotificacionesActivas;

            var respuesta = _usuarioLN.Modificar(usuario);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioController.ActualizarPreferencias");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── PUT /api/usuario/password (RF-15) ────────────────────────
    // Cambio de contraseña. Se pide la actual y se verifica contra el
    // hash guardado: sin eso, quien robara un token podría cambiarla
    // sin conocer la contraseña original.
    [HttpPut("password")]
    public IActionResult CambiarPassword([FromBody] CambiarPasswordRequest datos)
    {
        try
        {
            var usuId = _jwtService.ObtenerUsuIdDesdeToken(User);
            if (usuId <= 0) return Unauthorized("Token inválido.");

            if (string.IsNullOrWhiteSpace(datos.PasswordActual) ||
                string.IsNullOrWhiteSpace(datos.PasswordNueva))
                return BadRequest(new { strMensajeRespuesta = "Ambas contraseñas son requeridas." });

            if (datos.PasswordNueva.Length < 8)
                return BadRequest(new { strMensajeRespuesta = "La nueva contraseña debe tener al menos 8 caracteres." });

            if (datos.PasswordActual == datos.PasswordNueva)
                return BadRequest(new { strMensajeRespuesta = "La nueva contraseña debe ser distinta de la actual." });

            var actual = _usuarioLN.Buscar(new TUsuario { UsuId = usuId });
            if (!actual.blnIndicadorTransaccion)
                return NotFound(actual);

            var usuario = actual.ValorRetorno!;

            // Verificamos la contraseña actual contra el hash guardado
            bool passwordValida = BCrypt.Net.BCrypt.Verify(datos.PasswordActual, usuario.UsuPasswordHash);

            if (!passwordValida)
                return BadRequest(new { strMensajeRespuesta = "La contraseña actual no es correcta." });

            // workFactor 12, el mismo del registro en AuthController
            usuario.UsuPasswordHash = BCrypt.Net.BCrypt.HashPassword(datos.PasswordNueva, workFactor: 12);

            var respuesta = _usuarioLN.Modificar(usuario);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            return Ok(new { strMensajeRespuesta = "Contraseña actualizada correctamente." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioController.CambiarPassword");
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── DELETE /api/usuario ──────────────────────────────────────
    // Elimina la cuenta del usuario autenticado.
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
    // Endpoint administrativo. Pendiente restringirlo a rol ADMIN.
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