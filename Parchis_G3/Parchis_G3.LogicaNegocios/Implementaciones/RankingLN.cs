using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class RankingLN : IRankingLN
{
    public Respuesta<RankingDTO> ObtenerRanking(int usuId, int top, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            // Limitamos el top entre 10 y 100 para que nadie pida
            // 50,000 registros de un golpe y tumbe el servidor
            if (top <= 0) top = 50;
            if (top > 100) top = 100;

            // ── Traemos usuarios activos ordenados por monedas ganadas ──
            // La BD tiene el índice IDX_Usuarios_Ranking sobre este
            // campo en orden descendente, así que esta consulta es rápida
            var usuarios = unidadTrabajo.TUsuario
                .Buscar(u => u.UsuEstado == "A")
                .ValorRetorno?
                .OrderByDescending(u => u.UsuMonedasGanadasPartida)
                .ThenBy(u => u.UsuId)   // desempate estable por ID
                .ToList() ?? new List<Usuario>();

            var dto = new RankingDTO
            {
                TotalJugadores = usuarios.Count
            };

            // ── Armamos el top ───────────────────────────────────
            int posicion = 0;
            foreach (var usuario in usuarios.Take(top))
            {
                posicion++;

                dto.Top.Add(new RankingJugadorDTO
                {
                    Posicion = posicion,
                    UsuId = usuario.UsuId,
                    Nombre = usuario.UsuNombre,
                    Avatar = usuario.UsuAvatar,
                    MonedasGanadasPartida = usuario.UsuMonedasGanadasPartida,
                    PartidasGanadas = ContarVictorias(usuario.UsuId, unidadTrabajo),
                    EsElUsuarioActual = usuario.UsuId == usuId
                });
            }

            // ── Posición del usuario actual ──────────────────────
            // Si ya está en el top, reutilizamos ese objeto.
            // Si no, calculamos su posición real y la mandamos aparte
            // para mostrarla separada abajo de la lista.
            var enElTop = dto.Top.FirstOrDefault(j => j.UsuId == usuId);

            if (enElTop != null)
            {
                dto.MiPosicion = enElTop;
            }
            else
            {
                // Buscamos su índice en la lista completa ordenada
                int indice = usuarios.FindIndex(u => u.UsuId == usuId);

                if (indice >= 0)
                {
                    var yo = usuarios[indice];

                    dto.MiPosicion = new RankingJugadorDTO
                    {
                        Posicion = indice + 1,
                        UsuId = yo.UsuId,
                        Nombre = yo.UsuNombre,
                        Avatar = yo.UsuAvatar,
                        MonedasGanadasPartida = yo.UsuMonedasGanadasPartida,
                        PartidasGanadas = ContarVictorias(yo.UsuId, unidadTrabajo),
                        EsElUsuarioActual = true
                    };
                }
            }

            return Respuesta<RankingDTO>.Exito(dto, "Ranking obtenido correctamente.");
        }
        catch (Exception ex)
        {
            return Respuesta<RankingDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ── Cuenta las victorias de un jugador ───────────────────────
    // Se saca del historial de partidas, no de un contador en
    // Usuarios — así el dato siempre coincide con el historial real
    private int ContarVictorias(int usuId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            return unidadTrabajo.THistorialPartida
                .Buscar(h => h.UsuId == usuId && h.HpResultado == "VICTORIA")
                .ValorRetorno?.Count() ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}
