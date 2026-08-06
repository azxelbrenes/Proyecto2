
using AutoMapper;
using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.Dominio.InterfacesLN;

public interface IMatchmakingLN
{
    // Busca partida para un jugador en la sala indicada.
    // Si no hay ninguna esperando, crea una nueva.
    // Descuenta las monedas de entrada automáticamente.
    Respuesta<ResultadoMatchmakingDTO> BuscarPartida(int usuId, int salId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper);

    // Devuelve el estado actual de la sala de espera —
    // el frontend lo consulta para mostrar quién se ha unido.
    Respuesta<EstadoSalaEsperaDTO> ObtenerEstadoEspera(int parId, IUnidadTrabajoEF unidadTrabajo);

    // Revisa si ya pasaron los 30 segundos o si se llenó la partida.
    // Si corresponde, completa con bots e inicia el juego.
    // Devuelve true si la partida arrancó en esta llamada.
    Respuesta<bool> VerificarEInicIar(int parId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper);

    // El jugador se arrepiente y sale de la sala de espera.
    // Le devolvemos las monedas que había pagado.
    Respuesta<int> AbandonarEspera(int usuId, int parId, IUnidadTrabajoEF unidadTrabajo);
}
