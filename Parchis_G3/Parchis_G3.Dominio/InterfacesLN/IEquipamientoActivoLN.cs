using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesLN
{
    public interface IEquipamientoActivoLN
    {
        Respuesta<TEquipamientoActivo> Insertar(TEquipamientoActivo EquipamientoActivo);
        Respuesta<TEquipamientoActivo> Modificar(TEquipamientoActivo EquipamientoActivo);
        Respuesta<bool> Eliminar(TEquipamientoActivo EquipamientoActivo);
        Respuesta<IEnumerable<TEquipamientoActivo>> Obtener(TEquipamientoActivo EquipamientoActivo);
        Respuesta<TEquipamientoActivo> Buscar(TEquipamientoActivo EquipamientoActivo);
        Respuesta<IEnumerable<TEquipamientoActivo>> Listar();
    }
}
