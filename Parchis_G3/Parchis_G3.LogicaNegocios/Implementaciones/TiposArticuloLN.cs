using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class TiposArticuloLN : ITiposArticuloLN
{
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<TiposArticuloLN> _logger;

    public TiposArticuloLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<TiposArticuloLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // Devuelve todos los registros de el tipo de artículo
    public Respuesta<IEnumerable<TTiposArticulo>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.TTiposArticulo.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TTiposArticulo>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TTiposArticulo>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TTiposArticulo>>.Exito(tipadas, "Registros obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TiposArticuloLN.Listar");
            return Respuesta<IEnumerable<TTiposArticulo>>.Error(ex.Message);
        }
    }

    // Busca un registro específico por su PK (TipId)
    public Respuesta<TTiposArticulo> Buscar(TTiposArticulo entidad)
    {
        try
        {
            if (entidad.TipId <= 0)
                return Respuesta<TTiposArticulo>.Validacion("El ID no es válido.");

            var respuesta = _unidadTrabajo.TTiposArticulo
                .ObtenerEntidad(t => t.TipId == entidad.TipId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<TTiposArticulo>.Validacion("Registro no encontrado.");

            var tipada = _mapper.Map<TTiposArticulo>(respuesta.ValorRetorno);
            return Respuesta<TTiposArticulo>.Exito(tipada, "Registro encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TiposArticuloLN.Buscar");
            return Respuesta<TTiposArticulo>.Error(ex.Message);
        }
    }

    // Obtiene registros filtrados según los campos del objeto recibido
    public Respuesta<IEnumerable<TTiposArticulo>> Obtener(TTiposArticulo entidad)
    {
        try
        {
            // Filtramos por ID si viene especificado (mayor que 0)
            var respuesta = _unidadTrabajo.TTiposArticulo.Buscar(
                x => entidad.TipId <= 0 || x.TipId == entidad.TipId
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TTiposArticulo>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TTiposArticulo>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TTiposArticulo>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TiposArticuloLN.Obtener");
            return Respuesta<IEnumerable<TTiposArticulo>>.Error(ex.Message);
        }
    }

    // Inserta un nuevo registro validando los campos requeridos
    public Respuesta<TTiposArticulo> Insertar(TTiposArticulo entidad)
    {
        try
        {
            // Validamos que los campos clave no vengan vacíos o en cero
            if (string.IsNullOrWhiteSpace(entidad.TipNombre))
                return Respuesta<TTiposArticulo>.Validacion("El nombre del tipo de artículo es requerido.");

            var mapped = _mapper.Map<TiposArticulo>(entidad);
            var insertar = _unidadTrabajo.TTiposArticulo.Insertar(mapped);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<TTiposArticulo>.Error(insertar.strMensajeRespuesta);

            // Completar guarda los cambios en la base de datos
            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TTiposArticulo>(insertar.ValorRetorno);
            return Respuesta<TTiposArticulo>.Exito(tipada, "Registro insertado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TiposArticuloLN.Insertar");
            return Respuesta<TTiposArticulo>.Error(ex.Message);
        }
    }

    // Modifica un registro existente verificando que exista en BD
    public Respuesta<TTiposArticulo> Modificar(TTiposArticulo entidad)
    {
        try
        {
            if (entidad.TipId <= 0)
                return Respuesta<TTiposArticulo>.Validacion("El ID no es válido.");

            // Verificamos que el registro exista antes de modificar
            var existe = _unidadTrabajo.TTiposArticulo.ObtenerEntidad(t => t.TipId == entidad.TipId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<TTiposArticulo>.Validacion("El registro no existe.");

            var mapped = _mapper.Map<TiposArticulo>(entidad);
            var modificar = _unidadTrabajo.TTiposArticulo.Modificar(mapped);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<TTiposArticulo>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TTiposArticulo>(modificar.ValorRetorno);
            return Respuesta<TTiposArticulo>.Exito(tipada, "Registro modificado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TiposArticuloLN.Modificar");
            return Respuesta<TTiposArticulo>.Error(ex.Message);
        }
    }

    // Elimina un registro verificando que exista en BD antes de proceder
    public Respuesta<bool> Eliminar(TTiposArticulo entidad)
    {
        try
        {
            if (entidad.TipId <= 0)
                return Respuesta<bool>.Validacion("El ID no es válido.");

            // Buscamos la entidad real de EF para poder eliminarla
            var existe = _unidadTrabajo.TTiposArticulo.ObtenerEntidad(t => t.TipId == entidad.TipId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("El registro no existe.");

            var eliminar = _unidadTrabajo.TTiposArticulo.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            return Respuesta<bool>.Exito(true, "Registro eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TiposArticuloLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }
}
