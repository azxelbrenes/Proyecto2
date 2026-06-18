using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Parchis_G3.Dominio.Entidades;

namespace Parchis_G3.AccesoDatos.Model;

public partial class ParchisOnlineContext : DbContext
{
    public ParchisOnlineContext()
    {
    }

    public ParchisOnlineContext(DbContextOptions<ParchisOnlineContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Articulo> Articulos { get; set; }

    public virtual DbSet<EquipamientoActivo> EquipamientoActivos { get; set; }

    public virtual DbSet<EstadoFicha> EstadoFichas { get; set; }

    public virtual DbSet<FilaEspera> FilaEsperas { get; set; }

    public virtual DbSet<HistorialPartida> HistorialPartidas { get; set; }

    public virtual DbSet<JugadoresPartidum> JugadoresPartida { get; set; }

    public virtual DbSet<MensajesChat> MensajesChats { get; set; }

    public virtual DbSet<Partida> Partidas { get; set; }

    public virtual DbSet<Sala> Salas { get; set; }

    public virtual DbSet<SesionesActiva> SesionesActivas { get; set; }

    public virtual DbSet<TiposArticulo> TiposArticulos { get; set; }

    public virtual DbSet<Transaccione> Transacciones { get; set; }

    public virtual DbSet<TurnosPartidum> TurnosPartida { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<UsuarioArticulo> UsuarioArticulos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=DESKTOP-OPRD7PU\\SQLEXPRESS;Initial Catalog=ParchisOnline;Persist Security Info=True;User ID=progra;Password=1234;MultipleActiveResultSets=False;Encrypt=false;TrustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Articulo>(entity =>
        {
            entity.HasKey(e => e.ArtId).HasName("PK__Articulo__FD7CB5B203A9C43D");

            entity.Property(e => e.ArtId).HasColumnName("Art_ID");
            entity.Property(e => e.ArtDescripcion)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("Art_Descripcion");
            entity.Property(e => e.ArtEsPredeterminado).HasColumnName("Art_EsPredeterminado");
            entity.Property(e => e.ArtEstado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("A")
                .IsFixedLength()
                .HasColumnName("Art_Estado");
            entity.Property(e => e.ArtImagenUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("Art_ImagenURL");
            entity.Property(e => e.ArtNombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Art_Nombre");
            entity.Property(e => e.ArtPrecio).HasColumnName("Art_Precio");
            entity.Property(e => e.TipId).HasColumnName("Tip_ID");

            entity.HasOne(d => d.Tip).WithMany(p => p.Articulos)
                .HasForeignKey(d => d.TipId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Articulos_Tipos");
        });

        modelBuilder.Entity<EquipamientoActivo>(entity =>
        {
            entity.HasKey(e => e.EquId).HasName("PK__Equipami__3482C8BB6E7A7C20");

            entity.ToTable("EquipamientoActivo");

            entity.HasIndex(e => new { e.UsuId, e.TipId }, "UQ_Equ_UsuarioTipo").IsUnique();

            entity.Property(e => e.EquId).HasColumnName("Equ_ID");
            entity.Property(e => e.ArtId).HasColumnName("Art_ID");
            entity.Property(e => e.TipId).HasColumnName("Tip_ID");
            entity.Property(e => e.UsuId).HasColumnName("Usu_ID");

            entity.HasOne(d => d.Art).WithMany(p => p.EquipamientoActivos)
                .HasForeignKey(d => d.ArtId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Equ_Articulo");

            entity.HasOne(d => d.Tip).WithMany(p => p.EquipamientoActivos)
                .HasForeignKey(d => d.TipId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Equ_Tipo");

            entity.HasOne(d => d.Usu).WithMany(p => p.EquipamientoActivos)
                .HasForeignKey(d => d.UsuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Equ_Usuario");
        });

        modelBuilder.Entity<EstadoFicha>(entity =>
        {
            entity.HasKey(e => e.EfId).HasName("PK__EstadoFi__6903DE6B70C9D680");

            entity.HasIndex(e => e.ParId, "IDX_EF_Partida");

            entity.HasIndex(e => new { e.JpId, e.EfNumeroFicha }, "UQ_EF_FichaJugador").IsUnique();

            entity.Property(e => e.EfId).HasColumnName("EF_ID");
            entity.Property(e => e.EfEstadoFicha)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("EN_CASA")
                .HasColumnName("EF_EstadoFicha");
            entity.Property(e => e.EfNumeroFicha).HasColumnName("EF_NumeroFicha");
            entity.Property(e => e.EfPosicion).HasColumnName("EF_Posicion");
            entity.Property(e => e.EfUltimaActualizacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("EF_UltimaActualizacion");
            entity.Property(e => e.JpId).HasColumnName("JP_ID");
            entity.Property(e => e.ParId).HasColumnName("Par_ID");

            entity.HasOne(d => d.Jp).WithMany(p => p.EstadoFichas)
                .HasForeignKey(d => d.JpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EF_Jugador");

            entity.HasOne(d => d.Par).WithMany(p => p.EstadoFichas)
                .HasForeignKey(d => d.ParId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EF_Partida");
        });

        modelBuilder.Entity<FilaEspera>(entity =>
        {
            entity.HasKey(e => e.FeId).HasName("PK__FilaEspe__93E2516C539C46E9");

            entity.ToTable("FilaEspera");

            entity.HasIndex(e => new { e.SalId, e.FeEstado }, "IDX_FE_Sala_Estado");

            entity.Property(e => e.FeId).HasColumnName("FE_ID");
            entity.Property(e => e.FeEstado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ESPERANDO")
                .HasColumnName("FE_Estado");
            entity.Property(e => e.FeFechaIngreso)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("FE_FechaIngreso");
            entity.Property(e => e.FeFechaSalida)
                .HasColumnType("datetime")
                .HasColumnName("FE_FechaSalida");
            entity.Property(e => e.FePosicion).HasColumnName("FE_Posicion");
            entity.Property(e => e.SalId).HasColumnName("Sal_ID");
            entity.Property(e => e.UsuId).HasColumnName("Usu_ID");

            entity.HasOne(d => d.Sal).WithMany(p => p.FilaEsperas)
                .HasForeignKey(d => d.SalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FE_Sala");

            entity.HasOne(d => d.Usu).WithMany(p => p.FilaEsperas)
                .HasForeignKey(d => d.UsuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FE_Usuario");
        });

        modelBuilder.Entity<HistorialPartida>(entity =>
        {
            entity.HasKey(e => e.HpId).HasName("PK__Historia__53D22398E517BB7E");

            entity.HasIndex(e => e.UsuId, "IDX_HP_Usuario");

            entity.HasIndex(e => new { e.UsuId, e.ParId }, "UQ_HP_UsuarioPartida").IsUnique();

            entity.Property(e => e.HpId).HasColumnName("HP_ID");
            entity.Property(e => e.HpDuracionMinutos).HasColumnName("HP_DuracionMinutos");
            entity.Property(e => e.HpFecha)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("HP_Fecha");
            entity.Property(e => e.HpMonedasGanadas).HasColumnName("HP_MonedasGanadas");
            entity.Property(e => e.HpResultado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("HP_Resultado");
            entity.Property(e => e.ParId).HasColumnName("Par_ID");
            entity.Property(e => e.SalId).HasColumnName("Sal_ID");
            entity.Property(e => e.UsuId).HasColumnName("Usu_ID");

            entity.HasOne(d => d.Par).WithMany(p => p.HistorialPartida)
                .HasForeignKey(d => d.ParId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HP_Partida");

            entity.HasOne(d => d.Sal).WithMany(p => p.HistorialPartida)
                .HasForeignKey(d => d.SalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HP_Sala");

            entity.HasOne(d => d.Usu).WithMany(p => p.HistorialPartida)
                .HasForeignKey(d => d.UsuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HP_Usuario");
        });

        modelBuilder.Entity<JugadoresPartidum>(entity =>
        {
            entity.HasKey(e => e.JpId).HasName("PK__Jugadore__BE8866C372A84534");

            entity.HasIndex(e => e.ParId, "IDX_JP_Partida");

            entity.HasIndex(e => new { e.ParId, e.JpColorFicha }, "UQ_JP_ColorPartida").IsUnique();

            entity.HasIndex(e => new { e.ParId, e.JpPosicion }, "UQ_JP_PosicionPartida").IsUnique();

            entity.Property(e => e.JpId).HasColumnName("JP_ID");
            entity.Property(e => e.JpColorFicha)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("JP_ColorFicha");
            entity.Property(e => e.JpEsBot).HasColumnName("JP_EsBot");
            entity.Property(e => e.JpEsGanador).HasColumnName("JP_EsGanador");
            entity.Property(e => e.JpEstadoConexion)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("CONECTADO")
                .HasColumnName("JP_EstadoConexion");
            entity.Property(e => e.JpFechaDesconexion)
                .HasColumnType("datetime")
                .HasColumnName("JP_FechaDesconexion");
            entity.Property(e => e.JpFechaUnion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("JP_FechaUnion");
            entity.Property(e => e.JpPosicion).HasColumnName("JP_Posicion");
            entity.Property(e => e.ParId).HasColumnName("Par_ID");
            entity.Property(e => e.UsuId).HasColumnName("Usu_ID");

            entity.HasOne(d => d.Par).WithMany(p => p.JugadoresPartida)
                .HasForeignKey(d => d.ParId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JP_Partida");

            entity.HasOne(d => d.Usu).WithMany(p => p.JugadoresPartida)
                .HasForeignKey(d => d.UsuId)
                .HasConstraintName("FK_JP_Usuario");
        });

        modelBuilder.Entity<MensajesChat>(entity =>
        {
            entity.HasKey(e => e.McId).HasName("PK__Mensajes__46F824D3D7B8A47B");

            entity.ToTable("MensajesChat");

            entity.HasIndex(e => new { e.ParId, e.McFecha }, "IDX_MC_Partida");

            entity.Property(e => e.McId).HasColumnName("MC_ID");
            entity.Property(e => e.JpId).HasColumnName("JP_ID");
            entity.Property(e => e.McContenido)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("MC_Contenido");
            entity.Property(e => e.McEsPredefinido).HasColumnName("MC_EsPredefinido");
            entity.Property(e => e.McFecha)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("MC_Fecha");
            entity.Property(e => e.ParId).HasColumnName("Par_ID");

            entity.HasOne(d => d.Jp).WithMany(p => p.MensajesChats)
                .HasForeignKey(d => d.JpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MC_Jugador");

            entity.HasOne(d => d.Par).WithMany(p => p.MensajesChats)
                .HasForeignKey(d => d.ParId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MC_Partida");
        });

        modelBuilder.Entity<Partida>(entity =>
        {
            entity.HasKey(e => e.ParId).HasName("PK__Partidas__62046A2E57D9BDCE");

            entity.HasIndex(e => new { e.SalId, e.ParEstado }, "IDX_Partidas_Sala");

            entity.Property(e => e.ParId).HasColumnName("Par_ID");
            entity.Property(e => e.ParEstado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ESPERANDO")
                .HasColumnName("Par_Estado");
            entity.Property(e => e.ParFechaFin)
                .HasColumnType("datetime")
                .HasColumnName("Par_FechaFin");
            entity.Property(e => e.ParFechaInicio)
                .HasColumnType("datetime")
                .HasColumnName("Par_FechaInicio");
            entity.Property(e => e.ParPremioTotal).HasColumnName("Par_PremioTotal");
            entity.Property(e => e.SalId).HasColumnName("Sal_ID");

            entity.HasOne(d => d.Sal).WithMany(p => p.Partida)
                .HasForeignKey(d => d.SalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Partidas_Salas");
        });

        modelBuilder.Entity<Sala>(entity =>
        {
            entity.HasKey(e => e.SalId).HasName("PK__Salas__04A47448CBCF3004");

            entity.HasIndex(e => e.SalNombre, "UQ__Salas__2EC73F3CD5D92F9B").IsUnique();

            entity.Property(e => e.SalId).HasColumnName("Sal_ID");
            entity.Property(e => e.SalComision)
                .HasDefaultValue(0.10m)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("Sal_Comision");
            entity.Property(e => e.SalCostoEntrada).HasColumnName("Sal_CostoEntrada");
            entity.Property(e => e.SalEstado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("A")
                .IsFixedLength()
                .HasColumnName("Sal_Estado");
            entity.Property(e => e.SalNombre)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Sal_Nombre");
            entity.Property(e => e.SalPremioBase).HasColumnName("Sal_PremioBase");
        });

        modelBuilder.Entity<SesionesActiva>(entity =>
        {
            entity.HasKey(e => e.SesId).HasName("PK__Sesiones__2EA80E8E2E193FDC");

            entity.HasIndex(e => new { e.UsuId, e.SesActiva }, "IDX_Ses_Usuario");

            entity.Property(e => e.SesId).HasColumnName("Ses_ID");
            entity.Property(e => e.SesActiva)
                .HasDefaultValue(true)
                .HasColumnName("Ses_Activa");
            entity.Property(e => e.SesDispositivoInfo)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("Ses_DispositivoInfo");
            entity.Property(e => e.SesFechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Ses_FechaCreacion");
            entity.Property(e => e.SesFechaExpiracion)
                .HasColumnType("datetime")
                .HasColumnName("Ses_FechaExpiracion");
            entity.Property(e => e.SesTokenHash)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("Ses_TokenHash");
            entity.Property(e => e.SesUltimaActividad)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Ses_UltimaActividad");
            entity.Property(e => e.UsuId).HasColumnName("Usu_ID");

            entity.HasOne(d => d.Usu).WithMany(p => p.SesionesActivas)
                .HasForeignKey(d => d.UsuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ses_Usuario");
        });

        modelBuilder.Entity<TiposArticulo>(entity =>
        {
            entity.HasKey(e => e.TipId).HasName("PK__TiposArt__9E728C8E28DF3242");

            entity.ToTable("TiposArticulo");

            entity.HasIndex(e => e.TipNombre, "UQ__TiposArt__943A785A55C0CF77").IsUnique();

            entity.Property(e => e.TipId).HasColumnName("Tip_ID");
            entity.Property(e => e.TipDescripcion)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("Tip_Descripcion");
            entity.Property(e => e.TipNombre)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Tip_Nombre");
        });

        modelBuilder.Entity<Transaccione>(entity =>
        {
            entity.HasKey(e => e.TranId).HasName("PK__Transacc__950EE6D06F50CF21");

            entity.HasIndex(e => new { e.UsuId, e.TranFecha }, "IDX_Tran_Usuario").IsDescending(false, true);

            entity.Property(e => e.TranId).HasColumnName("Tran_ID");
            entity.Property(e => e.ParId).HasColumnName("Par_ID");
            entity.Property(e => e.TranConcepto)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("Tran_Concepto");
            entity.Property(e => e.TranFecha)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Tran_Fecha");
            entity.Property(e => e.TranMonto).HasColumnName("Tran_Monto");
            entity.Property(e => e.TranReferenciaExt)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("Tran_ReferenciaExt");
            entity.Property(e => e.TranSaldoResultante).HasColumnName("Tran_SaldoResultante");
            entity.Property(e => e.TranTipo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("Tran_Tipo");
            entity.Property(e => e.UsuId).HasColumnName("Usu_ID");

            entity.HasOne(d => d.Par).WithMany(p => p.Transacciones)
                .HasForeignKey(d => d.ParId)
                .HasConstraintName("FK_Tran_Partida");

            entity.HasOne(d => d.Usu).WithMany(p => p.Transacciones)
                .HasForeignKey(d => d.UsuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tran_Usuario");
        });

        modelBuilder.Entity<TurnosPartidum>(entity =>
        {
            entity.HasKey(e => e.TurId).HasName("PK__TurnosPa__C30E60231086C021");

            entity.Property(e => e.TurId).HasColumnName("Tur_ID");
            entity.Property(e => e.JpId).HasColumnName("JP_ID");
            entity.Property(e => e.ParId).HasColumnName("Par_ID");
            entity.Property(e => e.TurFecha)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Tur_Fecha");
            entity.Property(e => e.TurFichaMovida).HasColumnName("Tur_FichaMovida");
            entity.Property(e => e.TurFueAutomatico).HasColumnName("Tur_FueAutomatico");
            entity.Property(e => e.TurHuboCaptura).HasColumnName("Tur_HuboCaptura");
            entity.Property(e => e.TurNumeroTurno).HasColumnName("Tur_NumeroTurno");
            entity.Property(e => e.TurPosicionAnterior).HasColumnName("Tur_PosicionAnterior");
            entity.Property(e => e.TurPosicionNueva).HasColumnName("Tur_PosicionNueva");
            entity.Property(e => e.TurResultadoDado).HasColumnName("Tur_ResultadoDado");

            entity.HasOne(d => d.Jp).WithMany(p => p.TurnosPartida)
                .HasForeignKey(d => d.JpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tur_Jugador");

            entity.HasOne(d => d.Par).WithMany(p => p.TurnosPartida)
                .HasForeignKey(d => d.ParId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tur_Partida");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.UsuId).HasName("PK__Usuarios__B6173FEB0BE5A9AB");

            entity.HasIndex(e => e.UsuCorreo, "IDX_Usuarios_Correo");

            entity.HasIndex(e => e.UsuMonedasGanadasPartida, "IDX_Usuarios_Ranking").IsDescending();

            entity.HasIndex(e => e.UsuCorreo, "UQ__Usuarios__3E7FB8933E65E72C").IsUnique();

            entity.Property(e => e.UsuId).HasColumnName("Usu_ID");
            entity.Property(e => e.UsuAbandonosConsecutivos).HasColumnName("Usu_AbandonosConsecutivos");
            entity.Property(e => e.UsuAvatar)
                .HasDefaultValue(1)
                .HasColumnName("Usu_Avatar");
            entity.Property(e => e.UsuBloqueado).HasColumnName("Usu_Bloqueado");
            entity.Property(e => e.UsuCorreo)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("Usu_Correo");
            entity.Property(e => e.UsuEstado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("A")
                .IsFixedLength()
                .HasColumnName("Usu_Estado");
            entity.Property(e => e.UsuFechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Usu_FechaCreacion");
            entity.Property(e => e.UsuFechaDesbloqueo)
                .HasColumnType("datetime")
                .HasColumnName("Usu_FechaDesbloqueo");
            entity.Property(e => e.UsuMonedasGanadasPartida).HasColumnName("Usu_MonedasGanadasPartida");
            entity.Property(e => e.UsuMonedasTotal)
                .HasDefaultValue(5000)
                .HasColumnName("Usu_MonedasTotal");
            entity.Property(e => e.UsuMusicaActiva)
                .HasDefaultValue(true)
                .HasColumnName("Usu_MusicaActiva");
            entity.Property(e => e.UsuNombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Usu_Nombre");
            entity.Property(e => e.UsuNotificacionesActivas)
                .HasDefaultValue(true)
                .HasColumnName("Usu_NotificacionesActivas");
            entity.Property(e => e.UsuPasswordHash)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("Usu_PasswordHash");
            entity.Property(e => e.UsuRachaDias).HasColumnName("Usu_RachaDias");
            entity.Property(e => e.UsuSonidosActivos)
                .HasDefaultValue(true)
                .HasColumnName("Usu_SonidosActivos");
            entity.Property(e => e.UsuTokenFcm)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("Usu_TokenFCM");
            entity.Property(e => e.UsuTutorialCompletado).HasColumnName("Usu_TutorialCompletado");
            entity.Property(e => e.UsuUltimaConexion).HasColumnName("Usu_UltimaConexion");
        });

        modelBuilder.Entity<UsuarioArticulo>(entity =>
        {
            entity.HasKey(e => e.UartId).HasName("PK__UsuarioA__451701BC5D4647C1");

            entity.HasIndex(e => new { e.UsuId, e.ArtId }, "UQ_UArt_UsuarioArticulo").IsUnique();

            entity.Property(e => e.UartId).HasColumnName("UArt_ID");
            entity.Property(e => e.ArtId).HasColumnName("Art_ID");
            entity.Property(e => e.UartFechaCompra)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("UArt_FechaCompra");
            entity.Property(e => e.UsuId).HasColumnName("Usu_ID");

            entity.HasOne(d => d.Art).WithMany(p => p.UsuarioArticulos)
                .HasForeignKey(d => d.ArtId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UArt_Articulo");

            entity.HasOne(d => d.Usu).WithMany(p => p.UsuarioArticulos)
                .HasForeignKey(d => d.UsuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UArt_Usuario");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
