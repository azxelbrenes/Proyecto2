using System;
using System.Collections.Generic;

namespace Parchis_G3.Dominio.EntidadesTipadas;

public partial class TSala
{
    public int SalId { get; set; }

    public string SalNombre { get; set; } = null!;

    public int SalCostoEntrada { get; set; }

    public int SalPremioBase { get; set; }

    public decimal SalComision { get; set; }

    public string SalEstado { get; set; } = null!;

   
}
