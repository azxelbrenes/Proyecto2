using System;
using System.Collections.Generic;

namespace Parchis_G3.AccesoDatos.Model;

public partial class EstadoFicha
{
    public int EfId { get; set; }

    public int ParId { get; set; }

    public int JpId { get; set; }

    public int EfNumeroFicha { get; set; }

    public int EfPosicion { get; set; }

    public string EfEstadoFicha { get; set; } = null!;

    public DateTime EfUltimaActualizacion { get; set; }

    public virtual JugadoresPartidum Jp { get; set; } = null!;

    public virtual Partida Par { get; set; } = null!;
}
