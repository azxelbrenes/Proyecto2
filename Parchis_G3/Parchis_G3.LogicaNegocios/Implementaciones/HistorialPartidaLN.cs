using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class HistorialPartidaLN : IHistorialPartidaLN
{
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<HistorialPartidaLN> _logger;

    public HistorialPartidaLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<HistorialPartidaLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // Devuelve todos los registros de el historial
    public Respuesta<IEnumerable<THistorialPartida>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.THistorialPartida.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<THistorialPartida>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<THistorialPartida>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<THistorialPartida>>.Exito(tipadas, "Registros obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en HistorialPartidaLN.Listar");
            return Respuesta<IEnumerable<THistorialPartida>>.Error(ex.Message);
        }
    }

    // Busca un registro específico por su PK (HpId)
    public Respuesta<THistorialPartida> Buscar(THistorialPartida entidad)
    {
        try
        {
            if (entidad.HpId <= 0)
                return Respuesta<THistorialPartida>.Validacion("El ID no es válido.");

            var respuesta = _unidadTrabajo.THistorialPartida
                .ObtenerEntidad(h => h.HpId == entidad.HpId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<THistorialPartida>.Validacion("Registro no encontrado.");

            var tipada = _mapper.Map<THistorialPartida>(respuesta.ValorRetorno);
            return Respuesta<THistorialPartida>.Exito(tipada, "Registro encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en HistorialPartidaLN.Buscar");
            return Respuesta<THistorialPartida>.Error(ex.Message);
        }
    }

    // Obtiene registros filtrados según los campos del objeto recibido
    public Respuesta<IEnumerable<THistorialPartida>> Obtener(THistorialPartida entidad)
    {
        try
        {
            // Filtramos por ID si viene especificado (mayor que 0)
            var respuesta = _unidadTrabajo.THistorialPartida.Buscar(
                x => entidad.HpId <= 0 || x.HpId == entidad.HpId
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<THistorialPartida>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<THistorialPartida>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<THistorialPartida>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en HistorialPartidaLN.Obtener");
            return Respuesta<IEnumerable<THistorialPartida>>.Error(ex.Message);
        }
    }

    // Inserta un nuevo registro validando los campos requeridos
    public Respuesta<THistorialPartida> Insertar(THistorialPartida entidad)
    {
        try
        {
            // Validamos que los campos clave no vengan vacíos o en cero
            if (entidad.UsuId <= 0 || entidad.ParId <= 0)
                return Respuesta<THistorialPartida>.Validacion("El usuario y la partida son requeridos.");

            var mapped = _mapper.Map<HistorialPartida>(entidad);
            var insertar = _unidadTrabajo.THistorialPartida.Insertar(mapped);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<THistorialPartida>.Error(insertar.strMensajeRespuesta);

            // Completar guarda los cambios en la base de datos
            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<THistorialPartida>(insertar.ValorRetorno);
            return Respuesta<THistorialPartida>.Exito(tipada, "Registro insertado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en HistorialPartidaLN.Insertar");
            return Respuesta<THistorialPartida>.Error(ex.Message);
        }
    }

    // Modifica un registro existente verificando que exista en BD
    public Respuesta<THistorialPartida> Modificar(THistorialPartida entidad)
    {
        try
        {
            if (entidad.HpId <= 0)
                return Respuesta<THistorialPartida>.Validacion("El ID no es válido.");

            // Verificamos que el registro exista antes de modificar
            var existe = _unidadTrabajo.THistorialPartida.ObtenerEntidad(h => h.HpId == entidad.HpId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<THistorialPartida>.Validacion("El registro no existe.");

            var mapped = _mapper.Map<HistorialPartida>(entidad);
            var modificar = _unidadTrabajo.THistorialPartida.Modificar(mapped);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<THistorialPartida>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<THistorialPartida>(modificar.ValorRetorno);
            return Respuesta<THistorialPartida>.Exito(tipada, "Registro modificado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en HistorialPartidaLN.Modificar");
            return Respuesta<THistorialPartida>.Error(ex.Message);
        }
    }

    // Elimina un registro verificando que exista en BD antes de proceder
    public Respuesta<bool> Eliminar(THistorialPartida entidad)
    {
        try
        {
            if (entidad.HpId <= 0)
                return Respuesta<bool>.Validacion("El ID no es válido.");

            // Buscamos la entidad real de EF para poder eliminarla
            var existe = _unidadTrabajo.THistorialPartida.ObtenerEntidad(h => h.HpId == entidad.HpId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("El registro no existe.");

            var eliminar = _unidadTrabajo.THistorialPartida.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            return Respuesta<bool>.Exito(true, "Registro eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en HistorialPartidaLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }
}
