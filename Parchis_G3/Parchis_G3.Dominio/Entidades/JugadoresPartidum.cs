using System;
using System.Collections.Generic;

namespace Parchis_G3.AccesoDatos.Model;

public partial class JugadoresPartidum
{
    public int JpId { get; set; }

    public int ParId { get; set; }

    public int? UsuId { get; set; }

    public bool JpEsBot { get; set; }

    public int JpPosicion { get; set; }

    public string JpColorFicha { get; set; } = null!;

    public string JpEstadoConexion { get; set; } = null!;

    public DateTime? JpFechaDesconexion { get; set; }

    public bool JpEsGanador { get; set; }

    public DateTime JpFechaUnion { get; set; }

    public virtual ICollection<EstadoFicha> EstadoFichas { get; set; } = new List<EstadoFicha>();

    public virtual ICollection<MensajesChat> MensajesChats { get; set; } = new List<MensajesChat>();

    public virtual Partida Par { get; set; } = null!;

    public virtual ICollection<TurnosPartidum> TurnosPartida { get; set; } = new List<TurnosPartidum>();

    public virtual Usuario? Usu { get; set; }
}
