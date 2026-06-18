using System;
using System.Collections.Generic;

namespace Parchis_G3.Dominio.EntidadesTipadas;

public partial class THistorialPartida
{
    public int HpId { get; set; }

    public int UsuId { get; set; }

    public int ParId { get; set; }

    public int SalId { get; set; }

    public string HpResultado { get; set; } = null!;

    public int HpMonedasGanadas { get; set; }

    public DateTime HpFecha { get; set; }

    public int? HpDuracionMinutos { get; set; }

   
}
