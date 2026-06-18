using System;
using System.Collections.Generic;

namespace Parchis_G3.Dominio.EntidadesTipadas;

public partial class TEstadoFicha
{
    public int EfId { get; set; }

    public int ParId { get; set; }

    public int JpId { get; set; }

    public int EfNumeroFicha { get; set; }

    public int EfPosicion { get; set; }

    public string EfEstadoFicha { get; set; } = null!;

    public DateTime EfUltimaActualizacion { get; set; }

    
}
