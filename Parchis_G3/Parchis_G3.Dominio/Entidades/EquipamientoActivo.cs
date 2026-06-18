using System;
using System.Collections.Generic;

namespace Parchis_G3.AccesoDatos.Model;

public partial class EquipamientoActivo
{
    public int EquId { get; set; }

    public int UsuId { get; set; }

    public int TipId { get; set; }

    public int ArtId { get; set; }

    public virtual Articulo Art { get; set; } = null!;

    public virtual TiposArticulo Tip { get; set; } = null!;

    public virtual Usuario Usu { get; set; } = null!;
}
