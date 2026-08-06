using System.Collections.Concurrent;
using AutoMapper;
using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Motor;

public class MatchmakingLN : IMatchmakingLN
{
    private readonly IMotorParchisLN _motor;

    // Segundos que espera el sistema por más jugadores humanos
    // antes de completar los cupos con bots (HU-05)
    private const int SEGUNDOS_ESPERA = 30;
    private const int MAX_JUGADORES = 4;

    // Orden fijo de asignación de colores según posición en la mesa
    private static readonly string[] ColoresPorPosicion =
        { "ROJO", "AZUL", "VERDE", "AMARILLO" };

    // ── Estado en memoria (esto justifica el Singleton) ──────────
    // parId -> momento en que se creó la partida y empezó la cuenta
    private readonly ConcurrentDictionary<int, DateTime> _inicioEspera = new();

    // Lock por sala para evitar que dos jugadores entrando al mismo
    // tiempo creen dos partidas distintas o tomen el mismo color
    private readonly ConcurrentDictionary<int, object> _locksPorSala = new();

    public MatchmakingLN(IMotorParchisLN motor)
    {
        _motor = motor;
    }

    // ================================================================
    // BUSCAR PARTIDA
    // ================================================================
    public Respuesta<ResultadoMatchmakingDTO> BuscarPartida(int usuId, int salId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper)
    {
        // Bloqueamos por sala: si dos jugadores tocan "Sala Bronce"
        // en el mismo milisegundo, uno espera al otro. Sin esto se
        // podrían crear partidas duplicadas o asignar el mismo color.
        var candado = _locksPorSala.GetOrAdd(salId, _ => new object());

        lock (candado)
        {
            try
            {
                // ── 1. Validar la sala ───────────────────────────
                var salaResp = unidadTrabajo.TSala.ObtenerEntidad(s => s.SalId == salId);
                if (!salaResp.blnIndicadorTransaccion)
                    return Respuesta<ResultadoMatchmakingDTO>.Validacion("La sala no existe.");

                var sala = salaResp.ValorRetorno!;

                // ── 2. Validar el usuario y su saldo ─────────────
                var usuarioResp = unidadTrabajo.TUsuario.ObtenerEntidad(u => u.UsuId == usuId);
                if (!usuarioResp.blnIndicadorTransaccion)
                    return Respuesta<ResultadoMatchmakingDTO>.Validacion("Usuario no encontrado.");

                var usuario = usuarioResp.ValorRetorno!;

                // Verificamos bloqueo por abandonos (HU-19)
                if (usuario.UsuBloqueado == true && usuario.UsuFechaDesbloqueo > DateTime.Now)
                    return Respuesta<ResultadoMatchmakingDTO>.Validacion(
                        $"Cuenta bloqueada hasta {usuario.UsuFechaDesbloqueo:dd/MM/yyyy HH:mm} por abandonos.");

                // El servidor SIEMPRE valida el precio real de la BD,
                // nunca confía en lo que manda el cliente
                if (usuario.UsuMonedasTotal < sala.SalCostoEntrada)
                    return Respuesta<ResultadoMatchmakingDTO>.Validacion(
                        $"Saldo insuficiente. Necesitás {sala.SalCostoEntrada} monedas.");

                // ── 3. Buscar partida esperando con cupo ─────────
                var partidasEsperando = unidadTrabajo.TPartida
                    .Buscar(p => p.SalId == salId && p.ParEstado == "ESPERANDO")
                    .ValorRetorno?.ToList() ?? new List<Partida>();

                Partida? partidaElegida = null;
                List<JugadoresPartidum> jugadoresActuales = new();

                foreach (var partida in partidasEsperando)
                {
                    var jugadores = unidadTrabajo.TJugadoresPartida
                        .Buscar(j => j.ParId == partida.ParId)
                        .ValorRetorno?.ToList() ?? new List<JugadoresPartidum>();

                    // Si el usuario YA está en esta partida, no lo duplicamos
                    if (jugadores.Any(j => j.UsuId == usuId))
                        return Respuesta<ResultadoMatchmakingDTO>.Validacion("Ya estás en esta partida.");

                    if (jugadores.Count < MAX_JUGADORES)
                    {
                        partidaElegida = partida;
                        jugadoresActuales = jugadores;
                        break;
                    }
                }

                // ── 4. Si no hay partida con cupo, creamos una ───
                if (partidaElegida == null)
                {
                    var nuevaPartida = new Partida
                    {
                        SalId = salId,
                        ParEstado = "ESPERANDO",
                        ParPremioTotal = 0
                    };

                    var insertResp = unidadTrabajo.TPartida.Insertar(nuevaPartida);
                    if (!insertResp.blnIndicadorTransaccion)
                        return Respuesta<ResultadoMatchmakingDTO>.Error(insertResp.strMensajeRespuesta);

                    // Completar() persiste y hace que EF asigne el ParId real
                    unidadTrabajo.Completar();

                    partidaElegida = insertResp.ValorRetorno!;
                    jugadoresActuales = new List<JugadoresPartidum>();

                    // Arrancamos el cronómetro de 30 segundos
                    _inicioEspera[partidaElegida.ParId] = DateTime.Now;
                }

                // ── 5. Asignar posición y color libres ───────────
                int posicion = jugadoresActuales.Count + 1;
                string color = ColoresPorPosicion[posicion - 1];

                var nuevoJugador = new JugadoresPartidum
                {
                    ParId = partidaElegida.ParId,
                    UsuId = usuId,
                    JpEsBot = false,
                    JpPosicion = posicion,
                    JpColorFicha = color,
                    JpEstadoConexion = "CONECTADO",
                    JpEsGanador = false,
                    JpFechaUnion = DateTime.Now
                };

                var jugadorResp = unidadTrabajo.TJugadoresPartida.Insertar(nuevoJugador);
                if (!jugadorResp.blnIndicadorTransaccion)
                    return Respuesta<ResultadoMatchmakingDTO>.Error(jugadorResp.strMensajeRespuesta);

                // ── 6. Cobrar la entrada ─────────────────────────
                usuario.UsuMonedasTotal -= sala.SalCostoEntrada;
                unidadTrabajo.TUsuario.Modificar(usuario);

                // Registramos el movimiento para el historial de monedas
                unidadTrabajo.TTransaccion.Insertar(new Transaccione
                {
                    UsuId = usuId,
                    ParId = partidaElegida.ParId,
                    TranTipo = "ENTRADA_SALA",
                    TranConcepto = $"Entrada a {sala.SalNombre}",
                    TranMonto = -sala.SalCostoEntrada,  // negativo = salida
                    TranSaldoResultante = usuario.UsuMonedasTotal,
                    TranFecha = DateTime.Now
                });

                unidadTrabajo.Completar();

                int totalJugadores = jugadoresActuales.Count + 1;

                // ── 7. ¿Ya se llenó? Arrancamos de una ───────────
                bool iniciada = false;
                if (totalJugadores >= MAX_JUGADORES)
                {
                    var inicioResp = _motor.IniciarPartida(partidaElegida.ParId, unidadTrabajo);
                    iniciada = inicioResp.blnIndicadorTransaccion;
                    _inicioEspera.TryRemove(partidaElegida.ParId, out _);
                }

                // ── 8. Armar la respuesta ────────────────────────
                var resultado = new ResultadoMatchmakingDTO
                {
                    ParId = partidaElegida.ParId,
                    JpId = jugadorResp.ValorRetorno!.JpId,
                    ColorAsignado = color,
                    PosicionEnPartida = posicion,
                    JugadoresActuales = totalJugadores,
                    MonedasRestantes = usuario.UsuMonedasTotal,
                    PartidaIniciada = iniciada,
                    SegundosRestantes = CalcularSegundosRestantes(partidaElegida.ParId),
                    Jugadores = ConstruirListaJugadores(partidaElegida.ParId, unidadTrabajo)
                };

                return Respuesta<ResultadoMatchmakingDTO>.Exito(resultado,
                    iniciada ? "¡Partida completa! Iniciando..." : "Buscando jugadores...");
            }
            catch (Exception ex)
            {
                return Respuesta<ResultadoMatchmakingDTO>.Error(ex.InnerException?.Message ?? ex.Message);
            }
        }
    }

    // ================================================================
    // OBTENER ESTADO DE LA SALA DE ESPERA
    // ================================================================
    public Respuesta<EstadoSalaEsperaDTO> ObtenerEstadoEspera(int parId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            var partidaResp = unidadTrabajo.TPartida.ObtenerEntidad(p => p.ParId == parId);
            if (!partidaResp.blnIndicadorTransaccion)
                return Respuesta<EstadoSalaEsperaDTO>.Validacion("Partida no encontrada.");

            var partida = partidaResp.ValorRetorno!;
            var sala = unidadTrabajo.TSala.ObtenerEntidad(s => s.SalId == partida.SalId).ValorRetorno;
            var jugadores = ConstruirListaJugadores(parId, unidadTrabajo);

            var estado = new EstadoSalaEsperaDTO
            {
                ParId = parId,
                SalId = partida.SalId,
                SalaNombre = sala?.SalNombre ?? "",
                JugadoresActuales = jugadores.Count,
                SegundosRestantes = CalcularSegundosRestantes(parId),
                PartidaIniciada = partida.ParEstado == "EN_JUEGO",
                Jugadores = jugadores
            };

            return Respuesta<EstadoSalaEsperaDTO>.Exito(estado);
        }
        catch (Exception ex)
        {
            return Respuesta<EstadoSalaEsperaDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // VERIFICAR E INICIAR
    // ================================================================
    // El frontend llama a esto cada segundo mientras está en la sala
    // de espera. Cuando vencen los 30 segundos, completa con bots
    // y arranca la partida.
    public Respuesta<bool> VerificarEInicIar(int parId, IUnidadTrabajoEF unidadTrabajo, IMapper mapper)
    {
        try
        {
            var partidaResp = unidadTrabajo.TPartida.ObtenerEntidad(p => p.ParId == parId);
            if (!partidaResp.blnIndicadorTransaccion)
                return Respuesta<bool>.Validacion("Partida no encontrada.");

            var partida = partidaResp.ValorRetorno!;

            // Si ya arrancó, no hacemos nada
            if (partida.ParEstado != "ESPERANDO")
                return Respuesta<bool>.Exito(false, "La partida ya no está en espera.");

            var jugadores = unidadTrabajo.TJugadoresPartida
                .Buscar(j => j.ParId == parId)
                .ValorRetorno?.ToList() ?? new List<JugadoresPartidum>();

            int humanos = jugadores.Count(j => !j.JpEsBot);

            // Sin humanos no tiene sentido iniciar nada
            if (humanos == 0)
                return Respuesta<bool>.Validacion("No hay jugadores humanos en la partida.");

            bool llena = jugadores.Count >= MAX_JUGADORES;
            bool tiempoVencido = CalcularSegundosRestantes(parId) <= 0;

            // Solo iniciamos si está llena o si venció el tiempo
            if (!llena && !tiempoVencido)
                return Respuesta<bool>.Exito(false, "Todavía esperando jugadores.");

            // ── Completar cupos con bots (HU-05) ─────────────────
            // Con 2 humanos → 2 bots. Con 3 → 1 bot. Con 4 → sin bots.
            int cuposLibres = MAX_JUGADORES - jugadores.Count;

            for (int i = 0; i < cuposLibres; i++)
            {
                int posicion = jugadores.Count + i + 1;

                unidadTrabajo.TJugadoresPartida.Insertar(new JugadoresPartidum
                {
                    ParId = parId,
                    UsuId = null,          // los bots no tienen cuenta
                    JpEsBot = true,
                    JpPosicion = posicion,
                    JpColorFicha = ColoresPorPosicion[posicion - 1],
                    JpEstadoConexion = "BOT",
                    JpEsGanador = false,
                    JpFechaUnion = DateTime.Now
                });
            }

            if (cuposLibres > 0)
                unidadTrabajo.Completar();

            // ── Iniciar el juego ─────────────────────────────────
            var inicioResp = _motor.IniciarPartida(parId, unidadTrabajo);

            if (!inicioResp.blnIndicadorTransaccion)
                return Respuesta<bool>.Error(inicioResp.strMensajeRespuesta);

            // Limpiamos el cronómetro, ya no hace falta
            _inicioEspera.TryRemove(parId, out _);

            return Respuesta<bool>.Exito(true,
                cuposLibres > 0
                    ? $"Partida iniciada con {humanos} jugador(es) y {cuposLibres} bot(s)."
                    : "¡Partida iniciada con 4 jugadores!");
        }
        catch (Exception ex)
        {
            return Respuesta<bool>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // ABANDONAR LA ESPERA
    // ================================================================
    // El jugador se arrepiente antes de que arranque. Le devolvemos
    // las monedas completas (no hay penalización si aún no empezó).
    public Respuesta<int> AbandonarEspera(int usuId, int parId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            var partidaResp = unidadTrabajo.TPartida.ObtenerEntidad(p => p.ParId == parId);
            if (!partidaResp.blnIndicadorTransaccion)
                return Respuesta<int>.Validacion("Partida no encontrada.");

            var partida = partidaResp.ValorRetorno!;

            // Si ya arrancó no se puede "abandonar la espera" —
            // eso sería abandono de partida con penalización (HU-19)
            if (partida.ParEstado != "ESPERANDO")
                return Respuesta<int>.Validacion("La partida ya inició, no podés salir sin penalización.");

            var jugadorResp = unidadTrabajo.TJugadoresPartida
                .ObtenerEntidad(j => j.ParId == parId && j.UsuId == usuId);

            if (!jugadorResp.blnIndicadorTransaccion)
                return Respuesta<int>.Validacion("No estás en esta partida.");

            // Lo sacamos de la partida
            unidadTrabajo.TJugadoresPartida.Eliminar(jugadorResp.ValorRetorno!);

            // Le devolvemos las monedas
            var sala = unidadTrabajo.TSala.ObtenerEntidad(s => s.SalId == partida.SalId).ValorRetorno!;
            var usuario = unidadTrabajo.TUsuario.ObtenerEntidad(u => u.UsuId == usuId).ValorRetorno!;

            usuario.UsuMonedasTotal += sala.SalCostoEntrada;
            unidadTrabajo.TUsuario.Modificar(usuario);

            unidadTrabajo.TTransaccion.Insertar(new Transaccione
            {
                UsuId = usuId,
                ParId = parId,
                TranTipo = "DEVOLUCION",
                TranConcepto = $"Devolución por salir de {sala.SalNombre}",
                TranMonto = sala.SalCostoEntrada,   // positivo = entrada
                TranSaldoResultante = usuario.UsuMonedasTotal,
                TranFecha = DateTime.Now
            });

            unidadTrabajo.Completar();

            // Si la partida quedó vacía, la cancelamos
            var quedan = unidadTrabajo.TJugadoresPartida
                .Buscar(j => j.ParId == parId).ValorRetorno?.Count() ?? 0;

            if (quedan == 0)
            {
                partida.ParEstado = "CANCELADA";
                unidadTrabajo.TPartida.Modificar(partida);
                unidadTrabajo.Completar();
                _inicioEspera.TryRemove(parId, out _);
            }

            return Respuesta<int>.Exito(usuario.UsuMonedasTotal,
                $"Saliste de la sala. Se te devolvieron {sala.SalCostoEntrada} monedas.");
        }
        catch (Exception ex)
        {
            return Respuesta<int>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // HELPERS PRIVADOS
    // ================================================================

    // Calcula cuántos segundos faltan para que se acabe la espera
    private int CalcularSegundosRestantes(int parId)
    {
        if (!_inicioEspera.TryGetValue(parId, out DateTime inicio))
            return 0;  // Sin cronómetro registrado = ya venció

        int transcurridos = (int)(DateTime.Now - inicio).TotalSeconds;
        int restantes = SEGUNDOS_ESPERA - transcurridos;

        return restantes > 0 ? restantes : 0;
    }

    // Arma la lista de jugadores para mostrar en la sala de espera
    private List<JugadorEsperaDTO> ConstruirListaJugadores(int parId, IUnidadTrabajoEF unidadTrabajo)
    {
        var jugadores = unidadTrabajo.TJugadoresPartida
            .Buscar(j => j.ParId == parId)
            .ValorRetorno?.OrderBy(j => j.JpPosicion).ToList()
            ?? new List<JugadoresPartidum>();

        var lista = new List<JugadorEsperaDTO>();

        foreach (var jugador in jugadores)
        {
            string nombre = "Bot";
            int avatar = 0;

            if (!jugador.JpEsBot && jugador.UsuId.HasValue)
            {
                var usuario = unidadTrabajo.TUsuario
                    .ObtenerEntidad(u => u.UsuId == jugador.UsuId.Value).ValorRetorno;

                nombre = usuario?.UsuNombre ?? "Jugador";
                avatar = usuario?.UsuAvatar ?? 1;
            }

            lista.Add(new JugadorEsperaDTO
            {
                JpId = jugador.JpId,
                UsuId = jugador.UsuId,
                Nombre = nombre,
                Color = jugador.JpColorFicha,
                Posicion = jugador.JpPosicion,
                EsBot = jugador.JpEsBot,
                Avatar = avatar
            });
        }

        return lista;
    }
}
