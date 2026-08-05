namespace Parchis_G3.Dominio.DTO;

// Representa el estado COMPLETO de una partida en un momento dado.
// Esto es lo que se manda a los 4 jugadores cada vez que algo cambia.
public class EstadoPartidaDTO
{
    public int ParId { get; set; }
    public string ParEstado { get; set; } = string.Empty;
    public int TurnoActualJpId { get; set; }
    public List<FichaDTO> Fichas { get; set; } = new();
    public List<JugadorPartidaDTO> Jugadores { get; set; } = new();
}

// Representa una ficha individual en el tablero
public class FichaDTO
{
    public int JpId { get; set; }
    public int NumeroFicha { get; set; }
    public int Posicion { get; set; }      // 0=casa, 1-68=tablero, 69=coronada
    public string Estado { get; set; } = string.Empty;  // EN_CASA / EN_JUEGO / CORONADA
    public string Color { get; set; } = string.Empty;
}

// Representa a un jugador (humano o bot) dentro de la partida
public class JugadorPartidaDTO
{
    public int JpId { get; set; }
    public int? UsuId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool EsBot { get; set; }
    public string Color { get; set; } = string.Empty;
    public bool EsGanador { get; set; }
}

// Resultado de un turno completo: qué pasó al tirar el dado y mover
public class ResultadoTurnoDTO
{
    public int ValorDado { get; set; }
    public bool TurnoExtra { get; set; }
    public bool HuboCaptura { get; set; }
    public bool FichaCoronada { get; set; }
    public bool PartidaFinalizada { get; set; }
    public int? GanadorJpId { get; set; }
    public int SiguienteTurnoJpId { get; set; }
    public string? Mensaje { get; set; }          // Para errores o avisos
    public EstadoPartidaDTO? Estado { get; set; }  // Estado actualizado del tablero
}
