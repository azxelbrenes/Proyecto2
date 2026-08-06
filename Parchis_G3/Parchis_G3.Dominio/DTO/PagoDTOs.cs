namespace Parchis_G3.Dominio.DTO;

// Un paquete de monedas a la venta
public class PaqueteMonedasDTO
{
    public int PaqueteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Monedas { get; set; }
    public decimal PrecioUSD { get; set; }
    public decimal PrecioCRC { get; set; }
    public string Emoji { get; set; } = string.Empty;
    public bool EsPopular { get; set; }  // Para destacarlo en la tienda
}

// Lo que devuelve el servidor al crear una orden de PayPal
public class OrdenCreadaDTO
{
    public string OrdenId { get; set; } = string.Empty;
    public string UrlAprobacion { get; set; } = string.Empty;  // Donde el usuario paga
    public int PaqueteId { get; set; }
    public int Monedas { get; set; }
    public decimal Precio { get; set; }
}

// Resultado tras capturar (cobrar) el pago
public class ResultadoPagoDTO
{
    public bool Exitoso { get; set; }
    public string OrdenId { get; set; } = string.Empty;
    public int MonedasAcreditadas { get; set; }
    public int SaldoNuevo { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

// Body que manda el frontend para crear una orden
public class CrearOrdenRequest
{
    public int PaqueteId { get; set; }
}

// Body que manda el frontend tras aprobar el pago en PayPal
public class CapturarPagoRequest
{
    public string OrdenId { get; set; } = string.Empty;
    public int PaqueteId { get; set; }
}
