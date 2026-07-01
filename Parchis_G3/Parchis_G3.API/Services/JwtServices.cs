using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Parchis_G3.Dominio.EntidadesTipadas;

namespace Parchis_G3.API.Services;

public class JwtService
{
    // La clave secreta viene del appsettings.json
    // Nunca debe estar hardcodeada en el código
    private readonly string _key;

    public JwtService(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada.");
    }

    // GenerarToken 
    // Genera un JWT con los datos del usuario.
    // El token expira en 15 minutos por seguridad — si alguien
    // lo roba, solo lo puede usar por ese tiempo.
    public string GenerarToken(TUsuario usuario)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        // Convertimos la clave secreta a bytes para firmar el token
        var key = Encoding.ASCII.GetBytes(_key);

        // Claims: datos que viajan dentro del token
        // Android puede leer estos datos sin necesidad de ir a la BD
        var claims = new List<Claim>
        {
            // Identificador único del usuario
            new Claim("UsuId",    usuario.UsuId.ToString()),
            new Claim("nombre",   usuario.UsuNombre),
            new Claim("correo",   usuario.UsuCorreo),

            // ClaimTypes.NameIdentifier es el estándar para el ID principal
            new Claim(ClaimTypes.NameIdentifier, usuario.UsuId.ToString()),
            new Claim(ClaimTypes.Name,           usuario.UsuNombre),
            new Claim(ClaimTypes.Email,          usuario.UsuCorreo),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),

            // El token expira en 15 minutos
            // Si necesita más tiempo usás refresh tokens (se agrega después)
            Expires = DateTime.UtcNow.AddMinutes(15),

            // Firmamos con HMACSHA256 — algoritmo seguro y estándar
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    // ObtenerUsuIdDesdeToken
    // Extrae el ID del usuario desde el token ya validado.
    // Se usa en los controllers para saber quién hizo el request.
    public int ObtenerUsuIdDesdeToken(ClaimsPrincipal usuario)
    {
        var claim = usuario.FindFirst("UsuId")?.Value;
        return int.TryParse(claim, out int id) ? id : 0;
    }
}