namespace Parchis_G3.Dominio.DTO;

/// <summary>
/// RF-05: un logro del jugador.
///
/// Los logros no tienen tabla propia: se derivan del historial de
/// partidas y de las transacciones, que ya se guardan. Evita una
/// migración y garantiza que nunca queden desincronizados con los
/// datos reales — si el historial dice 5 victorias, el logro de
/// "5 victorias" está desbloqueado, sin posibilidad de discrepancia.
/// </summary>
public class LogroDTO
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Icono { get; set; } = string.Empty;

    public bool Desbloqueado { get; set; }

    // Progreso hacia el logro, para mostrar barras del tipo "3/10"
    public int ProgresoActual { get; set; }
    public int ProgresoMeta { get; set; }

    // Monedas que otorga al desbloquearse
    public int Recompensa { get; set; }

    // Si ya se reclamó la recompensa de este logro
    public bool Reclamado { get; set; }

    public int PorcentajeProgreso =>
        ProgresoMeta <= 0 ? 0 : Math.Min(100, (int)((double)ProgresoActual / ProgresoMeta * 100));
}

public class ResumenLogrosDTO
{
    public List<LogroDTO> Logros { get; set; } = new();

    public int TotalLogros { get; set; }
    public int Desbloqueados { get; set; }

    // Monedas que el jugador puede reclamar ahora mismo
    public int RecompensaPendiente { get; set; }
}

public class ResultadoReclamoLogrosDTO
{
    public int MonedasGanadas { get; set; }
    public int SaldoNuevo { get; set; }
    public List<string> LogrosReclamados { get; set; } = new();
    public string Mensaje { get; set; } = string.Empty;
}