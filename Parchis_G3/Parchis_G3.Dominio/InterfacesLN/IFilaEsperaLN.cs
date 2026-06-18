using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
   public interface IFilaEsperaLN
    {
        Respuesta<TFilaEspera> Insertar(TFilaEspera FilaEspera);
        Respuesta<TFilaEspera> Modificar(TFilaEspera FilaEspera);
        Respuesta<bool> Eliminar(TFilaEspera FilaEspera);
        Respuesta<IEnumerable<TFilaEspera>> Obtener(TFilaEspera FilaEspera);
        Respuesta<TFilaEspera> Buscar(TFilaEspera FilaEspera);
        Respuesta<IEnumerable<TFilaEspera>> Listar();
    }
}
