using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class EstadoFichaLN : IEstadoFichaLN
{
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<EstadoFichaLN> _logger;

    public EstadoFichaLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<EstadoFichaLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // Devuelve todos los registros de el estado de ficha
    public Respuesta<IEnumerable<TEstadoFicha>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.TEstadoFicha.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TEstadoFicha>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TEstadoFicha>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TEstadoFicha>>.Exito(tipadas, "Registros obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EstadoFichaLN.Listar");
            return Respuesta<IEnumerable<TEstadoFicha>>.Error(ex.Message);
        }
    }

    // Busca un registro específico por su PK (EfId)
    public Respuesta<TEstadoFicha> Buscar(TEstadoFicha entidad)
    {
        try
        {
            if (entidad.EfId <= 0)
                return Respuesta<TEstadoFicha>.Validacion("El ID no es válido.");

            var respuesta = _unidadTrabajo.TEstadoFicha
                .ObtenerEntidad(e => e.EfId == entidad.EfId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<TEstadoFicha>.Validacion("Registro no encontrado.");

            var tipada = _mapper.Map<TEstadoFicha>(respuesta.ValorRetorno);
            return Respuesta<TEstadoFicha>.Exito(tipada, "Registro encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EstadoFichaLN.Buscar");
            return Respuesta<TEstadoFicha>.Error(ex.Message);
        }
    }

    // Obtiene registros filtrados según los campos del objeto recibido
    public Respuesta<IEnumerable<TEstadoFicha>> Obtener(TEstadoFicha entidad)
    {
        try
        {
            // Filtramos por ID si viene especificado (mayor que 0)
            var respuesta = _unidadTrabajo.TEstadoFicha.Buscar(
                x => entidad.EfId <= 0 || x.EfId == entidad.EfId
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TEstadoFicha>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TEstadoFicha>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TEstadoFicha>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EstadoFichaLN.Obtener");
            return Respuesta<IEnumerable<TEstadoFicha>>.Error(ex.Message);
        }
    }

    // Inserta un nuevo registro validando los campos requeridos
    public Respuesta<TEstadoFicha> Insertar(TEstadoFicha entidad)
    {
        try
        {
            // Validamos que los campos clave no vengan vacíos o en cero
            if (entidad.JpId <= 0 || entidad.ParId <= 0)
                return Respuesta<TEstadoFicha>.Validacion("El jugador y la partida son requeridos.");

            var mapped = _mapper.Map<EstadoFicha>(entidad);
            var insertar = _unidadTrabajo.TEstadoFicha.Insertar(mapped);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<TEstadoFicha>.Error(insertar.strMensajeRespuesta);

            // Completar guarda los cambios en la base de datos
            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TEstadoFicha>(insertar.ValorRetorno);
            return Respuesta<TEstadoFicha>.Exito(tipada, "Registro insertado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EstadoFichaLN.Insertar");
            return Respuesta<TEstadoFicha>.Error(ex.Message);
        }
    }

    // Modifica un registro existente verificando que exista en BD
    public Respuesta<TEstadoFicha> Modificar(TEstadoFicha entidad)
    {
        try
        {
            if (entidad.EfId <= 0)
                return Respuesta<TEstadoFicha>.Validacion("El ID no es válido.");

            // Verificamos que el registro exista antes de modificar
            var existe = _unidadTrabajo.TEstadoFicha.ObtenerEntidad(e => e.EfId == entidad.EfId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<TEstadoFicha>.Validacion("El registro no existe.");

            var mapped = _mapper.Map<EstadoFicha>(entidad);
            var modificar = _unidadTrabajo.TEstadoFicha.Modificar(mapped);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<TEstadoFicha>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TEstadoFicha>(modificar.ValorRetorno);
            return Respuesta<TEstadoFicha>.Exito(tipada, "Registro modificado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EstadoFichaLN.Modificar");
            return Respuesta<TEstadoFicha>.Error(ex.Message);
        }
    }

    // Elimina un registro verificando que exista en BD antes de proceder
    public Respuesta<bool> Eliminar(TEstadoFicha entidad)
    {
        try
        {
            if (entidad.EfId <= 0)
                return Respuesta<bool>.Validacion("El ID no es válido.");

            // Buscamos la entidad real de EF para poder eliminarla
            var existe = _unidadTrabajo.TEstadoFicha.ObtenerEntidad(e => e.EfId == entidad.EfId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("El registro no existe.");

            var eliminar = _unidadTrabajo.TEstadoFicha.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            return Respuesta<bool>.Exito(true, "Registro eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EstadoFichaLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }
}
