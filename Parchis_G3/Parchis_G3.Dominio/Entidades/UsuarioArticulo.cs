using System;
using System.Collections.Generic;

namespace Parchis_G3.Dominio.Entidades;

public partial class UsuarioArticulo
{
    public int UartId { get; set; }

    public int UsuId { get; set; }

    public int ArtId { get; set; }

    public DateTime UartFechaCompra { get; set; }

    public virtual Articulo Art { get; set; } = null!;

    public virtual Usuario Usu { get; set; } = null!;
}
