using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class SalaLN : ISalaLN
{
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<SalaLN> _logger;

    public SalaLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<SalaLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    public Respuesta<IEnumerable<TSala>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.TSala.Listar();
            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TSala>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TSala>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TSala>>.Exito(tipadas, "Salas obtenidas correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SalaLN.Listar");
            return Respuesta<IEnumerable<TSala>>.Error(ex.Message);
        }
    }

    public Respuesta<TSala> Buscar(TSala Sala)
    {
        try
        {
            if (Sala.SalId <= 0)
                return Respuesta<TSala>.Validacion("El ID de la sala no es válido.");

            var respuesta = _unidadTrabajo.TSala
                .ObtenerEntidad(s => s.SalId == Sala.SalId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<TSala>.Validacion("Sala no encontrada.");

            var tipada = _mapper.Map<TSala>(respuesta.ValorRetorno);
            return Respuesta<TSala>.Exito(tipada, "Sala encontrada.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SalaLN.Buscar");
            return Respuesta<TSala>.Error(ex.Message);
        }
    }

    public Respuesta<IEnumerable<TSala>> Obtener(TSala Sala)
    {
        try
        {
            // Filtramos por estado si viene especificado (ej: solo salas activas "A")
            var respuesta = _unidadTrabajo.TSala.Buscar(s =>
                string.IsNullOrEmpty(Sala.SalEstado) || s.SalEstado == Sala.SalEstado
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TSala>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TSala>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TSala>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SalaLN.Obtener");
            return Respuesta<IEnumerable<TSala>>.Error(ex.Message);
        }
    }

    public Respuesta<TSala> Insertar(TSala Sala)
    {
        try
        {
            // El nombre de la sala es obligatorio
            if (string.IsNullOrWhiteSpace(Sala.SalNombre))
                return Respuesta<TSala>.Validacion("El nombre de la sala es requerido.");

            // El costo de entrada debe ser positivo — no puede ser gratis ni negativo
            if (Sala.SalCostoEntrada <= 0)
                return Respuesta<TSala>.Validacion("El costo de entrada debe ser mayor a 0.");

            // El premio base debe ser mayor que el costo para que tenga sentido apostar
            if (Sala.SalPremioBase <= 0)
                return Respuesta<TSala>.Validacion("El premio base debe ser mayor a 0.");

            // La comisión debe estar entre 0 y 1 (0% a 100%)
            if (Sala.SalComision < 0 || Sala.SalComision > 1)
                return Respuesta<TSala>.Validacion("La comisión debe estar entre 0 y 1.");

            Sala.SalEstado = "A"; // Activa por defecto

            var entidad = _mapper.Map<Sala>(Sala);
            var insertar = _unidadTrabajo.TSala.Insertar(entidad);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<TSala>.Error(insertar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TSala>(insertar.ValorRetorno);
            return Respuesta<TSala>.Exito(tipada, "Sala creada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SalaLN.Insertar");
            return Respuesta<TSala>.Error(ex.Message);
        }
    }

    public Respuesta<TSala> Modificar(TSala Sala)
    {
        try
        {
            if (Sala.SalId <= 0)
                return Respuesta<TSala>.Validacion("El ID de la sala no es válido.");

            if (string.IsNullOrWhiteSpace(Sala.SalNombre))
                return Respuesta<TSala>.Validacion("El nombre de la sala es requerido.");

            var existe = _unidadTrabajo.TSala.ObtenerEntidad(s => s.SalId == Sala.SalId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<TSala>.Validacion("La sala no existe.");

            var entidad = _mapper.Map<Sala>(Sala);
            var modificar = _unidadTrabajo.TSala.Modificar(entidad);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<TSala>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TSala>(modificar.ValorRetorno);
            return Respuesta<TSala>.Exito(tipada, "Sala modificada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SalaLN.Modificar");
            return Respuesta<TSala>.Error(ex.Message);
        }
    }

    public Respuesta<bool> Eliminar(TSala Sala)
    {
        try
        {
            if (Sala.SalId <= 0)
                return Respuesta<bool>.Validacion("El ID de la sala no es válido.");

            var existe = _unidadTrabajo.TSala.ObtenerEntidad(s => s.SalId == Sala.SalId);
            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("La sala no existe.");

            var eliminar = _unidadTrabajo.TSala.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            return Respuesta<bool>.Exito(true, "Sala eliminada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SalaLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }
}
