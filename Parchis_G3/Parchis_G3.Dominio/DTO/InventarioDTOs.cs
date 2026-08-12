namespace Parchis_G3.Dominio.DTO;



// Un artículo que el usuario ya desbloqueó
public class ArticuloInventarioDTO
{
    public int ArtId { get; set; }
    public int TipId { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public string ArtNombre { get; set; } = string.Empty;
    public string? ArtDescripcion { get; set; }
    public int ArtPrecio { get; set; }
    public string? ArtImagenUrl { get; set; }
    public bool EsPredeterminado { get; set; }
    public bool EstaEquipado { get; set; }  // ¿lo tiene puesto ahora?
    public DateTime FechaCompra { get; set; }
}

// Qué tiene equipado el usuario en cada categoría.
// Puede ser null si nunca equipó nada de ese tipo.
public class EquipamientoDTO
{
    public ArticuloInventarioDTO? Ficha { get; set; }
    public ArticuloInventarioDTO? Tablero { get; set; }
    public ArticuloInventarioDTO? Dado { get; set; }
}

// Body para equipar un artículo
public class EquiparRequest
{
    public int ArtId { get; set; }
}


// RANKING

// Una fila del ranking global
public class RankingJugadorDTO
{
    public int Posicion { get; set; }
    public int UsuId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Avatar { get; set; }
    public int MonedasGanadasPartida { get; set; }
    public int PartidasGanadas { get; set; }
    public bool EsElUsuarioActual { get; set; }  // para resaltarlo en la lista
}

// Respuesta completa del ranking
public class RankingDTO
{
    public List<RankingJugadorDTO> Top { get; set; } = new();

    // La posición del usuario actual. Si está dentro del top, es el
    // mismo objeto. Si está fuera (ej. puesto 200), viene aparte
    // para poder mostrarlo abajo separado.
    public RankingJugadorDTO? MiPosicion { get; set; }

    public int TotalJugadores { get; set; }
}

// RECOMPENSA DIARIA

// Estado actual de la racha del usuario
public class EstadoRecompensaDTO
{
    public bool PuedeReclamar { get; set; }
    public int RachaActual { get; set; }  // 0 a 5
    public int MonedasHoy { get; set; }  // lo que gana si reclama ahora
    public int MonedasSiguienteDia { get; set; }  // lo que ganaría mañana
    public DateTime? UltimaReclamacion { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

// Resultado de reclamar la recompensa
public class ResultadoRecompensaDTO
{
    public bool Exitoso { get; set; }
    public int MonedasOtorgadas { get; set; }
    public int SaldoNuevo { get; set; }
    public int RachaNueva { get; set; }
    public bool RachaReiniciada { get; set; }  // ¿perdió la racha por faltar?
    public string Mensaje { get; set; } = string.Empty;
}
