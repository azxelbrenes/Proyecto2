using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.EntidadesTipadas
{
    public class TArticulo
    {
        public int ArtId { get; set; }

        public int TipId { get; set; }

        public string ArtNombre { get; set; } = null!;

        public string? ArtDescripcion { get; set; }

        public int ArtPrecio { get; set; }

        public string? ArtImagenUrl { get; set; }

        public bool ArtEsPredeterminado { get; set; }

        public string ArtEstado { get; set; } = null!;
    }
}
