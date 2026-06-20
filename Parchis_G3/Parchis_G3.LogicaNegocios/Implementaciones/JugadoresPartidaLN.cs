using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class JugadoresPartidaLN : IJugadoresPartidaLN
{
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<JugadoresPartidaLN> _logger;

    public JugadoresPartidaLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<JugadoresPartidaLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // Devuelve todos los registros de el jugador de partida
    public Respuesta<IEnumerable<TJugadoresPartidum>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.TJugadoresPartida.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TJugadoresPartidum>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TJugadoresPartidum>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TJugadoresPartidum>>.Exito(tipadas, "Registros obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en JugadoresPartidaLN.Listar");
            return Respuesta<IEnumerable<TJugadoresPartidum>>.Error(ex.Message);
        }
    }

    // Busca un registro específico por su PK (JpId)
    public Respuesta<TJugadoresPartidum> Buscar(TJugadoresPartidum entidad)
    {
        try
        {
            if (entidad.JpId <= 0)
                return Respuesta<TJugadoresPartidum>.Validacion("El ID no es válido.");

            var respuesta = _unidadTrabajo.TJugadoresPartida
                .ObtenerEntidad(j => j.JpId == entidad.JpId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<TJugadoresPartidum>.Validacion("Registro no encontrado.");

            var tipada = _mapper.Map<TJugadoresPartidum>(respuesta.ValorRetorno);
            return Respuesta<TJugadoresPartidum>.Exito(tipada, "Registro encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en JugadoresPartidaLN.Buscar");
            return Respuesta<TJugadoresPartidum>.Error(ex.Message);
        }
    }

    // Obtiene registros filtrados según los campos del objeto recibido
    public Respuesta<IEnumerable<TJugadoresPartidum>> Obtener(TJugadoresPartidum entidad)
    {
        try
        {
            // Filtramos por ID si viene especificado (mayor que 0)
            var respuesta = _unidadTrabajo.TJugadoresPartida.Buscar(
                x => entidad.JpId <= 0 || x.JpId == entidad.JpId
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TJugadoresPartidum>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TJugadoresPartidum>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TJugadoresPartidum>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en JugadoresPartidaLN.Obtener");
            return Respuesta<IEnumerable<TJugadoresPartidum>>.Error(ex.Message);
        }
    }

    // Inserta un nuevo registro validando los campos requeridos
    public Respuesta<TJugadoresPartidum> Insertar(TJugadoresPartidum entidad)
    {
        try
        {
            // Validamos que los campos clave no vengan vacíos o en cero
            if (entidad.ParId <= 0)
                return Respuesta<TJugadoresPartidum>.Validacion("La partida es requerida.");

            var mapped = _mapper.Map<JugadoresPartidum>(entidad);
            var insertar = _unidadTrabajo.TJugadoresPartida.Insertar(mapped);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<TJugadoresPartidum>.Error(insertar.strMensajeRespuesta);

            // Completar guarda los cambios en la base de datos
            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TJugadoresPartidum>(insertar.ValorRetorno);
            return Respuesta<TJugadoresPartidum>.Exito(tipada, "Registro insertado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en JugadoresPartidaLN.Insertar");
            return Respuesta<TJugadoresPartidum>.Error(ex.Message);
        }
    }

    // Modifica un registro existente verificando que exista en BD
    public Respuesta<TJugadoresPartidum> Modificar(TJugadoresPartidum entidad)
    {
        try
        {
            if (entidad.JpId <= 0)
                return Respuesta<TJugadoresPartidum>.Validacion("El ID no es válido.");

            // Verificamos que el registro exista antes de modificar
            var existe = _unidadTrabajo.TJugadoresPartida.ObtenerEntidad(j => j.JpId == entidad.JpId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<TJugadoresPartidum>.Validacion("El registro no existe.");

            var mapped = _mapper.Map<JugadoresPartidum>(entidad);
            var modificar = _unidadTrabajo.TJugadoresPartida.Modificar(mapped);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<TJugadoresPartidum>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TJugadoresPartidum>(modificar.ValorRetorno);
            return Respuesta<TJugadoresPartidum>.Exito(tipada, "Registro modificado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en JugadoresPartidaLN.Modificar");
            return Respuesta<TJugadoresPartidum>.Error(ex.Message);
        }
    }

    // Elimina un registro verificando que exista en BD antes de proceder
    public Respuesta<bool> Eliminar(TJugadoresPartidum entidad)
    {
        try
        {
            if (entidad.JpId <= 0)
                return Respuesta<bool>.Validacion("El ID no es válido.");

            // Buscamos la entidad real de EF para poder eliminarla
            var existe = _unidadTrabajo.TJugadoresPartida.ObtenerEntidad(j => j.JpId == entidad.JpId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("El registro no existe.");

            var eliminar = _unidadTrabajo.TJugadoresPartida.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            return Respuesta<bool>.Exito(true, "Registro eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en JugadoresPartidaLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }
}
