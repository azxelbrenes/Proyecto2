
using System;
using System.Collections.Generic;

namespace Parchis_G3.Dominio.Entidades;

public partial class SegLog
{
    public int LogId { get; set; }

    public int? UsuId { get; set; }

    public string? LogCorreo { get; set; }

    public string LogEvento { get; set; } = null!;

    public string? LogIp { get; set; }

    public string? LogDetalle { get; set; }

    public DateTime LogFecha { get; set; }

    public virtual Usuario? Usu { get; set; }
}