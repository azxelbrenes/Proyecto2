using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
    public interface ITurnosPartidaLN
    {
        Respuesta<TTurnosPartidum> Insertar(TTurnosPartidum TurnosPartida);
        Respuesta<TTurnosPartidum> Modificar(TTurnosPartidum TurnosPartida);
        Respuesta<bool> Eliminar(TTurnosPartidum TurnosPartida);
        Respuesta<IEnumerable<TTurnosPartidum>> Obtener(TTurnosPartidum TurnosPartida);
        Respuesta<TTurnosPartidum> Buscar(TTurnosPartidum TurnosPartida);
        Respuesta<IEnumerable<TTurnosPartidum>> Listar();
    }
}
