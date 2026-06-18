using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
    public interface IUsuarioArticuloLN
    {
        Respuesta<TUsuarioArticulo> Insertar(TUsuarioArticulo UsuarioArticulo);
        Respuesta<TUsuarioArticulo> Modificar(TUsuarioArticulo UsuarioArticulo);
        Respuesta<bool> Eliminar(TUsuarioArticulo UsuarioArticulo);
        Respuesta<IEnumerable<TUsuarioArticulo>> Obtener(TUsuarioArticulo UsuarioArticulo);
        Respuesta<TUsuarioArticulo> Buscar(TUsuarioArticulo UsuarioArticulo);
        Respuesta<IEnumerable<TUsuarioArticulo>> Listar();
    }
}
