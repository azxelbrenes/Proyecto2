using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
  public interface ITiposArticuloLN
    {
        Respuesta<TTiposArticulo> Insertar(TTiposArticulo TiposArticulo);
        Respuesta<TTiposArticulo> Modificar(TTiposArticulo TiposArticulo);
        Respuesta<bool> Eliminar(TTiposArticulo TiposArticulo);
        Respuesta<IEnumerable<TTiposArticulo>> Obtener(TTiposArticulo TiposArticulo);
        Respuesta<TTiposArticulo> Buscar(TTiposArticulo TiposArticulo);
        Respuesta<IEnumerable<TTiposArticulo>> Listar();
    }
}
