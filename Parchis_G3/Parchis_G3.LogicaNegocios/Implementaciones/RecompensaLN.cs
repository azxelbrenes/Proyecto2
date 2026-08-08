using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class RecompensaLN : IRecompensaLN
{
    // Monedas por cada día de racha (índice 0 = día 1)
    private static readonly int[] MonedasPorDia = { 200, 400, 600, 800, 1000 };

    private const int RACHA_MAXIMA = 5;

    // ================================================================
    // OBTENER ESTADO
    // ================================================================
    // El frontend llama a esto al abrir la app para decidir si
    // muestra el modal de recompensa o no.
    public Respuesta<EstadoRecompensaDTO> ObtenerEstado(int usuId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            var usuarioResp = unidadTrabajo.TUsuario.ObtenerEntidad(u => u.UsuId == usuId);

            if (!usuarioResp.blnIndicadorTransaccion)
                return Respuesta<EstadoRecompensaDTO>.Validacion("Usuario no encontrado.");

            var usuario = usuarioResp.ValorRetorno!;

            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var ultimaConexion = usuario.UsuUltimaConexion;

            // ¿Ya reclamó hoy?
            bool yaReclamoHoy = ultimaConexion.HasValue && ultimaConexion.Value == hoy;

            // Calculamos qué racha tendría si reclamara ahora
            int rachaProyectada = CalcularRachaProyectada(usuario, hoy);

            var dto = new EstadoRecompensaDTO
            {
                PuedeReclamar = !yaReclamoHoy,
                RachaActual = usuario.UsuRachaDias,
                MonedasHoy = ObtenerMonedas(rachaProyectada),
                MonedasSiguienteDia = ObtenerMonedas(Math.Min(rachaProyectada + 1, RACHA_MAXIMA)),
                UltimaReclamacion = ultimaConexion?.ToDateTime(TimeOnly.MinValue),
                Mensaje = yaReclamoHoy
                    ? "Ya reclamaste tu recompensa de hoy. ¡Volvé mañana!"
                    : $"¡Reclamá tus {ObtenerMonedas(rachaProyectada)} monedas del día {rachaProyectada}!"
            };

            return Respuesta<EstadoRecompensaDTO>.Exito(dto, "Estado obtenido.");
        }
        catch (Exception ex)
        {
            return Respuesta<EstadoRecompensaDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // RECLAMAR RECOMPENSA
    // ================================================================
    public Respuesta<ResultadoRecompensaDTO> Reclamar(int usuId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            var usuarioResp = unidadTrabajo.TUsuario.ObtenerEntidad(u => u.UsuId == usuId);

            if (!usuarioResp.blnIndicadorTransaccion)
                return Respuesta<ResultadoRecompensaDTO>.Validacion("Usuario no encontrado.");

            var usuario = usuarioResp.ValorRetorno!;

            var hoy = DateOnly.FromDateTime(DateTime.Now);

            // ── Protección contra doble reclamo ──────────────────
            // Sin esto, alguien podría llamar al endpoint 100 veces
            // seguidas y llenarse de monedas gratis
            if (usuario.UsuUltimaConexion.HasValue && usuario.UsuUltimaConexion.Value == hoy)
            {
                return Respuesta<ResultadoRecompensaDTO>.Validacion(
                    "Ya reclamaste tu recompensa de hoy. ¡Volvé mañana!");
            }

            // ── Calcular la nueva racha ──────────────────────────
            int rachaAnterior = usuario.UsuRachaDias;
            int rachaNueva = CalcularRachaProyectada(usuario, hoy);
            bool seReinicio = rachaNueva == 1 && rachaAnterior > 1;

            int monedas = ObtenerMonedas(rachaNueva);

            // ── Acreditar ────────────────────────────────────────
            // IMPORTANTE: solo tocamos UsuMonedasTotal.
            // NO tocamos UsuMonedasGanadasPartida — ese campo es
            // exclusivo del ranking y solo sube ganando partidas.
            // Si lo sumáramos acá, alguien podría escalar el ranking
            // simplemente entrando todos los días sin jugar.
            usuario.UsuMonedasTotal += monedas;
            usuario.UsuRachaDias = rachaNueva;
            usuario.UsuUltimaConexion = hoy;

            unidadTrabajo.TUsuario.Modificar(usuario);

            // ── Registrar en el historial de transacciones ───────
            unidadTrabajo.TTransaccion.Insertar(new Transaccione
            {
                UsuId = usuId,
                ParId = null,  // no es de partida
                TranTipo = "RECOMPENSA_DIA",
                TranConcepto = $"Recompensa diaria - Día {rachaNueva} de racha",
                TranMonto = monedas,
                TranSaldoResultante = usuario.UsuMonedasTotal,
                TranFecha = DateTime.Now
            });

            unidadTrabajo.Completar();

            var dto = new ResultadoRecompensaDTO
            {
                Exitoso = true,
                MonedasOtorgadas = monedas,
                SaldoNuevo = usuario.UsuMonedasTotal,
                RachaNueva = rachaNueva,
                RachaReiniciada = seReinicio,
                Mensaje = seReinicio
                    ? $"Perdiste tu racha por faltar. Empezás de nuevo con {monedas} monedas."
                    : $"¡Día {rachaNueva} de racha! Ganaste {monedas} monedas."
            };

            return Respuesta<ResultadoRecompensaDTO>.Exito(dto, dto.Mensaje);
        }
        catch (Exception ex)
        {
            return Respuesta<ResultadoRecompensaDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // CALCULAR LA RACHA PROYECTADA
    // ================================================================
    // Determina en qué día de racha quedaría el usuario si reclamara
    // hoy, según cuándo fue su última reclamación.
    //
    //   Nunca reclamó         → día 1
    //   Reclamó AYER          → racha + 1 (tope en 5)
    //   Reclamó hace 2+ días  → día 1 (perdió la racha)
    // ================================================================
    private int CalcularRachaProyectada(Usuario usuario, DateOnly hoy)
    {
        // Primera vez que reclama
        if (!usuario.UsuUltimaConexion.HasValue)
            return 1;

        var ultima = usuario.UsuUltimaConexion.Value;
        var ayer = hoy.AddDays(-1);

        // Reclamó ayer → la racha continúa
        if (ultima == ayer)
        {
            int siguiente = usuario.UsuRachaDias + 1;

            // Al llegar al día 5 se queda ahí (no baja ni sube más)
            return Math.Min(siguiente, RACHA_MAXIMA);
        }

        // Reclamó hoy → mantiene la racha actual (no debería llegar
        // acá porque el método Reclamar lo bloquea antes, pero por
        // seguridad devolvemos la racha sin incrementar)
        if (ultima == hoy)
            return usuario.UsuRachaDias > 0 ? usuario.UsuRachaDias : 1;

        // Faltó uno o más días → se reinicia
        return 1;
    }

    // ── Monedas según el día de racha ────────────────────────────
    private int ObtenerMonedas(int dia)
    {
        if (dia < 1) dia = 1;
        if (dia > RACHA_MAXIMA) dia = RACHA_MAXIMA;

        // El array es base 0, la racha es base 1
        return MonedasPorDia[dia - 1];
    }
}
