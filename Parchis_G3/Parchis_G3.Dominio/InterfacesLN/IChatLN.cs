using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.Dominio.InterfacesLN;

public interface IChatLN
{
    // Devuelve los 4 mensajes rápidos definidos en la HU-14
    Respuesta<List<MensajePredefinidoDTO>> ObtenerMensajesPredefinidos();

    // Valida el cooldown, guarda el mensaje en BD y lo devuelve
    // listo para retransmitir a los demás jugadores
    Respuesta<MensajeChatDTO> EnviarMensaje(int parId, int jpId, string contenido, bool esPredefinido, IUnidadTrabajoEF unidadTrabajo);

    // Trae el historial completo del chat de una partida
    Respuesta<List<MensajeChatDTO>> ObtenerHistorial(int parId, IUnidadTrabajoEF unidadTrabajo);
}
