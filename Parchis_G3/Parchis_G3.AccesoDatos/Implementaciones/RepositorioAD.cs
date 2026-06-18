using Microsoft.EntityFrameworkCore;
using Parchis_G3.AccesoDatos.Model;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Utilitarios;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Parchis_G3.AccesoDatos.Implementaciones;

public class RepositorioAD<T> : IRepositorioAD<T> where T : class
{
    private readonly ParchisOnlineContext _contexto;
    private readonly DbSet<T> _dbSet;

    public RepositorioAD(ParchisOnlineContext contexto)
    {
        _contexto = contexto;
        _dbSet = _contexto.Set<T>();
    }

    public Respuesta<IEnumerable<T>> Listar(List<string>? includes = null)
    {
        try
        {
            IQueryable<T> query = _dbSet.AsQueryable();
            if (includes != null)
                foreach (var inc in includes)
                    query = query.Include(inc);

            var lista = query.ToList();
            return Respuesta<IEnumerable<T>>.Exito(lista, "Listado obtenido correctamente.");
        }
        catch (Exception ex)
        {
            return Respuesta<IEnumerable<T>>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    public Respuesta<IEnumerable<T>> Buscar(Expression<Func<T, bool>> predicado, List<string>? includes = null)
    {
        try
        {
            IQueryable<T> query = _dbSet.Where(predicado);
            if (includes != null)
                foreach (var inc in includes)
                    query = query.Include(inc);

            var lista = query.ToList();
            return Respuesta<IEnumerable<T>>.Exito(lista, "Consulta realizada correctamente.");
        }
        catch (Exception ex)
        {
            return Respuesta<IEnumerable<T>>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    public Respuesta<T> ObtenerEntidad(Expression<Func<T, bool>> predicado, List<string>? includes = null)
    {
        try
        {
            IQueryable<T> query = _dbSet.Where(predicado);
            if (includes != null)
                foreach (var inc in includes)
                    query = query.Include(inc);

            var entidad = query.FirstOrDefault();
            if (entidad == null)
                return Respuesta<T>.Validacion("No se encontró el registro solicitado.");

            return Respuesta<T>.Exito(entidad, "Registro obtenido correctamente.");
        }
        catch (Exception ex)
        {
            return Respuesta<T>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    public Respuesta<T> Insertar(T entidad)
    {
        try
        {
            _dbSet.Add(entidad);
            return Respuesta<T>.Exito(entidad, "Registro insertado correctamente.");
        }
        catch (Exception ex)
        {
            return Respuesta<T>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    public Respuesta<T> Modificar(T entidad)
    {
        try
        {
            _dbSet.Update(entidad);
            return Respuesta<T>.Exito(entidad, "Registro modificado correctamente.");
        }
        catch (Exception ex)
        {
            return Respuesta<T>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    public Respuesta<bool> Eliminar(T entidad)
    {
        try
        {
            _dbSet.Remove(entidad);
            return Respuesta<bool>.Exito(true, "Registro eliminado correctamente.");
        }
        catch (Exception ex)
        {
            return Respuesta<bool>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    public Respuesta<int> Contar(Expression<Func<T, bool>> predicado)
    {
        try
        {
            var cantidad = _dbSet.Count(predicado);
            return Respuesta<int>.Exito(cantidad, "Conteo realizado correctamente.");
        }
        catch (Exception ex)
        {
            return Respuesta<int>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }
}