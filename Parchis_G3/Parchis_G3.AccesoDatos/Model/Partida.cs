using System;
using System.Collections.Generic;

namespace Parchis_G3.AccesoDatos.Model;

public partial class Partida
{
    public int ParId { get; set; }

    public int SalId { get; set; }

    public string ParEstado { get; set; } = null!;

    public DateTime? ParFechaInicio { get; set; }

    public DateTime? ParFechaFin { get; set; }

    public int ParPremioTotal { get; set; }

    public virtual ICollection<EstadoFicha> EstadoFichas { get; set; } = new List<EstadoFicha>();

    public virtual ICollection<HistorialPartida> HistorialPartida { get; set; } = new List<HistorialPartida>();

    public virtual ICollection<JugadoresPartidum> JugadoresPartida { get; set; } = new List<JugadoresPartidum>();

    public virtual ICollection<MensajesChat> MensajesChats { get; set; } = new List<MensajesChat>();

    public virtual Sala Sal { get; set; } = null!;

    public virtual ICollection<Transaccione> Transacciones { get; set; } = new List<Transaccione>();

    public virtual ICollection<TurnosPartidum> TurnosPartida { get; set; } = new List<TurnosPartidum>();
}
