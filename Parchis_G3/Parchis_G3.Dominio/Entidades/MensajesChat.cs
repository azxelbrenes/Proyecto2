using System;
using System.Collections.Generic;

namespace Parchis_G3.Dominio.Entidades;

public partial class MensajesChat
{
    public int McId { get; set; }

    public int ParId { get; set; }

    public int JpId { get; set; }

    public string McContenido { get; set; } = null!;

    public bool McEsPredefinido { get; set; }

    public DateTime McFecha { get; set; }

    public virtual JugadoresPartidum Jp { get; set; } = null!;

    public virtual Partida Par { get; set; } = null!;
}
