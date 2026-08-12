using AutoMapper;
using Microsoft.Extensions.Logging;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;
using System;
using System.Linq.Expressions;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class UsuarioLN : IUsuarioLN
{

    // IUnidadTrabajoEF: acceso a todos los repositorios de la BD
    // IMapper: convierte entre Usuario (entidad EF) y TUsuario (tipada)
    // ILogger: registra errores en la consola/logs del servidor
    private readonly IUnidadTrabajoEF _unidadTrabajo;
    private readonly IMapper _mapper;
    private readonly ILogger<UsuarioLN> _logger;

    public UsuarioLN(IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<UsuarioLN> logger)
    {
        _unidadTrabajo = unidadTrabajo;
        _mapper = mapper;
        _logger = logger;
    }

    // ── LISTAR ──────────────────────────────────────────────────
    // Devuelve todos los usuarios de la BD mapeados a TUsuario.
    // Se usa en el panel de administración para ver todos los jugadores.
    public Respuesta<IEnumerable<TUsuario>> Listar()
    {
        try
        {
            var respuesta = _unidadTrabajo.TUsuario.Listar();

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TUsuario>>.Error(respuesta.strMensajeRespuesta);

            // Convertimos cada Usuario (EF) a TUsuario (tipada) para no
            // exponer propiedades internas de EF como las navegaciones
            var tipadas = _mapper.Map<IEnumerable<TUsuario>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TUsuario>>.Exito(tipadas, "Usuarios obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioLN.Listar");
            return Respuesta<IEnumerable<TUsuario>>.Error(ex.Message);
        }
    }

    // ── BUSCAR ──────────────────────────────────────────────────
    // Busca un usuario específico por su ID (UsuId).
    // Se usa cuando la API necesita los datos de un jugador en particular.
    public Respuesta<TUsuario> Buscar(TUsuario Usuario)
    {
        try
        {
            // Validamos que el ID sea válido antes de buscar en la BD
            if (Usuario.UsuId <= 0)
                return Respuesta<TUsuario>.Validacion("El ID del usuario no es válido.");

            var respuesta = _unidadTrabajo.TUsuario
                .ObtenerEntidad(u => u.UsuId == Usuario.UsuId);

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<TUsuario>.Validacion("Usuario no encontrado.");

            var tipada = _mapper.Map<TUsuario>(respuesta.ValorRetorno);
            return Respuesta<TUsuario>.Exito(tipada, "Usuario encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioLN.Buscar");
            return Respuesta<TUsuario>.Error(ex.Message);
        }
    }

    //metodo obtener
    // Busca usuarios usando filtros dinámicos (por correo, estado, etc.)
    // Se usa para verificar si un correo ya existe o buscar por criterios.
    public Respuesta<IEnumerable<TUsuario>> Obtener(TUsuario Usuario)
    {
        try
        {
            // Filtramos por correo si viene en el objeto — útil para
            // verificar duplicados antes del registro
            var respuesta = _unidadTrabajo.TUsuario.Buscar(u =>
                (string.IsNullOrEmpty(Usuario.UsuCorreo) || u.UsuCorreo == Usuario.UsuCorreo) &&
                (string.IsNullOrEmpty(Usuario.UsuEstado) || u.UsuEstado == Usuario.UsuEstado)
            );

            if (!respuesta.blnIndicadorTransaccion)
                return Respuesta<IEnumerable<TUsuario>>.Error(respuesta.strMensajeRespuesta);

            var tipadas = _mapper.Map<IEnumerable<TUsuario>>(respuesta.ValorRetorno);
            return Respuesta<IEnumerable<TUsuario>>.Exito(tipadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioLN.Obtener");
            return Respuesta<IEnumerable<TUsuario>>.Error(ex.Message);
        }
    }

    // metodo insertar
    // Registra un nuevo jugador en el sistema.
    // Valida datos, verifica que el correo no exista y asigna monedas.
    // NOTA: El hash de la contraseña se hace en la API con BCrypt
    //       antes de llegar aquí — nunca almacenamos texto plano.
    public Respuesta<TUsuario> Insertar(TUsuario Usuario)
    {
        try
        {
            // ── Validaciones de campos requeridos ────────────────
            if (string.IsNullOrWhiteSpace(Usuario.UsuNombre))
                return Respuesta<TUsuario>.Validacion("El nombre del usuario es requerido.");

            if (string.IsNullOrWhiteSpace(Usuario.UsuCorreo))
                return Respuesta<TUsuario>.Validacion("El correo electrónico es requerido.");

            if (string.IsNullOrWhiteSpace(Usuario.UsuPasswordHash))
                return Respuesta<TUsuario>.Validacion("La contraseña es requerida.");

            // ── Verificar que el correo no esté ya registrado ────
            // Si ya existe un usuario con ese correo, rechazamos el registro
            var existeCorreo = _unidadTrabajo.TUsuario
                .ObtenerEntidad(u => u.UsuCorreo == Usuario.UsuCorreo);

            if (existeCorreo.blnIndicadorTransaccion)
                return Respuesta<TUsuario>.Validacion("El correo ya está registrado en el sistema.");

            // Valores por defecto al registrarse en el app
            // Todo jugador nuevo empieza con 5,000 monedas de bienvenida,
            // avatar 1, sin bloqueo y con estado activo
            Usuario.UsuMonedasTotal = 5000;
            Usuario.UsuMonedasGanadasPartida = 0;
            Usuario.UsuAvatar = Usuario.UsuAvatar <= 0 ? 1 : Usuario.UsuAvatar;
            Usuario.UsuBloqueado = false;
            Usuario.UsuAbandonosConsecutivos = 0;
            Usuario.UsuTutorialCompletado = false;
            Usuario.UsuNotificacionesActivas = true;
            Usuario.UsuSonidosActivos = true;
            Usuario.UsuMusicaActiva = true;
            Usuario.UsuEstado = "A";
            Usuario.UsuFechaCreacion = DateTime.Now;

            // ── Mapear y guardar ─────────────────────────────────
            var entidad = _mapper.Map<Usuario>(Usuario);
            var insertar = _unidadTrabajo.TUsuario.Insertar(entidad);

            if (!insertar.blnIndicadorTransaccion)
                return Respuesta<TUsuario>.Error(insertar.strMensajeRespuesta);

            // Completar persiste los cambios en la base de datos
            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TUsuario>(insertar.ValorRetorno);
            return Respuesta<TUsuario>.Exito(tipada, "Usuario registrado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioLN.Insertar");
            return Respuesta<TUsuario>.Error(ex.Message);
        }
    }

    //modificar
    // Actualiza los datos de un usuario existente.
    // No permite modificar el correo — para eso habría un flujo separado.
    public Respuesta<TUsuario> Modificar(TUsuario Usuario)
    {
        try
        {
            if (Usuario.UsuId <= 0)
                return Respuesta<TUsuario>.Validacion("El ID del usuario no es válido.");

            if (string.IsNullOrWhiteSpace(Usuario.UsuNombre))
                return Respuesta<TUsuario>.Validacion("El nombre del usuario es requerido.");

            // Verificamos que el usuario a modificar exista en BD
            var existe = _unidadTrabajo.TUsuario
                .ObtenerEntidad(u => u.UsuId == Usuario.UsuId);

            if (!existe.blnIndicadorTransaccion)
                return Respuesta<TUsuario>.Validacion("El usuario no existe.");

            var entidad = _mapper.Map<Usuario>(Usuario);
            var modificar = _unidadTrabajo.TUsuario.Modificar(entidad);

            if (!modificar.blnIndicadorTransaccion)
                return Respuesta<TUsuario>.Error(modificar.strMensajeRespuesta);

            _unidadTrabajo.Completar();

            var tipada = _mapper.Map<TUsuario>(modificar.ValorRetorno);
            return Respuesta<TUsuario>.Exito(tipada, "Usuario modificado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UsuarioLN.Modificar");
            return Respuesta<TUsuario>.Error(ex.Message);
        }
    }

    // Eliminar
    // Elimina la cuenta del usuario y TODOS sus datos relacionados.
    //
    // 7 tablas apuntan a Usuarios por llave foránea (Usu_ID) y ninguna
    // tiene ON DELETE CASCADE en la base de datos, así que SQL Server
    // rechaza el DELETE de Usuarios mientras quede algo apuntándole.
    // Por eso hay que borrar primero, a mano, todo lo que depende del
    // usuario, en el orden correcto, y recién al final borrar su fila.
    //
    // Todo corre dentro de una transacción: si un paso falla, no queda
    // el borrado a medias (por ejemplo, artículos borrados pero la
    // cuenta todavía viva).
    //
    // Los SegLogs NO se borran: se les quita la referencia al usuario
    // (Usu_ID = NULL) para conservar la bitácora de seguridad aunque
    // la cuenta ya no exista.
    public Respuesta<bool> Eliminar(TUsuario Usuario)
    {
        try
        {
            if (Usuario.UsuId <= 0)
                return Respuesta<bool>.Validacion("El ID del usuario no es válido.");

            // Buscamos la entidad real en BD para eliminarla
            var existe = _unidadTrabajo.TUsuario
                .ObtenerEntidad(u => u.UsuId == Usuario.UsuId);

            if (!existe.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("El usuario no existe.");

            _unidadTrabajo.EmpezarTransaccion();

            // 1) Partidas jugadas por el usuario: primero lo que depende
            //    de cada JugadoresPartida (fichas, turnos, chat), luego
            //    la fila de JugadoresPartida en sí.
            var jugadas = _unidadTrabajo.TJugadoresPartida
                .Buscar(jp => jp.UsuId == Usuario.UsuId);

            if (!jugadas.blnIndicadorTransaccion)
                throw new Exception(jugadas.strMensajeRespuesta);

            foreach (var jp in jugadas.ValorRetorno!)
            {
                BorrarTodos(_unidadTrabajo.TEstadoFicha, f => f.JpId == jp.JpId);
                BorrarTodos(_unidadTrabajo.TTurnoPartida, t => t.JpId == jp.JpId);
                BorrarTodos(_unidadTrabajo.TMensajesChat, m => m.JpId == jp.JpId);
                _unidadTrabajo.TJugadoresPartida.Eliminar(jp);
            }

            // 2) Tablas que dependen directamente del usuario
            BorrarTodos(_unidadTrabajo.TUsuarioArticulo, ua => ua.UsuId == Usuario.UsuId);
            BorrarTodos(_unidadTrabajo.TEquipamientoActivo, e => e.UsuId == Usuario.UsuId);
            BorrarTodos(_unidadTrabajo.TTransaccion, t => t.UsuId == Usuario.UsuId);
            BorrarTodos(_unidadTrabajo.THistorialPartida, h => h.UsuId == Usuario.UsuId);
            BorrarTodos(_unidadTrabajo.TFilaEspera, f => f.UsuId == Usuario.UsuId);
            BorrarTodos(_unidadTrabajo.TSesionesActiva, s => s.UsuId == Usuario.UsuId);

            // 3) Bitácora de seguridad: se conserva, solo se desvincula
            //    del usuario (Usu_ID admite NULL para este caso).
            var logs = _unidadTrabajo.TSegLog.Buscar(l => l.UsuId == Usuario.UsuId);
            if (!logs.blnIndicadorTransaccion)
                throw new Exception(logs.strMensajeRespuesta);

            foreach (var log in logs.ValorRetorno!)
            {
                log.UsuId = null;
                _unidadTrabajo.TSegLog.Modificar(log);
            }

            // 4) Por último, el usuario
            var eliminar = _unidadTrabajo.TUsuario.Eliminar(existe.ValorRetorno!);
            if (!eliminar.blnIndicadorTransaccion)
                throw new Exception(eliminar.strMensajeRespuesta);

            _unidadTrabajo.Completar();
            _unidadTrabajo.CompletarTran();

            return Respuesta<bool>.Exito(true, "Usuario eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _unidadTrabajo.Rollback();
            _logger.LogError(ex, "Error en UsuarioLN.Eliminar");
            return Respuesta<bool>.Error(ex.Message);
        }
    }

    // Busca todos los registros de un repositorio que cumplan la
    // condición dada y los marca para borrar. Se usa para las tablas
    // hijas de Usuarios que no tienen, a su vez, tablas dependientes
    // propias (a diferencia de JugadoresPartida).
    private void BorrarTodos<T>(IRepositorioAD<T> repo, Expression<Func<T, bool>> condicion) where T : class
    {
        var resultado = repo.Buscar(condicion);
        if (!resultado.blnIndicadorTransaccion)
            throw new Exception(resultado.strMensajeRespuesta);

        foreach (var item in resultado.ValorRetorno!)
            repo.Eliminar(item);
    }
}
