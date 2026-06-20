using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class PartidaLN : IPartidaLN
{
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<PartidaLN> _logger;

    public PartidaLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<PartidaLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // Devuelve todos los registros de la partida
    public Respuesta<IEnumerable<TPartida>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.TPartida.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TPartida>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TPartida>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TPartida>>.Exito(tipadas, "Registros obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PartidaLN.Listar");
            return Respuesta<IEnumerable<TPartida>>.Error(ex.Message);
        }
    }

    // Busca un registro específico por su PK (ParId)
    public Respuesta<TPartida> Buscar(TPartida entidad)
    {
        try
        {
            if (entidad.ParId <= 0)
                return Respuesta<TPartida>.Validacion("El ID no es válido.");

            var respuesta = _unidadTrabajo.TPartida
                .ObtenerEntidad(p => p.ParId == entidad.ParId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<TPartida>.Validacion("Registro no encontrado.");

            var tipada = _mapper.Map<TPartida>(respuesta.ValorRetorno);
            return Respuesta<TPartida>.Exito(tipada, "Registro encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PartidaLN.Buscar");
            return Respuesta<TPartida>.Error(ex.Message);
        }
    }

    // Obtiene registros filtrados según los campos del objeto recibido
    public Respuesta<IEnumerable<TPartida>> Obtener(TPartida entidad)
    {
        try
        {
            // Filtramos por ID si viene especificado (mayor que 0)
            var respuesta = _unidadTrabajo.TPartida.Buscar(
                x => entidad.ParId <= 0 || x.ParId == entidad.ParId
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TPartida>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TPartida>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TPartida>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PartidaLN.Obtener");
            return Respuesta<IEnumerable<TPartida>>.Error(ex.Message);
        }
    }

    // Inserta un nuevo registro validando los campos requeridos
    public Respuesta<TPartida> Insertar(TPartida entidad)
    {
        try
        {
            // Validamos que los campos clave no vengan vacíos o en cero
            if (entidad.SalId <= 0)
                return Respuesta<TPartida>.Validacion("La sala de la partida es requerida.");

            var mapped = _mapper.Map<Partida>(entidad);
            var insertar = _unidadTrabajo.TPartida.Insertar(mapped);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<TPartida>.Error(insertar.strMensajeRespuesta);

            // Completar guarda los cambios en la base de datos
            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TPartida>(insertar.ValorRetorno);
            return Respuesta<TPartida>.Exito(tipada, "Registro insertado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PartidaLN.Insertar");
            return Respuesta<TPartida>.Error(ex.Message);
        }
    }

    // Modifica un registro existente verificando que exista en BD
    public Respuesta<TPartida> Modificar(TPartida entidad)
    {
        try
        {
            if (entidad.ParId <= 0)
                return Respuesta<TPartida>.Validacion("El ID no es válido.");

            // Verificamos que el registro exista antes de modificar
            var existe = _unidadTrabajo.TPartida.ObtenerEntidad(p => p.ParId == entidad.ParId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<TPartida>.Validacion("El registro no existe.");

            var mapped = _mapper.Map<Partida>(entidad);
            var modificar = _unidadTrabajo.TPartida.Modificar(mapped);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<TPartida>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TPartida>(modificar.ValorRetorno);
            return Respuesta<TPartida>.Exito(tipada, "Registro modificado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PartidaLN.Modificar");
            return Respuesta<TPartida>.Error(ex.Message);
        }
    }

    // Elimina un registro verificando que exista en BD antes de proceder
    public Respuesta<bool> Eliminar(TPartida entidad)
    {
        try
        {
            if (entidad.ParId <= 0)
                return Respuesta<bool>.Validacion("El ID no es válido.");

            // Buscamos la entidad real de EF para poder eliminarla
            var existe = _unidadTrabajo.TPartida.ObtenerEntidad(p => p.ParId == entidad.ParId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("El registro no existe.");

            var eliminar = _unidadTrabajo.TPartida.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            return Respuesta<bool>.Exito(true, "Registro eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PartidaLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }
}
