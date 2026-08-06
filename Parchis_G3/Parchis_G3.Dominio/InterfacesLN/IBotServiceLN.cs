using AutoMapper;
using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.Dominio.InterfacesLN;

public interface IBotServiceLN
{
    // Verifica si el jugador cuyo turno está activo es un bot.
    // El Hub llama a esto después de cada jugada para saber si
    // debe disparar el turno automático.
    bool EsTurnoDeBot(int parId, IUnidadTrabajoEF unidadTrabajo, out int jpIdBot);

    // Ejecuta el turno completo de un bot: tira el dado, evalúa
    // todos los movimientos posibles, elige el mejor y lo ejecuta.
    Respuesta<ResultadoTurnoDTO> JugarTurnoBot(int parId, int jpIdBot, IUnidadTrabajoEF unidadTrabajo, IMapper mapper);
}
