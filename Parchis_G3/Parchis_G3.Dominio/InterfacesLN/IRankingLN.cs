using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.Dominio.InterfacesLN;

public interface IRankingLN
{
    // Top de jugadores ordenados por monedas ganadas en partidas.
    // Incluye la posición del usuario actual aunque esté fuera
    // del top — sin eso, un jugador nuevo abriría el ranking y
    // no se vería a sí mismo por ningún lado.
    Respuesta<RankingDTO> ObtenerRanking(int usuId, int top, IUnidadTrabajoEF unidadTrabajo);
}
