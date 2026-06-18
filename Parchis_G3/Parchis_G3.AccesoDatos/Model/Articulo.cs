using System;
using System.Collections.Generic;

namespace Parchis_G3.AccesoDatos.Model;

public partial class Articulo
{
    public int ArtId { get; set; }

    public int TipId { get; set; }

    public string ArtNombre { get; set; } = null!;

    public string? ArtDescripcion { get; set; }

    public int ArtPrecio { get; set; }

    public string? ArtImagenUrl { get; set; }

    public bool ArtEsPredeterminado { get; set; }

    public string ArtEstado { get; set; } = null!;

    public virtual ICollection<EquipamientoActivo> EquipamientoActivos { get; set; } = new List<EquipamientoActivo>();

    public virtual TiposArticulo Tip { get; set; } = null!;

    public virtual ICollection<UsuarioArticulo> UsuarioArticulos { get; set; } = new List<UsuarioArticulo>();
}
