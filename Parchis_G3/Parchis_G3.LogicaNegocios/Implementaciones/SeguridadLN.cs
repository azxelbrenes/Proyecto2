using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class SeguridadLN : ISeguridadLN
{
    // Tras cuántos fallos se bloquea la cuenta
    private const int MAX_INTENTOS_FALLIDOS = 5;

    // Cuánto dura el bloqueo
    private const int MINUTOS_BLOQUEO_LOGIN = 15;

    // Tras cuánto tiempo sin intentar, se resetea el contador.
    // Si alguien falló 3 veces hace una hora, no tiene sentido
    // seguir contándolos — probablemente solo olvidó la contraseña.
    private const int MINUTOS_RESET_CONTADOR = 30;

    
    // VERIFICAR SI LA CUENTA ESTÁ BLOQUEADA
   
    public string? VerificarBloqueoLogin(string correo, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            var usuarioResp = unidadTrabajo.TUsuario
                .ObtenerEntidad(u => u.UsuCorreo == correo);

            // Si el correo no existe, no revelamos esa información.
            // Devolvemos null para que siga el flujo normal y el
            // error sea el genérico "correo o contraseña incorrectos".
            // Esto evita que un atacante descubra qué correos existen.
            if (!usuarioResp.blnIndicadorTransaccion)
                return null;

            var usuario = usuarioResp.ValorRetorno!;

            // ¿Está bloqueado y el bloqueo sigue vigente?
            if (usuario.UsuBloqueado == true &&
                usuario.UsuFechaDesbloqueo.HasValue &&
                usuario.UsuFechaDesbloqueo > DateTime.Now)
            {
                var minutosRestantes = (int)Math.Ceiling(
                    (usuario.UsuFechaDesbloqueo.Value - DateTime.Now).TotalMinutes
                );

                return $"Cuenta bloqueada temporalmente. Intentá de nuevo en {minutosRestantes} minuto(s).";
            }

            // Si el bloqueo ya venció, lo levantamos automáticamente
            if (usuario.UsuBloqueado == true &&
                usuario.UsuFechaDesbloqueo.HasValue &&
                usuario.UsuFechaDesbloqueo <= DateTime.Now)
            {
                usuario.UsuBloqueado = false;
                usuario.UsuFechaDesbloqueo = null;
                usuario.UsuIntentosFallidos = 0;
                unidadTrabajo.TUsuario.Modificar(usuario);
                unidadTrabajo.Completar();
            }

            return null;  // Puede intentar
        }
        catch
        {
            // Si falla la verificación, dejamos pasar — no queremos
            // que un error de BD impida el login a todos los usuarios
            return null;
        }
    }

    // ================================================================
    // REGISTRAR INTENTO FALLIDO
    // ================================================================
    public void RegistrarIntentoFallido(string correo, string? ip, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            var usuarioResp = unidadTrabajo.TUsuario
                .ObtenerEntidad(u => u.UsuCorreo == correo);

            // Registramos el evento aunque el correo no exista —
            // así detectamos ataques de enumeración de usuarios
            if (!usuarioResp.blnIndicadorTransaccion)
            {
                RegistrarEvento("LOGIN_FALLIDO", correo, null, ip,
                    "Correo no registrado", unidadTrabajo);
                return;
            }

            var usuario = usuarioResp.ValorRetorno!;

            // ── Reset del contador si pasó mucho tiempo ──────────
            // Si el último intento fallido fue hace más de 30 min,
            // empezamos a contar de cero
            if (usuario.UsuFechaUltimoIntento.HasValue &&
                (DateTime.Now - usuario.UsuFechaUltimoIntento.Value).TotalMinutes > MINUTOS_RESET_CONTADOR)
            {
                usuario.UsuIntentosFallidos = 0;
            }

            usuario.UsuIntentosFallidos += 1;
            usuario.UsuFechaUltimoIntento = DateTime.Now;

            string detalle = $"Intento {usuario.UsuIntentosFallidos} de {MAX_INTENTOS_FALLIDOS}";

            // ── ¿Llegó al límite? Bloqueamos ─────────────────────
            if (usuario.UsuIntentosFallidos >= MAX_INTENTOS_FALLIDOS)
            {
                usuario.UsuBloqueado = true;
                usuario.UsuFechaDesbloqueo = DateTime.Now.AddMinutes(MINUTOS_BLOQUEO_LOGIN);

                detalle = $"Cuenta bloqueada por {MINUTOS_BLOQUEO_LOGIN} minutos tras {MAX_INTENTOS_FALLIDOS} intentos fallidos";

                RegistrarEvento("CUENTA_BLOQUEADA", correo, usuario.UsuId, ip, detalle, unidadTrabajo);
            }
            else
            {
                RegistrarEvento("LOGIN_FALLIDO", correo, usuario.UsuId, ip, detalle, unidadTrabajo);
            }

            unidadTrabajo.TUsuario.Modificar(usuario);
            unidadTrabajo.Completar();
        }
        catch
        {
            // Silencioso: un fallo al registrar no debe romper el login
        }
    }

    
    // REGISTRAR LOGIN EXITOSO
 
    public void RegistrarLoginExitoso(int usuId, string correo, string? ip, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            var usuarioResp = unidadTrabajo.TUsuario.ObtenerEntidad(u => u.UsuId == usuId);
            if (!usuarioResp.blnIndicadorTransaccion) return;

            var usuario = usuarioResp.ValorRetorno!;

            // Login correcto → reseteamos el contador de fallos
            usuario.UsuIntentosFallidos = 0;
            usuario.UsuFechaUltimoIntento = null;

            unidadTrabajo.TUsuario.Modificar(usuario);
            unidadTrabajo.Completar();

            RegistrarEvento("LOGIN_EXITOSO", correo, usuId, ip, null, unidadTrabajo);
        }
        catch
        {
            // Silencioso
        }
    }

    
    // REGISTRAR EVENTO DE AUDITORÍA
 
    public void RegistrarEvento(string evento, string? correo, int? usuId, string? ip, string? detalle, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            // NOTA: esto usa la tabla SegLogs creada por el script
            // de migración. Si aún no la creaste, este método falla
            // silenciosamente sin romper nada.
            var log = new SegLog
            {
                UsuId = usuId,
                LogCorreo = correo?.Length > 200 ? correo[..200] : correo,
                LogEvento = evento,
                LogIp = ip?.Length > 45 ? ip[..45] : ip,
                LogDetalle = detalle?.Length > 500 ? detalle[..500] : detalle,
                LogFecha = DateTime.Now
            };

            unidadTrabajo.TSegLog.Insertar(log);
            unidadTrabajo.Completar();
        }
        catch
        {
            // Silencioso: la auditoría nunca debe romper el flujo principal
        }
    }
}
