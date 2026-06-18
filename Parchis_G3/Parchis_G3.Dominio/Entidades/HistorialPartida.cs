using System;
using System.Collections.Generic;

namespace Parchis_G3.AccesoDatos.Model;

public partial class HistorialPartida
{
    public int HpId { get; set; }

    public int UsuId { get; set; }

    public int ParId { get; set; }

    public int SalId { get; set; }

    public string HpResultado { get; set; } = null!;

    public int HpMonedasGanadas { get; set; }

    public DateTime HpFecha { get; set; }

    public int? HpDuracionMinutos { get; set; }

    public virtual Partida Par { get; set; } = null!;

    public virtual Sala Sal { get; set; } = null!;

    public virtual Usuario Usu { get; set; } = null!;
}
