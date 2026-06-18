using System;
using System.Collections.Generic;

namespace Parchis_G3.AccesoDatos.Model;

public partial class TiposArticulo
{
    public int TipId { get; set; }

    public string TipNombre { get; set; } = null!;

    public string? TipDescripcion { get; set; }

    public virtual ICollection<Articulo> Articulos { get; set; } = new List<Articulo>();

    public virtual ICollection<EquipamientoActivo> EquipamientoActivos { get; set; } = new List<EquipamientoActivo>();
}
