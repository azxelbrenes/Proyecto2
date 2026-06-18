using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.AccesoDatos.Model;
using Microsoft.EntityFrameworkCore.Storage;

namespace Parchis_G3.AccesoDatos.Implementaciones
{
    public class UnidadTrabajoEF : IUnidadTrabajoEF
    {
        private readonly ParchisOnlineContext _contexto;
        private IDbContextTransaction? _transaccion;

        public IRepositorioAD<Sala> TSala { get; }
        public IRepositorioAD<TiposArticulo> TTiposArticulo { get; }
        public IRepositorioAD<Usuario> TUsuario { get; }
        public IRepositorioAD<Articulo> TArticulo { get; }
        public IRepositorioAD<UsuarioArticulo> TUsuarioArticulo { get; }
        public IRepositorioAD<EquipamientoActivo> TEquipamientoActivo { get; }
        public IRepositorioAD<Partida> TPartida { get; }
        public IRepositorioAD<JugadoresPartidum> TJugadoresPartida { get; }
        public IRepositorioAD<EstadoFicha> TEstadoFicha { get; }
        public IRepositorioAD<TurnosPartidum> TTurnoPartida { get; }
        public IRepositorioAD<Transaccione> TTransaccion { get; }
        public IRepositorioAD<HistorialPartida> THistorialPartida { get; }
        public IRepositorioAD<MensajesChat> TMensajesChat { get; }
        public IRepositorioAD<FilaEspera> TFilaEspera { get; }
        public IRepositorioAD<SesionesActiva> TSesionesActiva { get; }

        public UnidadTrabajoEF(ParchisOnlineContext contexto)
        {
            _contexto = contexto;

            TSala = new RepositorioAD<Sala>(_contexto);
            TTiposArticulo = new RepositorioAD<TiposArticulo>(_contexto);
            TUsuario = new RepositorioAD<Usuario>(_contexto);
            TArticulo = new RepositorioAD<Articulo>(_contexto);
            TUsuarioArticulo = new RepositorioAD<UsuarioArticulo>(_contexto);
            TEquipamientoActivo = new RepositorioAD<EquipamientoActivo>(_contexto);
            TPartida = new RepositorioAD<Partida>(_contexto);
            TJugadoresPartida = new RepositorioAD<JugadoresPartidum>(_contexto);
            TEstadoFicha = new RepositorioAD<EstadoFicha>(_contexto);
            TTurnoPartida = new RepositorioAD<TurnosPartidum>(_contexto);
            TTransaccion = new RepositorioAD<Transaccione>(_contexto);
            THistorialPartida = new RepositorioAD<HistorialPartida>(_contexto);
            TMensajesChat = new RepositorioAD<MensajesChat>(_contexto);
            TFilaEspera = new RepositorioAD<FilaEspera>(_contexto);
            TSesionesActiva = new RepositorioAD<SesionesActiva>(_contexto);
        }

        public void EmpezarTransaccion()
        {
            if (_transaccion == null)
            {
                _transaccion = _contexto.Database.BeginTransaction();
            }
        }

        public void Completar()
        {
            _contexto.SaveChanges();
        }

        public void CompletarTran()
        {
            try
            {
                _transaccion?.Commit();
            }
            finally
            {
                _transaccion?.Dispose();
                _transaccion = null;
            }
        }

        public void Rollback()
        {
            try
            {
                _transaccion?.Rollback();
            }
            finally
            {
                _transaccion?.Dispose();
                _transaccion = null;
            }
        }
    }
}