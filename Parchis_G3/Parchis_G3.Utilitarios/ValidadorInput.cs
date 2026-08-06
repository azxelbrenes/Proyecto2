using System.Text.RegularExpressions;

namespace Parchis_G3.Utilitarios;

public static class ValidadorInput
{
    // ── Límites por campo (deben coincidir con la BD) ────────────
    public const int MAX_NOMBRE = 100;
    public const int MAX_CORREO = 200;
    public const int MIN_PASSWORD = 6;
    public const int MAX_PASSWORD = 100;
    public const int MAX_MENSAJE = 200;

    // ── Regex de correo ──────────────────────────────────────────
    // Compilado una sola vez por rendimiento (static readonly)
    private static readonly Regex RegexCorreo = new(
        @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    // Solo letras, números, espacios y algunos signos seguros.
    // Bloquea caracteres usados en inyecciones y XSS: < > " ' ; \
    private static readonly Regex RegexNombreSeguro = new(
        @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ0-9\s\.\-_]+$",
        RegexOptions.Compiled
    );
    // VALIDAR NOMBRE
 
    // Devuelve null si está bien, o el mensaje de error si falla.
    public static string? ValidarNombre(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return "El nombre es requerido.";

        nombre = nombre.Trim();

        if (nombre.Length < 2)
            return "El nombre debe tener al menos 2 caracteres.";

        if (nombre.Length > MAX_NOMBRE)
            return $"El nombre no puede superar los {MAX_NOMBRE} caracteres.";

        if (!RegexNombreSeguro.IsMatch(nombre))
            return "El nombre contiene caracteres no permitidos.";

        return null;
    }

    
    // VALIDAR CORREO
   
    public static string? ValidarCorreo(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
            return "El correo electrónico es requerido.";

        correo = correo.Trim();

        if (correo.Length > MAX_CORREO)
            return $"El correo no puede superar los {MAX_CORREO} caracteres.";

        if (!RegexCorreo.IsMatch(correo))
            return "El formato del correo electrónico no es válido.";

        return null;
    }

  
    // VALIDAR CONTRASEÑA
    
    // No exigimos mayúsculas ni símbolos porque es un juego casual,
    // pero sí una longitud mínima razonable. BCrypt se encarga del
    // resto (una contraseña de 6 chars con BCrypt workFactor 12
    // tarda años en romperse por fuerza bruta).
    public static string? ValidarPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "La contraseña es requerida.";

        if (password.Length < MIN_PASSWORD)
            return $"La contraseña debe tener al menos {MIN_PASSWORD} caracteres.";

        if (password.Length > MAX_PASSWORD)
            return $"La contraseña no puede superar los {MAX_PASSWORD} caracteres.";

        return null;
    }

   
    // VALIDAR MENSAJE DE CHAT
    
    public static string? ValidarMensaje(string? mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return "El mensaje no puede estar vacío.";

        mensaje = mensaje.Trim();

        if (mensaje.Length > MAX_MENSAJE)
            return $"El mensaje no puede superar los {MAX_MENSAJE} caracteres.";

        return null;
    }

  
    // SANITIZAR TEXTO
    
    // Elimina caracteres de control y espacios extra. Se aplica
    // antes de guardar en BD como capa adicional de defensa.
    //
    // NOTA: Entity Framework ya usa consultas parametrizadas, así
    // que la inyección SQL está cubierta. Esto es defensa en
    // profundidad contra XSS si el texto se renderiza en el frontend.
    public static string Sanitizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

        // Quitamos caracteres de control (saltos de línea raros, etc.)
        var limpio = new string(texto.Where(c => !char.IsControl(c)).ToArray());

        // Colapsamos espacios múltiples en uno solo
        limpio = Regex.Replace(limpio, @"\s+", " ");

        return limpio.Trim();
    }

    
    // VALIDAR ID POSITIVO
    
    // Muchos endpoints reciben IDs. Si llega 0 o negativo es
    // porque el cliente mandó algo mal o está probando ataques.
    public static string? ValidarId(int id, string nombreCampo = "ID")
    {
        if (id <= 0)
            return $"El {nombreCampo} no es válido.";

        return null;
    }
}
