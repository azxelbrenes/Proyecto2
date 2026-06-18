using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
    public interface IUsuarioLN
    {
        Respuesta<TUsuario> Insertar(TUsuario Usuario);
        Respuesta<TUsuario> Modificar(TUsuario Usuario);
        Respuesta<bool> Eliminar(TUsuario Usuario);
        Respuesta<IEnumerable<TUsuario>> Obtener(TUsuario Usuario);
        Respuesta<TUsuario> Buscar(TUsuario Usuario);
        Respuesta<IEnumerable<TUsuario>> Listar();
    }
}
