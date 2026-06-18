using System;
using System.Collections.Generic;

namespace Parchis_G3.Dominio.EntidadesTipadas;

public partial class TTransaccione
{
    public int TranId { get; set; }

    public int UsuId { get; set; }

    public int? ParId { get; set; }

    public string TranTipo { get; set; } = null!;

    public string TranConcepto { get; set; } = null!;

    public int TranMonto { get; set; }

    public int TranSaldoResultante { get; set; }

    public string? TranReferenciaExt { get; set; }

    public DateTime TranFecha { get; set; }

   
}
