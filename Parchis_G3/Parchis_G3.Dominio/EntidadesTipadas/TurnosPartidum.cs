using System;
using System.Collections.Generic;

namespace Parchis_G3.Dominio.EntidadesTipadas;

public partial class TurnosPartidum
{
    public int TurId { get; set; }

    public int ParId { get; set; }

    public int JpId { get; set; }

    public int TurNumeroTurno { get; set; }

    public int TurResultadoDado { get; set; }

    public int? TurFichaMovida { get; set; }

    public int? TurPosicionAnterior { get; set; }

    public int? TurPosicionNueva { get; set; }

    public bool TurFueAutomatico { get; set; }

    public bool TurHuboCaptura { get; set; }

    public DateTime TurFecha { get; set; }

   
}
