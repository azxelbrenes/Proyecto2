using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
    public interface IHistorialPartidaLN
    {
        Respuesta<THistorialPartida> Insertar(THistorialPartida HistorialPartida);
        Respuesta<THistorialPartida> Modificar(THistorialPartida HistorialPartida);
        Respuesta<bool> Eliminar(THistorialPartida HistorialPartida);
        Respuesta<IEnumerable<THistorialPartida>> Obtener(THistorialPartida HistorialPartida);
        Respuesta<THistorialPartida> Buscar(THistorialPartida HistorialPartida);
        Respuesta<IEnumerable<THistorialPartida>> Listar();
    }
}
