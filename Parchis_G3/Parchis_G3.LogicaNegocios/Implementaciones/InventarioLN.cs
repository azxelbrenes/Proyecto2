using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class InventarioLN : IInventarioLN
{
    // IDs de los tipos según el orden de inserción en TiposArticulo
    private const int TIPO_FICHA = 1;
    private const int TIPO_TABLERO = 2;
    private const int TIPO_DADO = 3;

    // ================================================================
    // OBTENER MIS ARTÍCULOS
    // ================================================================
    public Respuesta<List<ArticuloInventarioDTO>> ObtenerMisArticulos(int usuId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            if (usuId <= 0)
                return Respuesta<List<ArticuloInventarioDTO>>.Validacion("Usuario no válido.");

            // ── 1. Artículos que el usuario compró ───────────────
            var comprados = unidadTrabajo.TUsuarioArticulo
                .Buscar(ua => ua.UsuId == usuId)
                .ValorRetorno?.ToList() ?? new List<UsuarioArticulo>();

            // ── 2. Artículos predeterminados (gratis para todos) ──
            // Estos no están en UsuarioArticulos porque nadie los
            // "compra" — vienen desbloqueados desde el registro.
            var predeterminados = unidadTrabajo.TArticulo
                .Buscar(a => a.ArtEsPredeterminado == true && a.ArtEstado == "A")
                .ValorRetorno?.ToList() ?? new List<Articulo>();

            // ── 3. Qué tiene equipado actualmente ────────────────
            var equipados = unidadTrabajo.TEquipamientoActivo
                .Buscar(e => e.UsuId == usuId)
                .ValorRetorno?.ToList() ?? new List<EquipamientoActivo>();

            var idsEquipados = equipados.Select(e => e.ArtId).ToHashSet();

            // ── 4. Catálogo de tipos para poner el nombre ────────
            var tipos = unidadTrabajo.TTiposArticulo.Listar()
                .ValorRetorno?.ToList() ?? new List<TiposArticulo>();

            var lista = new List<ArticuloInventarioDTO>();

            // ── Agregamos los predeterminados ────────────────────
            foreach (var art in predeterminados)
            {
                lista.Add(ConstruirDTO(art, tipos, idsEquipados, DateTime.MinValue, true));
            }

            // ── Agregamos los comprados ──────────────────────────
            foreach (var ua in comprados)
            {
                var art = unidadTrabajo.TArticulo
                    .ObtenerEntidad(a => a.ArtId == ua.ArtId).ValorRetorno;

                if (art == null) continue;

                // Si ya lo agregamos como predeterminado, lo saltamos
                if (lista.Any(l => l.ArtId == art.ArtId)) continue;

                lista.Add(ConstruirDTO(art, tipos, idsEquipados, ua.UartFechaCompra, false));
            }

            return Respuesta<List<ArticuloInventarioDTO>>.Exito(
                lista.OrderBy(a => a.TipId).ThenBy(a => a.ArtPrecio).ToList(),
                "Inventario obtenido correctamente."
            );
        }
        catch (Exception ex)
        {
            return Respuesta<List<ArticuloInventarioDTO>>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // OBTENER MI EQUIPAMIENTO
    // ================================================================
    public Respuesta<EquipamientoDTO> ObtenerMiEquipamiento(int usuId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            if (usuId <= 0)
                return Respuesta<EquipamientoDTO>.Validacion("Usuario no válido.");

            var equipados = unidadTrabajo.TEquipamientoActivo
                .Buscar(e => e.UsuId == usuId)
                .ValorRetorno?.ToList() ?? new List<EquipamientoActivo>();

            var tipos = unidadTrabajo.TTiposArticulo.Listar()
                .ValorRetorno?.ToList() ?? new List<TiposArticulo>();

            var dto = new EquipamientoDTO();

            foreach (var equipo in equipados)
            {
                var art = unidadTrabajo.TArticulo
                    .ObtenerEntidad(a => a.ArtId == equipo.ArtId).ValorRetorno;

                if (art == null) continue;

                var artDto = ConstruirDTO(art, tipos, new HashSet<int> { art.ArtId },
                                          DateTime.MinValue, art.ArtEsPredeterminado);

                // Asignamos según el tipo
                switch (equipo.TipId)
                {
                    case TIPO_FICHA: dto.Ficha = artDto; break;
                    case TIPO_TABLERO: dto.Tablero = artDto; break;
                    case TIPO_DADO: dto.Dado = artDto; break;
                }
            }

            // ── Si no tiene nada equipado, ponemos los predeterminados ──
            // Así el frontend nunca recibe null y el juego siempre
            // tiene algo que renderizar en el tablero.
            if (dto.Ficha == null) dto.Ficha = ObtenerPredeterminado(TIPO_FICHA, tipos, unidadTrabajo);
            if (dto.Tablero == null) dto.Tablero = ObtenerPredeterminado(TIPO_TABLERO, tipos, unidadTrabajo);
            if (dto.Dado == null) dto.Dado = ObtenerPredeterminado(TIPO_DADO, tipos, unidadTrabajo);

            return Respuesta<EquipamientoDTO>.Exito(dto, "Equipamiento obtenido.");
        }
        catch (Exception ex)
        {
            return Respuesta<EquipamientoDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // EQUIPAR ARTÍCULO (UPSERT)
    // ================================================================
    // La tabla EquipamientoActivo tiene el constraint:
    //     UNIQUE (Usu_ID, Tip_ID)
    // Es decir: un usuario solo puede tener UN artículo activo por
    // tipo. Por eso NO podemos hacer un simple Insertar() — hay que
    // verificar si ya existe un registro de ese tipo y reemplazarlo.
    // Eso es lo que se llama un "upsert" (update + insert).
    // ================================================================
    public Respuesta<EquipamientoDTO> EquiparArticulo(int usuId, int artId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            if (usuId <= 0 || artId <= 0)
                return Respuesta<EquipamientoDTO>.Validacion("Datos no válidos.");

            // ── 1. El artículo existe y está activo ──────────────
            var artResp = unidadTrabajo.TArticulo
                .ObtenerEntidad(a => a.ArtId == artId && a.ArtEstado == "A");

            if (!artResp.blnIndicadorTransaccion)
                return Respuesta<EquipamientoDTO>.Validacion("El artículo no existe.");

            var articulo = artResp.ValorRetorno!;

            // ── 2. El usuario realmente lo tiene ─────────────────
            // Sin esta validación, cualquiera podría equipar artículos
            // que nunca compró simplemente mandando el ArtId.
            if (!YaLoTiene(usuId, artId, unidadTrabajo))
                return Respuesta<EquipamientoDTO>.Validacion(
                    "No tenés este artículo desbloqueado. Compralo primero en la tienda.");

            // ── 3. ¿Ya tiene algo equipado de este tipo? ─────────
            var existente = unidadTrabajo.TEquipamientoActivo
                .ObtenerEntidad(e => e.UsuId == usuId && e.TipId == articulo.TipId);

            if (existente.blnIndicadorTransaccion)
            {
                // UPDATE: reemplazamos el artículo del registro existente
                var equipo = existente.ValorRetorno!;
                equipo.ArtId = artId;
                unidadTrabajo.TEquipamientoActivo.Modificar(equipo);
            }
            else
            {
                // INSERT: primera vez que equipa algo de este tipo
                unidadTrabajo.TEquipamientoActivo.Insertar(new EquipamientoActivo
                {
                    UsuId = usuId,
                    TipId = articulo.TipId,
                    ArtId = artId
                });
            }

            unidadTrabajo.Completar();

            // Devolvemos el equipamiento completo actualizado para
            // que el frontend refresque toda la vista de una vez
            return ObtenerMiEquipamiento(usuId, unidadTrabajo);
        }
        catch (Exception ex)
        {
            return Respuesta<EquipamientoDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // ¿YA LO TIENE?
    // ================================================================
    // Lo usa la tienda para no permitir compras duplicadas, y el
    // equipar para validar propiedad.
    public bool YaLoTiene(int usuId, int artId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            // Los predeterminados los tiene todo el mundo sin comprar
            var art = unidadTrabajo.TArticulo
                .ObtenerEntidad(a => a.ArtId == artId).ValorRetorno;

            if (art?.ArtEsPredeterminado == true) return true;

            // Si no, verificamos que lo haya comprado
            var comprado = unidadTrabajo.TUsuarioArticulo
                .ObtenerEntidad(ua => ua.UsuId == usuId && ua.ArtId == artId);

            return comprado.blnIndicadorTransaccion;
        }
        catch
        {
            return false;
        }
    }

    // ================================================================
    // HELPERS PRIVADOS
    // ================================================================

    private ArticuloInventarioDTO ConstruirDTO(
        Articulo art,
        List<TiposArticulo> tipos,
        HashSet<int> idsEquipados,
        DateTime fechaCompra,
        bool esPredeterminado)
    {
        return new ArticuloInventarioDTO
        {
            ArtId = art.ArtId,
            TipId = art.TipId,
            TipoNombre = tipos.FirstOrDefault(t => t.TipId == art.TipId)?.TipNombre ?? "",
            ArtNombre = art.ArtNombre,
            ArtDescripcion = art.ArtDescripcion,
            ArtPrecio = art.ArtPrecio,
            ArtImagenUrl = art.ArtImagenUrl,
            EsPredeterminado = esPredeterminado,
            EstaEquipado = idsEquipados.Contains(art.ArtId),
            FechaCompra = fechaCompra
        };
    }

    // Devuelve el artículo gratuito de un tipo — se usa cuando el
    // usuario nunca equipó nada de esa categoría
    private ArticuloInventarioDTO? ObtenerPredeterminado(
        int tipId,
        List<TiposArticulo> tipos,
        IUnidadTrabajoEF unidadTrabajo)
    {
        var art = unidadTrabajo.TArticulo
            .ObtenerEntidad(a => a.TipId == tipId && a.ArtEsPredeterminado == true)
            .ValorRetorno;

        if (art == null) return null;

        return ConstruirDTO(art, tipos, new HashSet<int> { art.ArtId }, DateTime.MinValue, true);
    }
}
