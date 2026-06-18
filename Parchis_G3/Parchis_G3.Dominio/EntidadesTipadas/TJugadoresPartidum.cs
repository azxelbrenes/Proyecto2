using System;
using System.Collections.Generic;

namespace Parchis_G3.Dominio.EntidadesTipadas;

public partial class TJugadoresPartidum
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

   
}
