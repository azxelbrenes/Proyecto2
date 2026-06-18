using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
    public interface IJugadoresPartidaLN
    {
        Respuesta<TJugadoresPartidum> Insertar(TJugadoresPartidum JugadoresPartida);
        Respuesta<TJugadoresPartidum> Modificar(TJugadoresPartidum JugadoresPartida);
        Respuesta<bool> Eliminar(TJugadoresPartidum JugadoresPartida);
        Respuesta<IEnumerable<TJugadoresPartidum>> Obtener(TJugadoresPartidum JugadoresPartida);
        Respuesta<TJugadoresPartidum> Buscar(TJugadoresPartidum JugadoresPartida);
        Respuesta<IEnumerable<TJugadoresPartidum>> Listar();
    }
}
