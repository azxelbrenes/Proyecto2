using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class TransaccionLN : ITransaccionLN
{
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<TransaccionLN> _logger;

    public TransaccionLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<TransaccionLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // Devuelve todos los registros de la transacción
    public Respuesta<IEnumerable<TTransaccione>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.TTransaccion.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TTransaccione>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TTransaccione>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TTransaccione>>.Exito(tipadas, "Registros obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TransaccionLN.Listar");
            return Respuesta<IEnumerable<TTransaccione>>.Error(ex.Message);
        }
    }

    // Busca un registro específico por su PK (TranId)
    public Respuesta<TTransaccione> Buscar(TTransaccione entidad)
    {
        try
        {
            if (entidad.TranId <= 0)
                return Respuesta<TTransaccione>.Validacion("El ID no es válido.");

            var respuesta = _unidadTrabajo.TTransaccion
                .ObtenerEntidad(t => t.TranId == entidad.TranId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<TTransaccione>.Validacion("Registro no encontrado.");

            var tipada = _mapper.Map<TTransaccione>(respuesta.ValorRetorno);
            return Respuesta<TTransaccione>.Exito(tipada, "Registro encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TransaccionLN.Buscar");
            return Respuesta<TTransaccione>.Error(ex.Message);
        }
    }

    // Obtiene registros filtrados según los campos del objeto recibido
    public Respuesta<IEnumerable<TTransaccione>> Obtener(TTransaccione entidad)
    {
        try
        {
            // Filtramos por ID si viene especificado (mayor que 0)
            var respuesta = _unidadTrabajo.TTransaccion.Buscar(
                x => entidad.TranId <= 0 || x.TranId == entidad.TranId
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TTransaccione>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TTransaccione>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TTransaccione>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TransaccionLN.Obtener");
            return Respuesta<IEnumerable<TTransaccione>>.Error(ex.Message);
        }
    }

    // Inserta un nuevo registro validando los campos requeridos
    public Respuesta<TTransaccione> Insertar(TTransaccione entidad)
    {
        try
        {
            // Validamos que los campos clave no vengan vacíos o en cero
            if (entidad.UsuId <= 0 || string.IsNullOrWhiteSpace(entidad.TranTipo))
                return Respuesta<TTransaccione>.Validacion("El usuario y el tipo de transacción son requeridos.");

            var mapped = _mapper.Map<Transaccione>(entidad);
            var insertar = _unidadTrabajo.TTransaccion.Insertar(mapped);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<TTransaccione>.Error(insertar.strMensajeRespuesta);

            // Completar guarda los cambios en la base de datos
            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TTransaccione>(insertar.ValorRetorno);
            return Respuesta<TTransaccione>.Exito(tipada, "Registro insertado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TransaccionLN.Insertar");
            return Respuesta<TTransaccione>.Error(ex.Message);
        }
    }

    // Modifica un registro existente verificando que exista en BD
    public Respuesta<TTransaccione> Modificar(TTransaccione entidad)
    {
        try
        {
            if (entidad.TranId <= 0)
                return Respuesta<TTransaccione>.Validacion("El ID no es válido.");

            // Verificamos que el registro exista antes de modificar
            var existe = _unidadTrabajo.TTransaccion.ObtenerEntidad(t => t.TranId == entidad.TranId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<TTransaccione>.Validacion("El registro no existe.");

            var mapped = _mapper.Map<Transaccione>(entidad);
            var modificar = _unidadTrabajo.TTransaccion.Modificar(mapped);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<TTransaccione>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TTransaccione>(modificar.ValorRetorno);
            return Respuesta<TTransaccione>.Exito(tipada, "Registro modificado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TransaccionLN.Modificar");
            return Respuesta<TTransaccione>.Error(ex.Message);
        }
    }

    // Elimina un registro verificando que exista en BD antes de proceder
    public Respuesta<bool> Eliminar(TTransaccione entidad)
    {
        try
        {
            if (entidad.TranId <= 0)
                return Respuesta<bool>.Validacion("El ID no es válido.");

            // Buscamos la entidad real de EF para poder eliminarla
            var existe = _unidadTrabajo.TTransaccion.ObtenerEntidad(t => t.TranId == entidad.TranId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("El registro no existe.");

            var eliminar = _unidadTrabajo.TTransaccion.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            return Respuesta<bool>.Exito(true, "Registro eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TransaccionLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }
}
