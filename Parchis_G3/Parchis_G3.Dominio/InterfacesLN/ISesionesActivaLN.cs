using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
    public interface ISesionesActivaLN
    {
        Respuesta<TSesionesActiva> Insertar(TSesionesActiva SesionesActiva);
        Respuesta<TSesionesActiva> Modificar(TSesionesActiva SesionesActiva);
        Respuesta<bool> Eliminar(TSesionesActiva SesionesActiva);
        Respuesta<IEnumerable<TSesionesActiva>> Obtener(TSesionesActiva SesionesActiva);
        Respuesta<TSesionesActiva> Buscar(TSesionesActiva SesionesActiva);
        Respuesta<IEnumerable<TSesionesActiva>> Listar();
    }
}
