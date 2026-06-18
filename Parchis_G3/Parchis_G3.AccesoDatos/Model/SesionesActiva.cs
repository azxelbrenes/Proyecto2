using System;
using System.Collections.Generic;

namespace Parchis_G3.AccesoDatos.Model;

public partial class SesionesActiva
{
    public int SesId { get; set; }

    public int UsuId { get; set; }

    public string SesTokenHash { get; set; } = null!;

    public DateTime SesFechaCreacion { get; set; }

    public DateTime SesFechaExpiracion { get; set; }

    public DateTime SesUltimaActividad { get; set; }

    public string? SesDispositivoInfo { get; set; }

    public bool SesActiva { get; set; }

    public virtual Usuario Usu { get; set; } = null!;
}
