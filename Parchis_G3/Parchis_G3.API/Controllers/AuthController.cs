using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using Microsoft.AspNetCore.RateLimiting;
using Parchis_G3.API.Services;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioLN _usuarioLN;
    private readonly ISeguridadLN _seguridadLN;
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUsuarioLN usuarioLN,
        ISeguridadLN seguridadLN,
        IUnidadTrabajoEF unidadTrabajo,
        JwtService jwtService,
        ILogger<AuthController> logger)
    {
        _usuarioLN = usuarioLN;
        _seguridadLN = seguridadLN;
        _unidadTrabajo = unidadTrabajo;
        _jwtService = jwtService;
        _logger = logger;
    }

    // Obtiene la IP real del cliente, considerando proxies
    private string? ObtenerIP()
    {
        // Si hay un proxy/load balancer, la IP real viene en este header
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    // ================================================================
    // POST /api/auth/registro
    // ================================================================
    // EnableRateLimiting("registro") limita a 3 registros por hora
    // por IP — evita que un bot cree miles de cuentas.
    [HttpPost("registro")]
    [EnableRateLimiting("registro")]
    public IActionResult Registro([FromBody] TUsuario usuario)
    {
        try
        {
            if (usuario == null)
                return BadRequest(new { strMensajeRespuesta = "Los datos del usuario son requeridos." });

            // ── Validación estricta antes de tocar la BD ─────────
            var errorNombre = ValidadorInput.ValidarNombre(usuario.UsuNombre);
            if (errorNombre != null)
                return BadRequest(new { strMensajeRespuesta = errorNombre });

            var errorCorreo = ValidadorInput.ValidarCorreo(usuario.UsuCorreo);
            if (errorCorreo != null)
                return BadRequest(new { strMensajeRespuesta = errorCorreo });

            var errorPassword = ValidadorInput.ValidarPassword(usuario.UsuPasswordHash);
            if (errorPassword != null)
                return BadRequest(new { strMensajeRespuesta = errorPassword });

            // ── Sanitizar y normalizar ───────────────────────────
            usuario.UsuNombre = ValidadorInput.Sanitizar(usuario.UsuNombre);
            usuario.UsuCorreo = usuario.UsuCorreo.Trim().ToLowerInvariant();

            // ── Hash de la contraseña ────────────────────────────
            // workFactor 12 = ~250ms por hash. Suficientemente lento
            // para frenar fuerza bruta, suficientemente rápido para
            // no molestar al usuario.
            usuario.UsuPasswordHash = BCrypt.Net.BCrypt.HashPassword(usuario.UsuPasswordHash, workFactor: 12);

            var respuesta = _usuarioLN.Insertar(usuario);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            // Auditoría del registro
            _seguridadLN.RegistrarEvento("REGISTRO", usuario.UsuCorreo,
                respuesta.ValorRetorno!.UsuId, ObtenerIP(), null, _unidadTrabajo);

            var token = _jwtService.GenerarToken(respuesta.ValorRetorno!);

            return Ok(new
            {
                respuesta.strMensajeRespuesta,
                token,
                usuario = respuesta.ValorRetorno
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en AuthController.Registro");
            return StatusCode(500, new { strMensajeRespuesta = "Error interno del servidor." });
        }
    }

    // ================================================================
    // POST /api/auth/login
  
    // EnableRateLimiting("login") limita a 5 intentos por minuto
    // por IP — primera barrera contra fuerza bruta.
    // El bloqueo de cuenta es la segunda barrera.
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        try
        {
            // ── Validación de formato ────────────────────────────
            var errorCorreo = ValidadorInput.ValidarCorreo(request?.Correo);
            if (errorCorreo != null)
                return BadRequest(new { mensaje = errorCorreo });

            if (string.IsNullOrWhiteSpace(request!.Password))
                return BadRequest(new { mensaje = "La contraseña es requerida." });

            string correo = request.Correo.Trim().ToLowerInvariant();
            string? ip = ObtenerIP();

            // ── ¿Está bloqueada la cuenta? ───────────────────────
            var mensajeBloqueo = _seguridadLN.VerificarBloqueoLogin(correo, _unidadTrabajo);
            if (mensajeBloqueo != null)
                return StatusCode(429, new { mensaje = mensajeBloqueo });

            // ── Buscar el usuario ────────────────────────────────
            var buscar = _usuarioLN.Obtener(new TUsuario { UsuCorreo = correo });

            if (!buscar.blnIndicadorTransaccion || !buscar.ValorRetorno!.Any())
            {
                // Registramos el fallo pero devolvemos mensaje genérico
                _seguridadLN.RegistrarIntentoFallido(correo, ip, _unidadTrabajo);
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });
            }

            var usuario = buscar.ValorRetorno!.First();

            // ── Verificar la contraseña ──────────────────────────
            bool passwordValida = BCrypt.Net.BCrypt.Verify(request.Password, usuario.UsuPasswordHash);

            if (!passwordValida)
            {
                _seguridadLN.RegistrarIntentoFallido(correo, ip, _unidadTrabajo);
                // Mismo mensaje que si el correo no existiera —
                // no le damos pistas al atacante
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });
            }

            // ── Verificar bloqueo por abandonos (HU-19) ──────────
            if (usuario.UsuBloqueado == true && usuario.UsuFechaDesbloqueo > DateTime.Now)
            {
                return Unauthorized(new
                {
                    mensaje = $"Cuenta bloqueada hasta {usuario.UsuFechaDesbloqueo:dd/MM/yyyy HH:mm}"
                });
            }

            // ── Login correcto ───────────────────────────────────
            _seguridadLN.RegistrarLoginExitoso(usuario.UsuId, correo, ip, _unidadTrabajo);

            var token = _jwtService.GenerarToken(usuario);

            return Ok(new
            {
                mensaje = "Login exitoso.",
                token,
                usuario = new
                {
                    usuario.UsuId,
                    usuario.UsuNombre,
                    usuario.UsuCorreo,
                    usuario.UsuMonedasTotal,
                    usuario.UsuAvatar
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en AuthController.Login");
            return StatusCode(500, new { mensaje = "Error interno del servidor." });
        }
    }
}

// DTO simple para recibir correo y contraseña en el login
public class LoginRequest
{
    public string Correo { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
