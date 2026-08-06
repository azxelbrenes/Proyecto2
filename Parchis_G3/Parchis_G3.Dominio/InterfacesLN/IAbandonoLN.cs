using AutoMapper;
using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.Dominio.InterfacesLN;

public interface IAbandonoLN
{
    // HU-19: el jugador abandona voluntariamente.
    // Pierde 20% de la entrada, un bot lo reemplaza y se le
    // cuenta el abandono para el bloqueo por reincidencia.
    Respuesta<ResultadoAbandonoDTO> AbandonarPartida(int usuId, int parId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper);

    // HU-18: se detectó desconexión. Marca al jugador como
    // RECONECTANDO y arranca el temporizador de 60 segundos.
    Respuesta<bool> MarcarDesconectado(int parId, int jpId, IUnidadTrabajoEF unidadTrabajo);

    // HU-18: el jugador volvió. Si fue dentro de los 60 segundos,
    // retoma la partida normalmente.
    Respuesta<bool> Reconectar(int parId, int jpId, IUnidadTrabajoEF unidadTrabajo);

    // Revisa si a algún jugador desconectado ya se le vencieron
    // los 60 segundos. Si es así, aplica el abandono automático.
    Respuesta<List<int>> VerificarDesconexionesVencidas(int parId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper);

    // Cuántos segundos le quedan a un jugador para reconectarse
    int SegundosRestantesReconexion(int jpId);
}
