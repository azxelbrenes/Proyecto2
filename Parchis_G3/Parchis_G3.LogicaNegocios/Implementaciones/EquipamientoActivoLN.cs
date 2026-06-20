using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class EquipamientoActivoLN : IEquipamientoActivoLN
{
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<EquipamientoActivoLN> _logger;

    public EquipamientoActivoLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<EquipamientoActivoLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // Devuelve todos los registros de el equipamiento
    public Respuesta<IEnumerable<TEquipamientoActivo>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.TEquipamientoActivo.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TEquipamientoActivo>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TEquipamientoActivo>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TEquipamientoActivo>>.Exito(tipadas, "Registros obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EquipamientoActivoLN.Listar");
            return Respuesta<IEnumerable<TEquipamientoActivo>>.Error(ex.Message);
        }
    }

    // Busca un registro específico por su PK (EquId)
    public Respuesta<TEquipamientoActivo> Buscar(TEquipamientoActivo entidad)
    {
        try
        {
            if (entidad.EquId <= 0)
                return Respuesta<TEquipamientoActivo>.Validacion("El ID no es válido.");

            var respuesta = _unidadTrabajo.TEquipamientoActivo
                .ObtenerEntidad(e => e.EquId == entidad.EquId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<TEquipamientoActivo>.Validacion("Registro no encontrado.");

            var tipada = _mapper.Map<TEquipamientoActivo>(respuesta.ValorRetorno);
            return Respuesta<TEquipamientoActivo>.Exito(tipada, "Registro encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EquipamientoActivoLN.Buscar");
            return Respuesta<TEquipamientoActivo>.Error(ex.Message);
        }
    }

    // Obtiene registros filtrados según los campos del objeto recibido
    public Respuesta<IEnumerable<TEquipamientoActivo>> Obtener(TEquipamientoActivo entidad)
    {
        try
        {
            // Filtramos por ID si viene especificado (mayor que 0)
            var respuesta = _unidadTrabajo.TEquipamientoActivo.Buscar(
                x => entidad.EquId <= 0 || x.EquId == entidad.EquId
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TEquipamientoActivo>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TEquipamientoActivo>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TEquipamientoActivo>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EquipamientoActivoLN.Obtener");
            return Respuesta<IEnumerable<TEquipamientoActivo>>.Error(ex.Message);
        }
    }

    // Inserta un nuevo registro validando los campos requeridos
    public Respuesta<TEquipamientoActivo> Insertar(TEquipamientoActivo entidad)
    {
        try
        {
            // Validamos que los campos clave no vengan vacíos o en cero
            if (entidad.UsuId <= 0 || entidad.ArtId <= 0)
                return Respuesta<TEquipamientoActivo>.Validacion("El usuario y el artículo son requeridos.");

            var mapped = _mapper.Map<EquipamientoActivo>(entidad);
            var insertar = _unidadTrabajo.TEquipamientoActivo.Insertar(mapped);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<TEquipamientoActivo>.Error(insertar.strMensajeRespuesta);

            // Completar guarda los cambios en la base de datos
            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TEquipamientoActivo>(insertar.ValorRetorno);
            return Respuesta<TEquipamientoActivo>.Exito(tipada, "Registro insertado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EquipamientoActivoLN.Insertar");
            return Respuesta<TEquipamientoActivo>.Error(ex.Message);
        }
    }

    // Modifica un registro existente verificando que exista en BD
    public Respuesta<TEquipamientoActivo> Modificar(TEquipamientoActivo entidad)
    {
        try
        {
            if (entidad.EquId <= 0)
                return Respuesta<TEquipamientoActivo>.Validacion("El ID no es válido.");

            // Verificamos que el registro exista antes de modificar
            var existe = _unidadTrabajo.TEquipamientoActivo.ObtenerEntidad(e => e.EquId == entidad.EquId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<TEquipamientoActivo>.Validacion("El registro no existe.");

            var mapped = _mapper.Map<EquipamientoActivo>(entidad);
            var modificar = _unidadTrabajo.TEquipamientoActivo.Modificar(mapped);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<TEquipamientoActivo>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TEquipamientoActivo>(modificar.ValorRetorno);
            return Respuesta<TEquipamientoActivo>.Exito(tipada, "Registro modificado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EquipamientoActivoLN.Modificar");
            return Respuesta<TEquipamientoActivo>.Error(ex.Message);
        }
    }

    // Elimina un registro verificando que exista en BD antes de proceder
    public Respuesta<bool> Eliminar(TEquipamientoActivo entidad)
    {
        try
        {
            if (entidad.EquId <= 0)
                return Respuesta<bool>.Validacion("El ID no es válido.");

            // Buscamos la entidad real de EF para poder eliminarla
            var existe = _unidadTrabajo.TEquipamientoActivo.ObtenerEntidad(e => e.EquId == entidad.EquId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("El registro no existe.");

            var eliminar = _unidadTrabajo.TEquipamientoActivo.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            return Respuesta<bool>.Exito(true, "Registro eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EquipamientoActivoLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }
}
