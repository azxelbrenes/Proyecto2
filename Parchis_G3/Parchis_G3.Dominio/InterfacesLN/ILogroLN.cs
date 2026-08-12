using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.Dominio.InterfacesLN;

public interface ILogroLN
{
    // RF-05: devuelve los 8 logros con su progreso y si ya se reclamó
    // la recompensa. Se calculan desde el historial y las
    // transacciones, no hay tabla de logros.
    Respuesta<ResumenLogrosDTO> ObtenerLogros(int usuId, IUnidadTrabajoEF unidadTrabajo);

    // Acredita de una vez las monedas de todos los logros que estén
    // desbloqueados y sin reclamar.
    Respuesta<ResultadoReclamoLogrosDTO> ReclamarPendientes(int usuId, IUnidadTrabajoEF unidadTrabajo);
}