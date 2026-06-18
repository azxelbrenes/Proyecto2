using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Parchis_G3.Dominio.InterfacesAD
{
    public interface IRepositorioAD<T> where T : class
    {
        Respuesta<IEnumerable<T>> Listar(List<string>? includes = null);

        Respuesta<IEnumerable<T>> Buscar(
            Expression<Func<T, bool>> predicado,
            List<string>? includes = null);

        Respuesta<T> ObtenerEntidad(
            Expression<Func<T, bool>> predicado,
            List<string>? includes = null);

        Respuesta<T> Insertar(T entidad);

        Respuesta<T> Modificar(T entidad);

        Respuesta<bool> Eliminar(T entidad);

        Respuesta<int> Contar(Expression<Func<T, bool>> predicado);
    }

}
