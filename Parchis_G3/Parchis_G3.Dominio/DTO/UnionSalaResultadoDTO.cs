using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.DTO
{
    public class UnionSalaResultadoDTO
    {
        public int SalId { get; set; }
        public string SalNombre { get; set; } = string.Empty;
        public int CostoEntrada { get; set; }
        public int MonedasRestantes { get; set; }
    }
}
