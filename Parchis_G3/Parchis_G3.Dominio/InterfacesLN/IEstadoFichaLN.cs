using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
    public interface IEstadoFichaLN
    {
        Respuesta<TEstadoFicha> Insertar(TEstadoFicha EstadoFicha);
        Respuesta<TEstadoFicha> Modificar(TEstadoFicha EstadoFicha);
        Respuesta<bool> Eliminar(TEstadoFicha EstadoFicha);
        Respuesta<IEnumerable<TEstadoFicha>> Obtener(TEstadoFicha EstadoFicha);
        Respuesta<TEstadoFicha> Buscar(TEstadoFicha EstadoFicha);
        Respuesta<IEnumerable<TEstadoFicha>> Listar();
    }
}
