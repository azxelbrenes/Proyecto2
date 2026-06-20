using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class UsuarioArticuloLN : IUsuarioArticuloLN
{
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<UsuarioArticuloLN> _logger;

    public UsuarioArticuloLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<UsuarioArticuloLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // Devuelve todos los registros de el artículo del usuario
    public Respuesta<IEnumerable<TUsuarioArticulo>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.TUsuarioArticulo.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TUsuarioArticulo>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TUsuarioArticulo>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TUsuarioArticulo>>.Exito(tipadas, "Registros obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioArticuloLN.Listar");
            return Respuesta<IEnumerable<TUsuarioArticulo>>.Error(ex.Message);
        }
    }

    // Busca un registro específico por su PK (UartId)
    public Respuesta<TUsuarioArticulo> Buscar(TUsuarioArticulo entidad)
    {
        try
        {
            if (entidad.UartId <= 0)
                return Respuesta<TUsuarioArticulo>.Validacion("El ID no es válido.");

            var respuesta = _unidadTrabajo.TUsuarioArticulo
                .ObtenerEntidad(u => u.UartId == entidad.UartId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<TUsuarioArticulo>.Validacion("Registro no encontrado.");

            var tipada = _mapper.Map<TUsuarioArticulo>(respuesta.ValorRetorno);
            return Respuesta<TUsuarioArticulo>.Exito(tipada, "Registro encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioArticuloLN.Buscar");
            return Respuesta<TUsuarioArticulo>.Error(ex.Message);
        }
    }

    // Obtiene registros filtrados según los campos del objeto recibido
    public Respuesta<IEnumerable<TUsuarioArticulo>> Obtener(TUsuarioArticulo entidad)
    {
        try
        {
            // Filtramos por ID si viene especificado (mayor que 0)
            var respuesta = _unidadTrabajo.TUsuarioArticulo.Buscar(
                x => entidad.UartId <= 0 || x.UartId == entidad.UartId
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TUsuarioArticulo>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TUsuarioArticulo>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TUsuarioArticulo>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioArticuloLN.Obtener");
            return Respuesta<IEnumerable<TUsuarioArticulo>>.Error(ex.Message);
        }
    }

    // Inserta un nuevo registro validando los campos requeridos
    public Respuesta<TUsuarioArticulo> Insertar(TUsuarioArticulo entidad)
    {
        try
        {
            // Validamos que los campos clave no vengan vacíos o en cero
            if (entidad.UsuId <= 0 || entidad.ArtId <= 0)
                return Respuesta<TUsuarioArticulo>.Validacion("El usuario y el artículo son requeridos.");

            var mapped = _mapper.Map<UsuarioArticulo>(entidad);
            var insertar = _unidadTrabajo.TUsuarioArticulo.Insertar(mapped);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<TUsuarioArticulo>.Error(insertar.strMensajeRespuesta);

            // Completar guarda los cambios en la base de datos
            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TUsuarioArticulo>(insertar.ValorRetorno);
            return Respuesta<TUsuarioArticulo>.Exito(tipada, "Registro insertado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioArticuloLN.Insertar");
            return Respuesta<TUsuarioArticulo>.Error(ex.Message);
        }
    }

    // Modifica un registro existente verificando que exista en BD
    public Respuesta<TUsuarioArticulo> Modificar(TUsuarioArticulo entidad)
    {
        try
        {
            if (entidad.UartId <= 0)
                return Respuesta<TUsuarioArticulo>.Validacion("El ID no es válido.");

            // Verificamos que el registro exista antes de modificar
            var existe = _unidadTrabajo.TUsuarioArticulo.ObtenerEntidad(u => u.UartId == entidad.UartId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<TUsuarioArticulo>.Validacion("El registro no existe.");

            var mapped = _mapper.Map<UsuarioArticulo>(entidad);
            var modificar = _unidadTrabajo.TUsuarioArticulo.Modificar(mapped);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<TUsuarioArticulo>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TUsuarioArticulo>(modificar.ValorRetorno);
            return Respuesta<TUsuarioArticulo>.Exito(tipada, "Registro modificado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioArticuloLN.Modificar");
            return Respuesta<TUsuarioArticulo>.Error(ex.Message);
        }
    }

    // Elimina un registro verificando que exista en BD antes de proceder
    public Respuesta<bool> Eliminar(TUsuarioArticulo entidad)
    {
        try
        {
            if (entidad.UartId <= 0)
                return Respuesta<bool>.Validacion("El ID no es válido.");

            // Buscamos la entidad real de EF para poder eliminarla
            var existe = _unidadTrabajo.TUsuarioArticulo.ObtenerEntidad(u => u.UartId == entidad.UartId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("El registro no existe.");

            var eliminar = _unidadTrabajo.TUsuarioArticulo.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            return Respuesta<bool>.Exito(true, "Registro eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioArticuloLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }
}
