namespace Parchis_G3.Dominio.DTO;

// Un mensaje que viaja del servidor a los 4 jugadores
public class MensajeChatDTO
{
    public int McId { get; set; }
    public int JpId { get; set; }
    public string NombreJugador { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public bool EsPredefinido { get; set; }
    public DateTime Fecha { get; set; }
}

// Los 4 mensajes rápidos que definiste en la HU-14
public class MensajePredefinidoDTO
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;
}

// Body que manda el frontend al enviar un mensaje
public class EnviarMensajeRequest
{
    public int ParId { get; set; }
    public int JpId { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public bool EsPredefinido { get; set; }
}

// Resultado de un abandono de partida (HU-19)
public class ResultadoAbandonoDTO
{
    public int JpId { get; set; }
    public int MonedasPenalizadas { get; set; }
    public int SaldoNuevo { get; set; }
    public int AbandonosConsecutivos { get; set; }
    public bool CuentaBloqueada { get; set; }
    public DateTime? FechaDesbloqueo { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
