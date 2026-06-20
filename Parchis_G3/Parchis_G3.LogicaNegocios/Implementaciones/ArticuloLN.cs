using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class ArticuloLN : IArticuloLN
{
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<ArticuloLN> _logger;

    public ArticuloLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<ArticuloLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    public Respuesta<IEnumerable<TArticulo>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.TArticulo.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TArticulo>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TArticulo>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TArticulo>>.Exito(tipadas, "Artículos obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ArticuloLN.Listar");
            return Respuesta<IEnumerable<TArticulo>>.Error(ex.Message);
        }
    }

    public Respuesta<TArticulo> Buscar(TArticulo Articulo)
    {
        try
        {
            if (Articulo.ArtId <= 0)
                return Respuesta<TArticulo>.Validacion("El ID del artículo no es válido.");

            var respuesta = _unidadTrabajo.TArticulo
                .ObtenerEntidad(a => a.ArtId == Articulo.ArtId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<TArticulo>.Validacion("Artículo no encontrado.");

            var tipada = _mapper.Map<TArticulo>(respuesta.ValorRetorno);
            return Respuesta<TArticulo>.Exito(tipada, "Artículo encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ArticuloLN.Buscar");
            return Respuesta<TArticulo>.Error(ex.Message);
        }
    }

    public Respuesta<IEnumerable<TArticulo>> Obtener(TArticulo Articulo)
    {
        try
        {
            // Filtramos por tipo de artículo (ficha, tablero, dado) y estado
            var respuesta = _unidadTrabajo.TArticulo.Buscar(a =>
                (Articulo.TipId <= 0 || a.TipId == Articulo.TipId) &&
                (string.IsNullOrEmpty(Articulo.ArtEstado) || a.ArtEstado == Articulo.ArtEstado)
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TArticulo>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TArticulo>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TArticulo>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ArticuloLN.Obtener");
            return Respuesta<IEnumerable<TArticulo>>.Error(ex.Message);
        }
    }

    public Respuesta<TArticulo> Insertar(TArticulo Articulo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Articulo.ArtNombre))
                return Respuesta<TArticulo>.Validacion("El nombre del artículo es requerido.");

            if (Articulo.TipId <= 0)
                return Respuesta<TArticulo>.Validacion("El tipo de artículo es requerido.");

            // El precio no puede ser negativo (puede ser 0 si es predeterminado/gratis)
            if (Articulo.ArtPrecio < 0)
                return Respuesta<TArticulo>.Validacion("El precio no puede ser negativo.");

            Articulo.ArtEstado = "A"; // Activo por defecto

            var entidad = _mapper.Map<Articulo>(Articulo);
            var insertar = _unidadTrabajo.TArticulo.Insertar(entidad);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<TArticulo>.Error(insertar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            var tipada = _mapper.Map<TArticulo>(insertar.ValorRetorno);
            return Respuesta<TArticulo>.Exito(tipada, "Artículo creado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ArticuloLN.Insertar");
            return Respuesta<TArticulo>.Error(ex.Message);
        }
    }

    public Respuesta<TArticulo> Modificar(TArticulo Articulo)
    {
        try
        {
            if (Articulo.ArtId <= 0)
                return Respuesta<TArticulo>.Validacion("El ID del artículo no es válido.");

            if (string.IsNullOrWhiteSpace(Articulo.ArtNombre))
                return Respuesta<TArticulo>.Validacion("El nombre del artículo es requerido.");

            var existe = _unidadTrabajo.TArticulo.ObtenerEntidad(a => a.ArtId == Articulo.ArtId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<TArticulo>.Validacion("El artículo no existe.");

            var entidad = _mapper.Map<Articulo>(Articulo);
            var modificar = _unidadTrabajo.TArticulo.Modificar(entidad);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<TArticulo>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            var tipada = _mapper.Map<TArticulo>(modificar.ValorRetorno);
            return Respuesta<TArticulo>.Exito(tipada, "Artículo modificado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ArticuloLN.Modificar");
            return Respuesta<TArticulo>.Error(ex.Message);
        }
    }

    public Respuesta<bool> Eliminar(TArticulo Articulo)
    {
        try
        {
            if (Articulo.ArtId <= 0)
                return Respuesta<bool>.Validacion("El ID del artículo no es válido.");

            var existe = _unidadTrabajo.TArticulo.ObtenerEntidad(a => a.ArtId == Articulo.ArtId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("El artículo no existe.");

            var eliminar = _unidadTrabajo.TArticulo.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            return Respuesta<bool>.Exito(true, "Artículo eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ArticuloLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }
}

