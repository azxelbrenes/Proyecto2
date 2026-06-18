using System;
using System.Collections.Generic;

namespace Parchis_G3.Dominio.EntidadesTipadas;

public partial class TTiposArticulo
{
    public int TipId { get; set; }

    public string TipNombre { get; set; } = null!;

    public string? TipDescripcion { get; set; }

    
}
