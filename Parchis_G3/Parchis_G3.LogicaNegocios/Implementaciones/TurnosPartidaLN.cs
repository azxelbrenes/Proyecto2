using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class TurnosPartidaLN : ITurnosPartidaLN
{
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<TurnosPartidaLN> _logger;

    public TurnosPartidaLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<TurnosPartidaLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // Devuelve todos los registros de el turno
    public Respuesta<IEnumerable<TTurnosPartidum>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.TTurnoPartida.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TTurnosPartidum>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TTurnosPartidum>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TTurnosPartidum>>.Exito(tipadas, "Registros obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TurnosPartidaLN.Listar");
            return Respuesta<IEnumerable<TTurnosPartidum>>.Error(ex.Message);
        }
    }

    // Busca un registro específico por su PK (TurId)
    public Respuesta<TTurnosPartidum> Buscar(TTurnosPartidum entidad)
    {
        try
        {
            if (entidad.TurId <= 0)
                return Respuesta<TTurnosPartidum>.Validacion("El ID no es válido.");

            var respuesta = _unidadTrabajo.TTurnoPartida
                .ObtenerEntidad(t => t.TurId == entidad.TurId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<TTurnosPartidum>.Validacion("Registro no encontrado.");

            var tipada = _mapper.Map<TTurnosPartidum>(respuesta.ValorRetorno);
            return Respuesta<TTurnosPartidum>.Exito(tipada, "Registro encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TurnosPartidaLN.Buscar");
            return Respuesta<TTurnosPartidum>.Error(ex.Message);
        }
    }

    // Obtiene registros filtrados según los campos del objeto recibido
    public Respuesta<IEnumerable<TTurnosPartidum>> Obtener(TTurnosPartidum entidad)
    {
        try
        {
            // Filtramos por ID si viene especificado (mayor que 0)
            var respuesta = _unidadTrabajo.TTurnoPartida.Buscar(
                x => entidad.TurId <= 0 || x.TurId == entidad.TurId
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TTurnosPartidum>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TTurnosPartidum>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TTurnosPartidum>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TurnosPartidaLN.Obtener");
            return Respuesta<IEnumerable<TTurnosPartidum>>.Error(ex.Message);
        }
    }

    // Inserta un nuevo registro validando los campos requeridos
    public Respuesta<TTurnosPartidum> Insertar(TTurnosPartidum entidad)
    {
        try
        {
            // Validamos que los campos clave no vengan vacíos o en cero
            if (entidad.ParId <= 0 || entidad.JpId <= 0)
                return Respuesta<TTurnosPartidum>.Validacion("La partida y el jugador son requeridos.");

            var mapped = _mapper.Map<TurnosPartidum>(entidad);
            var insertar = _unidadTrabajo.TTurnoPartida.Insertar(mapped);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<TTurnosPartidum>.Error(insertar.strMensajeRespuesta);

            // Completar guarda los cambios en la base de datos
            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TTurnosPartidum>(insertar.ValorRetorno);
            return Respuesta<TTurnosPartidum>.Exito(tipada, "Registro insertado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TurnosPartidaLN.Insertar");
            return Respuesta<TTurnosPartidum>.Error(ex.Message);
        }
    }

    // Modifica un registro existente verificando que exista en BD
    public Respuesta<TTurnosPartidum> Modificar(TTurnosPartidum entidad)
    {
        try
        {
            if (entidad.TurId <= 0)
                return Respuesta<TTurnosPartidum>.Validacion("El ID no es válido.");

            // Verificamos que el registro exista antes de modificar
            var existe = _unidadTrabajo.TTurnoPartida.ObtenerEntidad(t => t.TurId == entidad.TurId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<TTurnosPartidum>.Validacion("El registro no existe.");

            var mapped = _mapper.Map<TurnosPartidum>(entidad);
            var modificar = _unidadTrabajo.TTurnoPartida.Modificar(mapped);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<TTurnosPartidum>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TTurnosPartidum>(modificar.ValorRetorno);
            return Respuesta<TTurnosPartidum>.Exito(tipada, "Registro modificado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TurnosPartidaLN.Modificar");
            return Respuesta<TTurnosPartidum>.Error(ex.Message);
        }
    }

    // Elimina un registro verificando que exista en BD antes de proceder
    public Respuesta<bool> Eliminar(TTurnosPartidum entidad)
    {
        try
        {
            if (entidad.TurId <= 0)
                return Respuesta<bool>.Validacion("El ID no es válido.");

            // Buscamos la entidad real de EF para poder eliminarla
            var existe = _unidadTrabajo.TTurnoPartida.ObtenerEntidad(t => t.TurId == entidad.TurId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("El registro no existe.");

            var eliminar = _unidadTrabajo.TTurnoPartida.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            return Respuesta<bool>.Exito(true, "Registro eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TurnosPartidaLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }
}
