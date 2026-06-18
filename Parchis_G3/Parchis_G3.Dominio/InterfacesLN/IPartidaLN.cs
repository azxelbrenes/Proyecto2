using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
   public interface IPartidaLN
    {
        Respuesta<TPartida> Insertar(TPartida Partida);
        Respuesta<TPartida> Modificar(TPartida Partida);
        Respuesta<bool> Eliminar(TPartida Partida);
        Respuesta<IEnumerable<TPartida>> Obtener(TPartida Partida);
        Respuesta<TPartida> Buscar(TPartida Partida);
        Respuesta<IEnumerable<TPartida>> Listar();
    }
}
