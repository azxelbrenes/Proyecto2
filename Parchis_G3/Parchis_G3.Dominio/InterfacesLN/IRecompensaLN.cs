using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.Dominio.InterfacesLN;

public interface IRecompensaLN
{
    // ¿Puede reclamar hoy? ¿Qué racha lleva? ¿Cuánto ganaría?
    // El frontend lo consulta al abrir la app para decidir si
    // muestra el modal de recompensa.
    Respuesta<EstadoRecompensaDTO> ObtenerEstado(int usuId, IUnidadTrabajoEF unidadTrabajo);

    // Acredita las monedas del día y actualiza la racha.
    // Valida que no haya reclamado ya hoy.
    Respuesta<ResultadoRecompensaDTO> Reclamar(int usuId, IUnidadTrabajoEF unidadTrabajo);
}
