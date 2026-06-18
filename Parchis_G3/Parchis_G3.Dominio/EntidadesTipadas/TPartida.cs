using System;
using System.Collections.Generic;

namespace Parchis_G3.Dominio.EntidadesTipadas;

public partial class TPartida
{
    public int ParId { get; set; }

    public int SalId { get; set; }

    public string ParEstado { get; set; } = null!;

    public DateTime? ParFechaInicio { get; set; }

    public DateTime? ParFechaFin { get; set; }

    public int ParPremioTotal { get; set; }

 
}
