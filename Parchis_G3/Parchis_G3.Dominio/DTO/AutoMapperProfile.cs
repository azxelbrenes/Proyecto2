using AutoMapper;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.EntidadesTipadas;

namespace Parchis_G3.Dominio.DTO
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Sala
            CreateMap<Sala, TSala>().ReverseMap();

            // TiposArticulo
            CreateMap<TiposArticulo, TTiposArticulo>().ReverseMap();

            // Usuario
            CreateMap<Usuario, TUsuario>().ReverseMap();

            // Articulo
            CreateMap<Articulo, TArticulo>().ReverseMap();

            // UsuarioArticulo
            CreateMap<UsuarioArticulo, TUsuarioArticulo>().ReverseMap();

            // EquipamientoActivo
            CreateMap<EquipamientoActivo, TEquipamientoActivo>().ReverseMap();

            // Partida
            CreateMap<Partida, TPartida>().ReverseMap();

            // JugadoresPartidum
            CreateMap<JugadoresPartidum, TJugadoresPartidum>().ReverseMap();

            // EstadoFicha
            CreateMap<EstadoFicha, TEstadoFicha>().ReverseMap();

            // TurnosPartidum
            CreateMap<TurnosPartidum, TTurnosPartidum>().ReverseMap();

            // Transaccione
            CreateMap<Transaccione, TTransaccione>().ReverseMap();

            // HistorialPartida
            CreateMap<HistorialPartida, THistorialPartida>().ReverseMap();

            // MensajesChat
            CreateMap<MensajesChat, TMensajesChat>().ReverseMap();

            // FilaEspera
            CreateMap<FilaEspera, TFilaEspera>().ReverseMap();

            // SesionesActiva
            CreateMap<SesionesActiva, TSesionesActiva>().ReverseMap();
        }
    }
}