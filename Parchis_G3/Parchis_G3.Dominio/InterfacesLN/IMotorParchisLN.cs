using AutoMapper;
using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.Dominio.InterfacesLN;

public interface IMotorParchisLN
{
    // Crea las 16 fichas (4 por jugador) en estado EN_CASA
    // y marca la partida como EN_JUEGO. Se llama una sola vez
    // al iniciar la partida.
    Respuesta<bool> IniciarPartida(int parId, IUnidadTrabajoEF unidadTrabajo);

    // Tira el dado para el jugador indicado. Valida que sea su turno.
    Respuesta<ResultadoTurnoDTO> TirarDado(int parId, int jpId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper);

    // Mueve la ficha elegida usando el valor de dado ya tirado.
    // Valida el movimiento, aplica capturas, coronación y determina
    // si hay ganador.
    Respuesta<ResultadoTurnoDTO> MoverFicha(int parId, int jpId, int numeroFicha, int valorDado, IUnidadTrabajoEF unidadTrabajo, IMapper mapper);

    // Devuelve el estado completo actual del tablero — se usa quien
    // se reconecta o entra a ver la partida por primera vez.
    Respuesta<EstadoPartidaDTO> ObtenerEstado(int parId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper);
}
