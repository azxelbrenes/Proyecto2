using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class SesionesActivaLN : ISesionesActivaLN
{
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<SesionesActivaLN> _logger;

    public SesionesActivaLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<SesionesActivaLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // Devuelve todos los registros de la sesión
    public Respuesta<IEnumerable<TSesionesActiva>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.TSesionesActiva.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TSesionesActiva>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TSesionesActiva>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TSesionesActiva>>.Exito(tipadas, "Registros obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SesionesActivaLN.Listar");
            return Respuesta<IEnumerable<TSesionesActiva>>.Error(ex.Message);
        }
    }

    // Busca un registro específico por su PK (SesId)
    public Respuesta<TSesionesActiva> Buscar(TSesionesActiva entidad)
    {
        try
        {
            if (entidad.SesId <= 0)
                return Respuesta<TSesionesActiva>.Validacion("El ID no es válido.");

            var respuesta = _unidadTrabajo.TSesionesActiva
                .ObtenerEntidad(s => s.SesId == entidad.SesId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<TSesionesActiva>.Validacion("Registro no encontrado.");

            var tipada = _mapper.Map<TSesionesActiva>(respuesta.ValorRetorno);
            return Respuesta<TSesionesActiva>.Exito(tipada, "Registro encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SesionesActivaLN.Buscar");
            return Respuesta<TSesionesActiva>.Error(ex.Message);
        }
    }

    // Obtiene registros filtrados según los campos del objeto recibido
    public Respuesta<IEnumerable<TSesionesActiva>> Obtener(TSesionesActiva entidad)
    {
        try
        {
            // Filtramos por ID si viene especificado (mayor que 0)
            var respuesta = _unidadTrabajo.TSesionesActiva.Buscar(
                x => entidad.SesId <= 0 || x.SesId == entidad.SesId
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TSesionesActiva>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TSesionesActiva>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TSesionesActiva>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SesionesActivaLN.Obtener");
            return Respuesta<IEnumerable<TSesionesActiva>>.Error(ex.Message);
        }
    }

    // Inserta un nuevo registro validando los campos requeridos
    public Respuesta<TSesionesActiva> Insertar(TSesionesActiva entidad)
    {
        try
        {
            // Validamos que los campos clave no vengan vacíos o en cero
            if (entidad.UsuId <= 0 || string.IsNullOrWhiteSpace(entidad.SesTokenHash))
                return Respuesta<TSesionesActiva>.Validacion("El usuario y el token son requeridos.");

            var mapped = _mapper.Map<SesionesActiva>(entidad);
            var insertar = _unidadTrabajo.TSesionesActiva.Insertar(mapped);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<TSesionesActiva>.Error(insertar.strMensajeRespuesta);

            // Completar guarda los cambios en la base de datos
            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TSesionesActiva>(insertar.ValorRetorno);
            return Respuesta<TSesionesActiva>.Exito(tipada, "Registro insertado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SesionesActivaLN.Insertar");
            return Respuesta<TSesionesActiva>.Error(ex.Message);
        }
    }

    // Modifica un registro existente verificando que exista en BD
    public Respuesta<TSesionesActiva> Modificar(TSesionesActiva entidad)
    {
        try
        {
            if (entidad.SesId <= 0)
                return Respuesta<TSesionesActiva>.Validacion("El ID no es válido.");

            // Verificamos que el registro exista antes de modificar
            var existe = _unidadTrabajo.TSesionesActiva.ObtenerEntidad(s => s.SesId == entidad.SesId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<TSesionesActiva>.Validacion("El registro no existe.");

            var mapped = _mapper.Map<SesionesActiva>(entidad);
            var modificar = _unidadTrabajo.TSesionesActiva.Modificar(mapped);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<TSesionesActiva>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TSesionesActiva>(modificar.ValorRetorno);
            return Respuesta<TSesionesActiva>.Exito(tipada, "Registro modificado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SesionesActivaLN.Modificar");
            return Respuesta<TSesionesActiva>.Error(ex.Message);
        }
    }

    // Elimina un registro verificando que exista en BD antes de proceder
    public Respuesta<bool> Eliminar(TSesionesActiva entidad)
    {
        try
        {
            if (entidad.SesId <= 0)
                return Respuesta<bool>.Validacion("El ID no es válido.");

            // Buscamos la entidad real de EF para poder eliminarla
            var existe = _unidadTrabajo.TSesionesActiva.ObtenerEntidad(s => s.SesId == entidad.SesId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("El registro no existe.");

            var eliminar = _unidadTrabajo.TSesionesActiva.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            return Respuesta<bool>.Exito(true, "Registro eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SesionesActivaLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }
}
