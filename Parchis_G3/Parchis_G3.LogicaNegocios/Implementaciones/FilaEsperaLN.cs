using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class FilaEsperaLN : IFilaEsperaLN
{
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<FilaEsperaLN> _logger;

    public FilaEsperaLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<FilaEsperaLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // Devuelve todos los registros de la fila de espera
    public Respuesta<IEnumerable<TFilaEspera>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.TFilaEspera.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TFilaEspera>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TFilaEspera>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TFilaEspera>>.Exito(tipadas, "Registros obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en FilaEsperaLN.Listar");
            return Respuesta<IEnumerable<TFilaEspera>>.Error(ex.Message);
        }
    }

    // Busca un registro específico por su PK (FEId)
    public Respuesta<TFilaEspera> Buscar(TFilaEspera entidad)
    {
        try
        {
            if (entidad.FeId <= 0)
                return Respuesta<TFilaEspera>.Validacion("El ID no es válido.");

            var respuesta = _unidadTrabajo.TFilaEspera
                .ObtenerEntidad(f => f.FeId == entidad.FeId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<TFilaEspera>.Validacion("Registro no encontrado.");

            var tipada = _mapper.Map<TFilaEspera>(respuesta.ValorRetorno);
            return Respuesta<TFilaEspera>.Exito(tipada, "Registro encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en FilaEsperaLN.Buscar");
            return Respuesta<TFilaEspera>.Error(ex.Message);
        }
    }

    // Obtiene registros filtrados según los campos del objeto recibido
    public Respuesta<IEnumerable<TFilaEspera>> Obtener(TFilaEspera entidad)
    {
        try
        {
            // Filtramos por ID si viene especificado (mayor que 0)
            var respuesta = _unidadTrabajo.TFilaEspera.Buscar(
                x => entidad.FeId <= 0 || x.FeId == entidad.FeId
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TFilaEspera>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TFilaEspera>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TFilaEspera>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en FilaEsperaLN.Obtener");
            return Respuesta<IEnumerable<TFilaEspera>>.Error(ex.Message);
        }
    }

    // Inserta un nuevo registro validando los campos requeridos
    public Respuesta<TFilaEspera> Insertar(TFilaEspera entidad)
    {
        try
        {
            // Validamos que los campos clave no vengan vacíos o en cero
            if (entidad.UsuId <= 0 || entidad.SalId <= 0)
                return Respuesta<TFilaEspera>.Validacion("El usuario y la sala son requeridos.");

            var mapped = _mapper.Map<FilaEspera>(entidad);
            var insertar = _unidadTrabajo.TFilaEspera.Insertar(mapped);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<TFilaEspera>.Error(insertar.strMensajeRespuesta);

            // Completar guarda los cambios en la base de datos
            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TFilaEspera>(insertar.ValorRetorno);
            return Respuesta<TFilaEspera>.Exito(tipada, "Registro insertado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en FilaEsperaLN.Insertar");
            return Respuesta<TFilaEspera>.Error(ex.Message);
        }
    }

    // Modifica un registro existente verificando que exista en BD
    public Respuesta<TFilaEspera> Modificar(TFilaEspera entidad)
    {
        try
        {
            if (entidad.FeId <= 0)
                return Respuesta<TFilaEspera>.Validacion("El ID no es válido.");

            // Verificamos que el registro exista antes de modificar
            var existe = _unidadTrabajo.TFilaEspera.ObtenerEntidad(f => f.FeId == entidad.FeId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<TFilaEspera>.Validacion("El registro no existe.");

            var mapped = _mapper.Map<FilaEspera>(entidad);
            var modificar = _unidadTrabajo.TFilaEspera.Modificar(mapped);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<TFilaEspera>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TFilaEspera>(modificar.ValorRetorno);
            return Respuesta<TFilaEspera>.Exito(tipada, "Registro modificado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en FilaEsperaLN.Modificar");
            return Respuesta<TFilaEspera>.Error(ex.Message);
        }
    }

    // Elimina un registro verificando que exista en BD antes de proceder
    public Respuesta<bool> Eliminar(TFilaEspera entidad)
    {
        try
        {
            if (entidad.FeId <= 0)
                return Respuesta<bool>.Validacion("El ID no es válido.");

            // Buscamos la entidad real de EF para poder eliminarla
            var existe = _unidadTrabajo.TFilaEspera.ObtenerEntidad(f => f.FeId == entidad.FeId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("El registro no existe.");

            var eliminar = _unidadTrabajo.TFilaEspera.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            return Respuesta<bool>.Exito(true, "Registro eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en FilaEsperaLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }
}
