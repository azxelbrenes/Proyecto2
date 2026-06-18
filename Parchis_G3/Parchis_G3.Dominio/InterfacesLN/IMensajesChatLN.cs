using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
    public interface IMensajesChatLN
    {
        Respuesta<TMensajesChat> Insertar(TMensajesChat MensajesChat);
        Respuesta<TMensajesChat> Modificar(TMensajesChat MensajesChat);
        Respuesta<bool> Eliminar(TMensajesChat MensajesChat);
        Respuesta<IEnumerable<TMensajesChat>> Obtener(TMensajesChat MensajesChat);
        Respuesta<TMensajesChat> Buscar(TMensajesChat MensajesChat);
        Respuesta<IEnumerable<TMensajesChat>> Listar();
    }
}
