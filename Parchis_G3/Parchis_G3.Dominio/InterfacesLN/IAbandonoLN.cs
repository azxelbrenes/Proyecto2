using AutoMapper;
using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.Dominio.InterfacesLN;

public interface IAbandonoLN
{
    // RF-14: el jugador deja la partida. Pierde el 20% de la entrada,
    // un bot lo reemplaza y queda registrado como derrota.
    //
    // esVoluntario distingue quién tocó "Abandonar" de quién perdió la
    // conexión y se le vencieron los 60 segundos. Solo el primero suma
    // abandono consecutivo para el bloqueo de 30 minutos.
    Respuesta<ResultadoAbandonoDTO> AbandonarPartida(int usuId, int parId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper, bool esVoluntario = true);

    // RF-13: se detectó desconexión. Marca al jugador como
    // RECONECTANDO y arranca el temporizador de 60 segundos.
    Respuesta<bool> MarcarDesconectado(int parId, int jpId, IUnidadTrabajoEF unidadTrabajo);

    // RF-13: el jugador volvió dentro de los 60 segundos y retoma
    // la partida normalmente.
    Respuesta<bool> Reconectar(int parId, int jpId, IUnidadTrabajoEF unidadTrabajo);

    // Revisa si a algún desconectado se le vencieron los 60 segundos.
    // Si es así, lo reemplaza por un bot sin penalizarlo como abandono.
    Respuesta<List<int>> VerificarDesconexionesVencidas(int parId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper);

    // Cuántos segundos le quedan a un jugador para reconectarse
    int SegundosRestantesReconexion(int jpId);
}