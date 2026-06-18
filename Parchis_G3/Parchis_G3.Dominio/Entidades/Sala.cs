using System;
using System.Collections.Generic;

namespace Parchis_G3.AccesoDatos.Model;

public partial class Sala
{
    public int SalId { get; set; }

    public string SalNombre { get; set; } = null!;

    public int SalCostoEntrada { get; set; }

    public int SalPremioBase { get; set; }

    public decimal SalComision { get; set; }

    public string SalEstado { get; set; } = null!;

    public virtual ICollection<FilaEspera> FilaEsperas { get; set; } = new List<FilaEspera>();

    public virtual ICollection<HistorialPartida> HistorialPartida { get; set; } = new List<HistorialPartida>();

    public virtual ICollection<Partida> Partida { get; set; } = new List<Partida>();
}
