using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class MensajesChatLN : IMensajesChatLN
{
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<MensajesChatLN> _logger;

    public MensajesChatLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<MensajesChatLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // Devuelve todos los registros de el mensaje
    public Respuesta<IEnumerable<TMensajesChat>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.TMensajesChat.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TMensajesChat>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TMensajesChat>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TMensajesChat>>.Exito(tipadas, "Registros obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MensajesChatLN.Listar");
            return Respuesta<IEnumerable<TMensajesChat>>.Error(ex.Message);
        }
    }

    // Busca un registro específico por su PK (McId)
    public Respuesta<TMensajesChat> Buscar(TMensajesChat entidad)
    {
        try
        {
            if (entidad.McId <= 0)
                return Respuesta<TMensajesChat>.Validacion("El ID no es válido.");

            var respuesta = _unidadTrabajo.TMensajesChat
                .ObtenerEntidad(m => m.McId == entidad.McId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<TMensajesChat>.Validacion("Registro no encontrado.");

            var tipada = _mapper.Map<TMensajesChat>(respuesta.ValorRetorno);
            return Respuesta<TMensajesChat>.Exito(tipada, "Registro encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MensajesChatLN.Buscar");
            return Respuesta<TMensajesChat>.Error(ex.Message);
        }
    }

    // Obtiene registros filtrados según los campos del objeto recibido
    public Respuesta<IEnumerable<TMensajesChat>> Obtener(TMensajesChat entidad)
    {
        try
        {
            // Filtramos por ID si viene especificado (mayor que 0)
            var respuesta = _unidadTrabajo.TMensajesChat.Buscar(
                x => entidad.McId <= 0 || x.McId == entidad.McId
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TMensajesChat>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TMensajesChat>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TMensajesChat>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MensajesChatLN.Obtener");
            return Respuesta<IEnumerable<TMensajesChat>>.Error(ex.Message);
        }
    }

    // Inserta un nuevo registro validando los campos requeridos
    public Respuesta<TMensajesChat> Insertar(TMensajesChat entidad)
    {
        try
        {
            // Validamos que los campos clave no vengan vacíos o en cero
            if (entidad.ParId <= 0 || string.IsNullOrWhiteSpace(entidad.McContenido))
                return Respuesta<TMensajesChat>.Validacion("La partida y el contenido del mensaje son requeridos.");

            var mapped = _mapper.Map<MensajesChat>(entidad);
            var insertar = _unidadTrabajo.TMensajesChat.Insertar(mapped);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<TMensajesChat>.Error(insertar.strMensajeRespuesta);

            // Completar guarda los cambios en la base de datos
            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TMensajesChat>(insertar.ValorRetorno);
            return Respuesta<TMensajesChat>.Exito(tipada, "Registro insertado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MensajesChatLN.Insertar");
            return Respuesta<TMensajesChat>.Error(ex.Message);
        }
    }

    // Modifica un registro existente verificando que exista en BD
    public Respuesta<TMensajesChat> Modificar(TMensajesChat entidad)
    {
        try
        {
            if (entidad.McId <= 0)
                return Respuesta<TMensajesChat>.Validacion("El ID no es válido.");

            // Verificamos que el registro exista antes de modificar
            var existe = _unidadTrabajo.TMensajesChat.ObtenerEntidad(m => m.McId == entidad.McId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<TMensajesChat>.Validacion("El registro no existe.");

            var mapped = _mapper.Map<MensajesChat>(entidad);
            var modificar = _unidadTrabajo.TMensajesChat.Modificar(mapped);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<TMensajesChat>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TMensajesChat>(modificar.ValorRetorno);
            return Respuesta<TMensajesChat>.Exito(tipada, "Registro modificado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MensajesChatLN.Modificar");
            return Respuesta<TMensajesChat>.Error(ex.Message);
        }
    }

    // Elimina un registro verificando que exista en BD antes de proceder
    public Respuesta<bool> Eliminar(TMensajesChat entidad)
    {
        try
        {
            if (entidad.McId <= 0)
                return Respuesta<bool>.Validacion("El ID no es válido.");

            // Buscamos la entidad real de EF para poder eliminarla
            var existe = _unidadTrabajo.TMensajesChat.ObtenerEntidad(m => m.McId == entidad.McId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("El registro no existe.");

            var eliminar = _unidadTrabajo.TMensajesChat.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            return Respuesta<bool>.Exito(true, "Registro eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en MensajesChatLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }
}
