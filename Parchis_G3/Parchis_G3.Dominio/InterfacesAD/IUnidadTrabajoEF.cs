using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;

namespace Parchis_G3.Dominio.InterfacesAD
{

    public interface IUnidadTrabajoEF
    {
        IRepositorioAD<Sala> TSala { get; }
        IRepositorioAD<TiposArticulo> TTiposArticulo { get; }
        IRepositorioAD<Usuario> TUsuario { get; }
        IRepositorioAD<Articulo> TArticulo { get; }
        IRepositorioAD<UsuarioArticulo> TUsuarioArticulo { get; }
        IRepositorioAD<EquipamientoActivo> TEquipamientoActivo { get; }
        IRepositorioAD<Partida> TPartida { get; }
        IRepositorioAD<JugadoresPartidum> TJugadoresPartida { get; }
        IRepositorioAD<EstadoFicha> TEstadoFicha { get; }
        IRepositorioAD<TurnosPartidum> TTurnoPartida { get; }
        IRepositorioAD<Transaccione> TTransaccion { get; }
        IRepositorioAD<HistorialPartida> THistorialPartida { get; }
        IRepositorioAD<MensajesChat> TMensajesChat { get; }
        IRepositorioAD<FilaEspera> TFilaEspera { get; }
        IRepositorioAD<SesionesActiva> TSesionesActiva { get; }

        void EmpezarTransaccion();
        void Completar();
        void CompletarTran();
        void Rollback();
    }
}