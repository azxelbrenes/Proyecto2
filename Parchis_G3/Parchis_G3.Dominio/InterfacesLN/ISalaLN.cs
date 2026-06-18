using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
   public interface ISalaLN
    {
        Respuesta<TSala> Insertar(TSala Sala);
        Respuesta<TSala> Modificar(TSala Sala);
        Respuesta<bool> Eliminar(TSala Sala);
        Respuesta<IEnumerable<TSala>> Obtener(TSala Sala);
        Respuesta<TSala> Buscar(TSala Sala);
        Respuesta<IEnumerable<TSala>> Listar();
    }
}
