using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Parchis_G3.API.Servicios;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesLN;

namespace Parchis_G3.API.Controllers;

[AllowAnonymous] // Sin JWT — cualquiera puede llegar aquí
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // Inyectamos la LogicaNegocios de Usuario y el servicio JWT
    private readonly IUsuarioLN _usuarioLN;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUsuarioLN usuarioLN, JwtService jwtService, ILogger<AuthController> logger)
    {
        _usuarioLN = usuarioLN;
        _jwtService = jwtService;
        _logger = logger;
    }

    // Registra un nuevo jugador.
    // IMPORTANTE: la contraseña llega en texto plano desde Android
    // y aquí la hasheamos con BCrypt ANTES de pasarla a la LN.
    // La LN nunca ve la contraseña real — solo el hash.
    [HttpPost("registro")]
    public IActionResult Registro([FromBody] TUsuario usuario)
    {
        try
        {
            if (usuario == null)
                return BadRequest("Los datos del usuario son requeridos.");

            // Hasheamos la contraseña aquí en la API
            // BCrypt.HashPassword genera un hash seguro con salt automático
            // El número 12 es el "work factor" — entre más alto, más lento de hackear
            usuario.UsuPasswordHash = BCrypt.Net.BCrypt.HashPassword(
                usuario.UsuPasswordHash, workFactor: 12
            );

            var respuesta = _usuarioLN.Insertar(usuario);

            if (!respuesta.blnIndicadorTransaccion)
                return BadRequest(respuesta);

            // Al registrarse, generamos el token inmediatamente
            // para que no tenga que hacer login por separado
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
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // ── POST /api/auth/login ─────────────────────────────────────
    // Autentica un usuario y devuelve su JWT.
    // Recibe correo y contraseña, valida con BCrypt y genera el token.
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Correo) ||
                string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("El correo y la contraseña son requeridos.");

            // Buscamos el usuario por correo para obtener su hash almacenado
            var buscar = _usuarioLN.Obtener(new TUsuario { UsuCorreo = request.Correo });

            if (!buscar.blnIndicadorTransaccion || !buscar.ValorRetorno!.Any())
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });

            var usuario = buscar.ValorRetorno!.First();

            // Verificamos la contraseña contra el hash guardado en BD
            // BCrypt.Verify compara el texto plano con el hash almacenado
            bool passwordValida = BCrypt.Net.BCrypt.Verify(request.Password, usuario.UsuPasswordHash);

            if (!passwordValida)
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });

            // Verificamos que la cuenta no esté bloqueada por abandonos
            if (usuario.UsuBloqueado && usuario.UsuFechaDesbloqueo > DateTime.Now)
                return Unauthorized(new
                {
                    mensaje = $"Cuenta bloqueada hasta {usuario.UsuFechaDesbloqueo:dd/MM/yyyy HH:mm}"
                });

            // Todo correcto — generamos el JWT y lo devolvemos
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
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}

// ── DTO para el Login ────────────────────────────────────────────
// Clase simple para recibir solo correo y contraseña en el login.
// No usamos TUsuario completo porque solo necesitamos esos 2 campos.
public class LoginRequest
{
    public string Correo { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
