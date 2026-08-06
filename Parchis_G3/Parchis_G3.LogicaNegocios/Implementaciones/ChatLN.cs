using System.Collections.Concurrent;
using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class ChatLN : IChatLN
{
    // Segundos que debe esperar un jugador entre mensajes (HU-14)
    private const int COOLDOWN_SEGUNDOS = 5;
    private const int MAX_LONGITUD_TEXTO = 200;

    // jpId -> momento del último mensaje enviado
    private readonly ConcurrentDictionary<int, DateTime> _ultimoMensaje = new();

    // Los 4 mensajes rápidos que definiste en la HU-14
    private static readonly List<MensajePredefinidoDTO> _predefinidos = new()
    {
        new MensajePredefinidoDTO { Id = 1, Texto = "¡Buena jugada!"   },
        new MensajePredefinidoDTO { Id = 2, Texto = "¡Eso te pasa!"    },
        new MensajePredefinidoDTO { Id = 3, Texto = "¡Nadie me para!"  },
        new MensajePredefinidoDTO { Id = 4, Texto = "¡Gg!"             }
    };

    public Respuesta<List<MensajePredefinidoDTO>> ObtenerMensajesPredefinidos()
    {
        return Respuesta<List<MensajePredefinidoDTO>>.Exito(_predefinidos, "Mensajes obtenidos.");
    }

    // ================================================================
    // ENVIAR MENSAJE
    // ================================================================
    public Respuesta<MensajeChatDTO> EnviarMensaje(int parId, int jpId, string contenido, bool esPredefinido, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            // ── Validación de contenido ──────────────────────────
            if (string.IsNullOrWhiteSpace(contenido))
                return Respuesta<MensajeChatDTO>.Validacion("El mensaje no puede estar vacío.");

            contenido = contenido.Trim();

            if (contenido.Length > MAX_LONGITUD_TEXTO)
                return Respuesta<MensajeChatDTO>.Validacion(
                    $"El mensaje no puede superar los {MAX_LONGITUD_TEXTO} caracteres.");

            // ── Anti-spam: cooldown de 5 segundos ────────────────
            if (_ultimoMensaje.TryGetValue(jpId, out DateTime ultimo))
            {
                double segundosTranscurridos = (DateTime.Now - ultimo).TotalSeconds;

                if (segundosTranscurridos < COOLDOWN_SEGUNDOS)
                {
                    int faltan = (int)Math.Ceiling(COOLDOWN_SEGUNDOS - segundosTranscurridos);
                    return Respuesta<MensajeChatDTO>.Validacion(
                        $"Esperá {faltan} segundo(s) antes de enviar otro mensaje.");
                }
            }

            // ── Validar que el jugador esté en esa partida ───────
            // Sin esto, alguien podría mandar mensajes a partidas ajenas
            var jugadorResp = unidadTrabajo.TJugadoresPartida
                .ObtenerEntidad(j => j.JpId == jpId && j.ParId == parId);

            if (!jugadorResp.blnIndicadorTransaccion)
                return Respuesta<MensajeChatDTO>.Validacion("No pertenecés a esta partida.");

            var jugador = jugadorResp.ValorRetorno!;

            // ── Guardar en BD ────────────────────────────────────
            var mensaje = new MensajesChat
            {
                ParId = parId,
                JpId = jpId,
                McContenido = contenido,
                McEsPredefinido = esPredefinido,
                McFecha = DateTime.Now
            };

            var insertResp = unidadTrabajo.TMensajesChat.Insertar(mensaje);
            if (!insertResp.blnIndicadorTransaccion)
                return Respuesta<MensajeChatDTO>.Error(insertResp.strMensajeRespuesta);

            unidadTrabajo.Completar();

            // Registramos el momento para el cooldown
            _ultimoMensaje[jpId] = DateTime.Now;

            // ── Armar el DTO con el nombre del jugador ───────────
            string nombre = "Bot";
            if (!jugador.JpEsBot && jugador.UsuId.HasValue)
            {
                var usuario = unidadTrabajo.TUsuario
                    .ObtenerEntidad(u => u.UsuId == jugador.UsuId.Value).ValorRetorno;
                nombre = usuario?.UsuNombre ?? "Jugador";
            }

            var dto = new MensajeChatDTO
            {
                McId = insertResp.ValorRetorno!.McId,
                JpId = jpId,
                NombreJugador = nombre,
                Color = jugador.JpColorFicha,
                Contenido = contenido,
                EsPredefinido = esPredefinido,
                Fecha = mensaje.McFecha
            };

            return Respuesta<MensajeChatDTO>.Exito(dto, "Mensaje enviado.");
        }
        catch (Exception ex)
        {
            return Respuesta<MensajeChatDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // OBTENER HISTORIAL
    // ================================================================
    public Respuesta<List<MensajeChatDTO>> ObtenerHistorial(int parId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            var mensajes = unidadTrabajo.TMensajesChat
                .Buscar(m => m.ParId == parId)
                .ValorRetorno?.OrderBy(m => m.McFecha).ToList()
                ?? new List<MensajesChat>();

            var jugadores = unidadTrabajo.TJugadoresPartida
                .Buscar(j => j.ParId == parId)
                .ValorRetorno?.ToList() ?? new List<JugadoresPartidum>();

            var lista = new List<MensajeChatDTO>();

            foreach (var msg in mensajes)
            {
                var jugador = jugadores.FirstOrDefault(j => j.JpId == msg.JpId);

                string nombre = "Bot";
                if (jugador != null && !jugador.JpEsBot && jugador.UsuId.HasValue)
                {
                    var usuario = unidadTrabajo.TUsuario
                        .ObtenerEntidad(u => u.UsuId == jugador.UsuId.Value).ValorRetorno;
                    nombre = usuario?.UsuNombre ?? "Jugador";
                }

                lista.Add(new MensajeChatDTO
                {
                    McId = msg.McId,
                    JpId = msg.JpId,
                    NombreJugador = nombre,
                    Color = jugador?.JpColorFicha ?? "",
                    Contenido = msg.McContenido,
                    EsPredefinido = msg.McEsPredefinido,
                    Fecha = msg.McFecha
                });
            }

            return Respuesta<List<MensajeChatDTO>>.Exito(lista, "Historial obtenido.");
        }
        catch (Exception ex)
        {
            return Respuesta<List<MensajeChatDTO>>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }
}
