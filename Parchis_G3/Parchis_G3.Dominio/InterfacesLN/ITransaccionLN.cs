using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
    public interface ITransaccionLN
    {
        Respuesta<TTransaccione> Insertar(TTransaccione Transaccion);
        Respuesta<TTransaccione> Modificar(TTransaccione Transaccion);
        Respuesta<bool> Eliminar(TTransaccione Transaccion);
        Respuesta<IEnumerable<TTransaccione>> Obtener(TTransaccione Transaccion);
        Respuesta<TTransaccione> Buscar(TTransaccione Transaccion);
        Respuesta<IEnumerable<TTransaccione>> Listar();
    }
}
