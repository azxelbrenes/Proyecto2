using System;
using System.Collections.Generic;

namespace Parchis_G3.AccesoDatos.Model;

public partial class Usuario
{
    public int UsuId { get; set; }

    public string UsuNombre { get; set; } = null!;

    public string UsuCorreo { get; set; } = null!;

    public string UsuPasswordHash { get; set; } = null!;

    public int UsuAvatar { get; set; }

    public string? UsuTokenFcm { get; set; }

    public int UsuMonedasTotal { get; set; }

    public int UsuMonedasGanadasPartida { get; set; }

    public int UsuRachaDias { get; set; }

    public DateOnly? UsuUltimaConexion { get; set; }

    public bool UsuBloqueado { get; set; }

    public DateTime? UsuFechaDesbloqueo { get; set; }

    public int UsuAbandonosConsecutivos { get; set; }

    public bool UsuTutorialCompletado { get; set; }

    public bool UsuNotificacionesActivas { get; set; }

    public bool UsuSonidosActivos { get; set; }

    public bool UsuMusicaActiva { get; set; }

    public string UsuEstado { get; set; } = null!;

    public DateTime UsuFechaCreacion { get; set; }

    public virtual ICollection<EquipamientoActivo> EquipamientoActivos { get; set; } = new List<EquipamientoActivo>();

    public virtual ICollection<FilaEspera> FilaEsperas { get; set; } = new List<FilaEspera>();

    public virtual ICollection<HistorialPartida> HistorialPartida { get; set; } = new List<HistorialPartida>();

    public virtual ICollection<JugadoresPartidum> JugadoresPartida { get; set; } = new List<JugadoresPartidum>();

    public virtual ICollection<SesionesActiva> SesionesActivas { get; set; } = new List<SesionesActiva>();

    public virtual ICollection<Transaccione> Transacciones { get; set; } = new List<Transaccione>();

    public virtual ICollection<UsuarioArticulo> UsuarioArticulos { get; set; } = new List<UsuarioArticulo>();
}
