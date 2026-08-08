using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.Dominio.InterfacesLN;

public interface IInventarioLN
{
    // Todos los artículos que el usuario desbloqueó, marcando
    // cuáles tiene equipados actualmente
    Respuesta<List<ArticuloInventarioDTO>> ObtenerMisArticulos(int usuId, IUnidadTrabajoEF unidadTrabajo);

    // Qué ficha, tablero y dado tiene puestos ahora mismo
    Respuesta<EquipamientoDTO> ObtenerMiEquipamiento(int usuId, IUnidadTrabajoEF unidadTrabajo);

    // Cambia el artículo activo de su categoría (upsert).
    // Valida que el usuario realmente lo tenga desbloqueado.
    Respuesta<EquipamientoDTO> EquiparArticulo(int usuId, int artId, IUnidadTrabajoEF unidadTrabajo);

    // Helper para la tienda: ¿ya tiene este artículo?
    // Evita compras duplicadas antes de llegar a la BD.
    bool YaLoTiene(int usuId, int artId, IUnidadTrabajoEF unidadTrabajo);
}
