using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.Dominio.InterfacesLN;

public interface IPagoLN
{
    // Devuelve el catálogo de paquetes de monedas disponibles
    Respuesta<List<PaqueteMonedasDTO>> ObtenerPaquetes();

    // PASO 1: le pide a PayPal crear una orden de pago.
    // Devuelve la URL a la que el frontend debe mandar al usuario.
    Task<Respuesta<OrdenCreadaDTO>> CrearOrden(int usuId, int paqueteId);

    // PASO 2: tras aprobar el usuario, verifica con PayPal que el
    // pago se completó de verdad y recién ahí acredita las monedas.
    Task<Respuesta<ResultadoPagoDTO>> CapturarPago(int usuId, string ordenId, int paqueteId, IUnidadTrabajoEF unidadTrabajo);
}
