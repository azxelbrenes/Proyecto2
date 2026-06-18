using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
    public interface IArticuloLN
    {
        Respuesta<TArticulo> Insertar(TArticulo Articulo);
        Respuesta<TArticulo> Modificar(TArticulo Articulo);
        Respuesta<bool> Eliminar(TArticulo Articulo);
        Respuesta<IEnumerable<TArticulo>> Obtener(TArticulo Articulo);
        Respuesta<TArticulo> Buscar(TArticulo Articulo);
        Respuesta<IEnumerable<TArticulo>> Listar();
    }
}
