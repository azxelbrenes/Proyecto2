using System;
using System.Collections.Generic;

namespace Parchis_G3.Dominio.EntidadesTipadas;

public partial class FilaEspera
{
    public int FeId { get; set; }

    public int UsuId { get; set; }

    public int SalId { get; set; }

    public int FePosicion { get; set; }

    public string FeEstado { get; set; } = null!;

    public DateTime FeFechaIngreso { get; set; }

    public DateTime? FeFechaSalida { get; set; }

   
}
