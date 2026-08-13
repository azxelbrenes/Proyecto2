USE [ParchisOnline]
GO
/****** Object:  Table [dbo].[Articulos]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Articulos](
	[Art_ID] [int] IDENTITY(1,1) NOT NULL,
	[Tip_ID] [int] NOT NULL,
	[Art_Nombre] [varchar](100) NOT NULL,
	[Art_Descripcion] [varchar](300) NULL,
	[Art_Precio] [int] NOT NULL,
	[Art_ImagenURL] [varchar](500) NULL,
	[Art_EsPredeterminado] [bit] NOT NULL,
	[Art_Estado] [char](1) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Art_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EquipamientoActivo]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EquipamientoActivo](
	[Equ_ID] [int] IDENTITY(1,1) NOT NULL,
	[Usu_ID] [int] NOT NULL,
	[Tip_ID] [int] NOT NULL,
	[Art_ID] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Equ_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Equ_UsuarioTipo] UNIQUE NONCLUSTERED 
(
	[Usu_ID] ASC,
	[Tip_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EstadoFichas]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EstadoFichas](
	[EF_ID] [int] IDENTITY(1,1) NOT NULL,
	[Par_ID] [int] NOT NULL,
	[JP_ID] [int] NOT NULL,
	[EF_NumeroFicha] [int] NOT NULL,
	[EF_Posicion] [int] NOT NULL,
	[EF_EstadoFicha] [varchar](20) NOT NULL,
	[EF_UltimaActualizacion] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[EF_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_EF_FichaJugador] UNIQUE NONCLUSTERED 
(
	[JP_ID] ASC,
	[EF_NumeroFicha] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FilaEspera]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FilaEspera](
	[FE_ID] [int] IDENTITY(1,1) NOT NULL,
	[Usu_ID] [int] NOT NULL,
	[Sal_ID] [int] NOT NULL,
	[FE_Posicion] [int] NOT NULL,
	[FE_Estado] [varchar](20) NOT NULL,
	[FE_FechaIngreso] [datetime] NOT NULL,
	[FE_FechaSalida] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[FE_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[HistorialPartidas]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[HistorialPartidas](
	[HP_ID] [int] IDENTITY(1,1) NOT NULL,
	[Usu_ID] [int] NOT NULL,
	[Par_ID] [int] NOT NULL,
	[Sal_ID] [int] NOT NULL,
	[HP_Resultado] [varchar](20) NOT NULL,
	[HP_MonedasGanadas] [int] NOT NULL,
	[HP_Fecha] [datetime] NOT NULL,
	[HP_DuracionMinutos] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[HP_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_HP_UsuarioPartida] UNIQUE NONCLUSTERED 
(
	[Usu_ID] ASC,
	[Par_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[JugadoresPartida]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[JugadoresPartida](
	[JP_ID] [int] IDENTITY(1,1) NOT NULL,
	[Par_ID] [int] NOT NULL,
	[Usu_ID] [int] NULL,
	[JP_EsBot] [bit] NOT NULL,
	[JP_Posicion] [int] NOT NULL,
	[JP_ColorFicha] [varchar](10) NOT NULL,
	[JP_EstadoConexion] [varchar](20) NOT NULL,
	[JP_FechaDesconexion] [datetime] NULL,
	[JP_EsGanador] [bit] NOT NULL,
	[JP_FechaUnion] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[JP_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_JP_ColorPartida] UNIQUE NONCLUSTERED 
(
	[Par_ID] ASC,
	[JP_ColorFicha] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_JP_PosicionPartida] UNIQUE NONCLUSTERED 
(
	[Par_ID] ASC,
	[JP_Posicion] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MensajesChat]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MensajesChat](
	[MC_ID] [int] IDENTITY(1,1) NOT NULL,
	[Par_ID] [int] NOT NULL,
	[JP_ID] [int] NOT NULL,
	[MC_Contenido] [varchar](300) NOT NULL,
	[MC_EsPredefinido] [bit] NOT NULL,
	[MC_Fecha] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MC_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Partidas]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Partidas](
	[Par_ID] [int] IDENTITY(1,1) NOT NULL,
	[Sal_ID] [int] NOT NULL,
	[Par_Estado] [varchar](20) NOT NULL,
	[Par_FechaInicio] [datetime] NULL,
	[Par_FechaFin] [datetime] NULL,
	[Par_PremioTotal] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Par_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Salas]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Salas](
	[Sal_ID] [int] IDENTITY(1,1) NOT NULL,
	[Sal_Nombre] [varchar](50) NOT NULL,
	[Sal_CostoEntrada] [int] NOT NULL,
	[Sal_PremioBase] [int] NOT NULL,
	[Sal_Comision] [decimal](4, 2) NOT NULL,
	[Sal_Estado] [char](1) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Sal_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Sal_Nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SegLogs]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SegLogs](
	[Log_ID] [int] IDENTITY(1,1) NOT NULL,
	[Usu_ID] [int] NULL,
	[Log_Correo] [varchar](200) NULL,
	[Log_Evento] [varchar](50) NOT NULL,
	[Log_IP] [varchar](45) NULL,
	[Log_Detalle] [varchar](500) NULL,
	[Log_Fecha] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Log_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SesionesActivas]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SesionesActivas](
	[Ses_ID] [int] IDENTITY(1,1) NOT NULL,
	[Usu_ID] [int] NOT NULL,
	[Ses_TokenHash] [varchar](500) NOT NULL,
	[Ses_FechaCreacion] [datetime] NOT NULL,
	[Ses_FechaExpiracion] [datetime] NOT NULL,
	[Ses_UltimaActividad] [datetime] NOT NULL,
	[Ses_DispositivoInfo] [varchar](200) NULL,
	[Ses_Activa] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Ses_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TiposArticulo]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TiposArticulo](
	[Tip_ID] [int] IDENTITY(1,1) NOT NULL,
	[Tip_Nombre] [varchar](50) NOT NULL,
	[Tip_Descripcion] [varchar](200) NULL,
PRIMARY KEY CLUSTERED 
(
	[Tip_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Tip_Nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Transacciones]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Transacciones](
	[Tran_ID] [int] IDENTITY(1,1) NOT NULL,
	[Usu_ID] [int] NOT NULL,
	[Par_ID] [int] NULL,
	[Tran_Tipo] [varchar](30) NOT NULL,
	[Tran_Concepto] [varchar](200) NOT NULL,
	[Tran_Monto] [int] NOT NULL,
	[Tran_SaldoResultante] [int] NOT NULL,
	[Tran_ReferenciaExt] [varchar](200) NULL,
	[Tran_Fecha] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Tran_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TurnosPartida]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TurnosPartida](
	[Tur_ID] [int] IDENTITY(1,1) NOT NULL,
	[Par_ID] [int] NOT NULL,
	[JP_ID] [int] NOT NULL,
	[Tur_NumeroTurno] [int] NOT NULL,
	[Tur_ResultadoDado] [int] NOT NULL,
	[Tur_FichaMovida] [int] NULL,
	[Tur_PosicionAnterior] [int] NULL,
	[Tur_PosicionNueva] [int] NULL,
	[Tur_FueAutomatico] [bit] NOT NULL,
	[Tur_HuboCaptura] [bit] NOT NULL,
	[Tur_Fecha] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Tur_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UsuarioArticulos]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UsuarioArticulos](
	[UArt_ID] [int] IDENTITY(1,1) NOT NULL,
	[Usu_ID] [int] NOT NULL,
	[Art_ID] [int] NOT NULL,
	[UArt_FechaCompra] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UArt_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_UArt_UsuarioArticulo] UNIQUE NONCLUSTERED 
(
	[Usu_ID] ASC,
	[Art_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Usuarios]    Script Date: 12/8/2026 19:00:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Usuarios](
	[Usu_ID] [int] IDENTITY(1,1) NOT NULL,
	[Usu_Nombre] [varchar](100) NOT NULL,
	[Usu_Correo] [varchar](200) NOT NULL,
	[Usu_PasswordHash] [varchar](500) NOT NULL,
	[Usu_Avatar] [int] NOT NULL,
	[Usu_TokenFCM] [varchar](500) NULL,
	[Usu_MonedasTotal] [int] NOT NULL,
	[Usu_MonedasGanadasPartida] [int] NOT NULL,
	[Usu_RachaDias] [int] NOT NULL,
	[Usu_UltimaConexion] [date] NULL,
	[Usu_Bloqueado] [bit] NOT NULL,
	[Usu_FechaDesbloqueo] [datetime] NULL,
	[Usu_AbandonosConsecutivos] [int] NOT NULL,
	[Usu_TutorialCompletado] [bit] NOT NULL,
	[Usu_NotificacionesActivas] [bit] NOT NULL,
	[Usu_SonidosActivos] [bit] NOT NULL,
	[Usu_MusicaActiva] [bit] NOT NULL,
	[Usu_Estado] [char](1) NOT NULL,
	[Usu_FechaCreacion] [datetime] NOT NULL,
	[Usu_IntentosFallidos] [int] NOT NULL,
	[Usu_FechaUltimoIntento] [datetime] NULL,


	INSERT INTO Salas (Sal_Nombre, Sal_CostoEntrada, Sal_PremioBase, Sal_Comision) VALUES
('Sala Bronce',   500,   1800,  0.10),
('Sala Plata',    1000,  3600,  0.10),
('Sala Oro',      2000,  7200,  0.10),
('Sala Diamante', 5000,  18000, 0.10),
('Sala Élite',    10000, 36000, 0.10);
GO

-- Tipos de artículo de la tienda
INSERT INTO TiposArticulo (Tip_Nombre, Tip_Descripcion) VALUES
('Ficha',   'Diseños alternativos para las fichas del jugador'),
('Tablero', 'Diseños alternativos para el tablero del juego'),
('Dado',    'Diseños alternativos para el dado');
GO

-- Artículos de la tienda
-- Los predeterminados son gratis y todos los tienen desde el inicio
INSERT INTO Articulos (Tip_ID, Art_Nombre, Art_Descripcion, Art_Precio, Art_EsPredeterminado) VALUES
-- Fichas (Tip_ID = 1)
(1, 'Ficha Clásica',   'Ficha estándar del juego',          0,    1),
(1, 'Ficha Dorada',    'Ficha con acabado dorado brillante', 2000, 0),
(1, 'Ficha Cristal',   'Ficha transparente con efecto cristal', 3500, 0),
(1, 'Ficha Neón',      'Ficha con efecto de luz neón',      5000, 0),
(1, 'Ficha Diamante',  'Ficha exclusiva con efecto diamante', 10000, 0),
-- Tableros (Tip_ID = 2)
(2, 'Tablero Clásico', 'Tablero estándar del juego',        0,    1),
(2, 'Tablero Madera',  'Tablero con textura de madera fina', 3000, 0),
(2, 'Tablero Galaxia', 'Tablero con fondo de galaxia',      6000, 0),
(2, 'Tablero Dorado',  'Tablero con acabados dorados',      8000, 0),
-- Dados (Tip_ID = 3)
(3, 'Dado Clásico',    'Dado estándar del juego',           0,    1),
(3, 'Dado Neón',       'Dado con efecto de luz neón',       1500, 0),
(3, 'Dado Cristal',    'Dado transparente con efecto cristal', 2500, 0),
(3, 'Dado Dorado',     'Dado con acabado dorado brillante', 4000, 0);
GO
PRIMARY KEY CLUSTERED 
(
	[Usu_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Usu_Correo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Articulos] ADD  DEFAULT ((0)) FOR [Art_Precio]
GO
ALTER TABLE [dbo].[Articulos] ADD  DEFAULT ((0)) FOR [Art_EsPredeterminado]
GO
ALTER TABLE [dbo].[Articulos] ADD  DEFAULT ('A') FOR [Art_Estado]
GO
ALTER TABLE [dbo].[EstadoFichas] ADD  DEFAULT ((0)) FOR [EF_Posicion]
GO
ALTER TABLE [dbo].[EstadoFichas] ADD  DEFAULT ('EN_CASA') FOR [EF_EstadoFicha]
GO
ALTER TABLE [dbo].[EstadoFichas] ADD  DEFAULT (getdate()) FOR [EF_UltimaActualizacion]
GO
ALTER TABLE [dbo].[FilaEspera] ADD  DEFAULT ('ESPERANDO') FOR [FE_Estado]
GO
ALTER TABLE [dbo].[FilaEspera] ADD  DEFAULT (getdate()) FOR [FE_FechaIngreso]
GO
ALTER TABLE [dbo].[HistorialPartidas] ADD  DEFAULT ((0)) FOR [HP_MonedasGanadas]
GO
ALTER TABLE [dbo].[HistorialPartidas] ADD  DEFAULT (getdate()) FOR [HP_Fecha]
GO
ALTER TABLE [dbo].[JugadoresPartida] ADD  DEFAULT ((0)) FOR [JP_EsBot]
GO
ALTER TABLE [dbo].[JugadoresPartida] ADD  DEFAULT ('CONECTADO') FOR [JP_EstadoConexion]
GO
ALTER TABLE [dbo].[JugadoresPartida] ADD  DEFAULT ((0)) FOR [JP_EsGanador]
GO
ALTER TABLE [dbo].[JugadoresPartida] ADD  DEFAULT (getdate()) FOR [JP_FechaUnion]
GO
ALTER TABLE [dbo].[MensajesChat] ADD  DEFAULT ((0)) FOR [MC_EsPredefinido]
GO
ALTER TABLE [dbo].[MensajesChat] ADD  DEFAULT (getdate()) FOR [MC_Fecha]
GO
ALTER TABLE [dbo].[Partidas] ADD  DEFAULT ('ESPERANDO') FOR [Par_Estado]
GO
ALTER TABLE [dbo].[Partidas] ADD  DEFAULT ((0)) FOR [Par_PremioTotal]
GO
ALTER TABLE [dbo].[Salas] ADD  DEFAULT ((0.10)) FOR [Sal_Comision]
GO
ALTER TABLE [dbo].[Salas] ADD  DEFAULT ('A') FOR [Sal_Estado]
GO
ALTER TABLE [dbo].[SegLogs] ADD  DEFAULT (getdate()) FOR [Log_Fecha]
GO
ALTER TABLE [dbo].[SesionesActivas] ADD  DEFAULT (getdate()) FOR [Ses_FechaCreacion]
GO
ALTER TABLE [dbo].[SesionesActivas] ADD  DEFAULT (getdate()) FOR [Ses_UltimaActividad]
GO
ALTER TABLE [dbo].[SesionesActivas] ADD  DEFAULT ((1)) FOR [Ses_Activa]
GO
ALTER TABLE [dbo].[Transacciones] ADD  DEFAULT (getdate()) FOR [Tran_Fecha]
GO
ALTER TABLE [dbo].[TurnosPartida] ADD  DEFAULT ((0)) FOR [Tur_FueAutomatico]
GO
ALTER TABLE [dbo].[TurnosPartida] ADD  DEFAULT ((0)) FOR [Tur_HuboCaptura]
GO
ALTER TABLE [dbo].[TurnosPartida] ADD  DEFAULT (getdate()) FOR [Tur_Fecha]
GO
ALTER TABLE [dbo].[UsuarioArticulos] ADD  DEFAULT (getdate()) FOR [UArt_FechaCompra]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((1)) FOR [Usu_Avatar]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((5000)) FOR [Usu_MonedasTotal]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((0)) FOR [Usu_MonedasGanadasPartida]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((0)) FOR [Usu_RachaDias]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((0)) FOR [Usu_Bloqueado]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((0)) FOR [Usu_AbandonosConsecutivos]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((0)) FOR [Usu_TutorialCompletado]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((1)) FOR [Usu_NotificacionesActivas]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((1)) FOR [Usu_SonidosActivos]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((1)) FOR [Usu_MusicaActiva]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ('A') FOR [Usu_Estado]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT (getdate()) FOR [Usu_FechaCreacion]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((0)) FOR [Usu_IntentosFallidos]
GO
ALTER TABLE [dbo].[Articulos]  WITH CHECK ADD  CONSTRAINT [FK_Articulos_Tipos] FOREIGN KEY([Tip_ID])
REFERENCES [dbo].[TiposArticulo] ([Tip_ID])
GO
ALTER TABLE [dbo].[Articulos] CHECK CONSTRAINT [FK_Articulos_Tipos]
GO
ALTER TABLE [dbo].[EquipamientoActivo]  WITH CHECK ADD  CONSTRAINT [FK_Equ_Articulo] FOREIGN KEY([Art_ID])
REFERENCES [dbo].[Articulos] ([Art_ID])
GO
ALTER TABLE [dbo].[EquipamientoActivo] CHECK CONSTRAINT [FK_Equ_Articulo]
GO
ALTER TABLE [dbo].[EquipamientoActivo]  WITH CHECK ADD  CONSTRAINT [FK_Equ_Tipo] FOREIGN KEY([Tip_ID])
REFERENCES [dbo].[TiposArticulo] ([Tip_ID])
GO
ALTER TABLE [dbo].[EquipamientoActivo] CHECK CONSTRAINT [FK_Equ_Tipo]
GO
ALTER TABLE [dbo].[EquipamientoActivo]  WITH CHECK ADD  CONSTRAINT [FK_Equ_Usuario] FOREIGN KEY([Usu_ID])
REFERENCES [dbo].[Usuarios] ([Usu_ID])
GO
ALTER TABLE [dbo].[EquipamientoActivo] CHECK CONSTRAINT [FK_Equ_Usuario]
GO
ALTER TABLE [dbo].[EstadoFichas]  WITH CHECK ADD  CONSTRAINT [FK_EF_Jugador] FOREIGN KEY([JP_ID])
REFERENCES [dbo].[JugadoresPartida] ([JP_ID])
GO
ALTER TABLE [dbo].[EstadoFichas] CHECK CONSTRAINT [FK_EF_Jugador]
GO
ALTER TABLE [dbo].[EstadoFichas]  WITH CHECK ADD  CONSTRAINT [FK_EF_Partida] FOREIGN KEY([Par_ID])
REFERENCES [dbo].[Partidas] ([Par_ID])
GO
ALTER TABLE [dbo].[EstadoFichas] CHECK CONSTRAINT [FK_EF_Partida]
GO
ALTER TABLE [dbo].[FilaEspera]  WITH CHECK ADD  CONSTRAINT [FK_FE_Sala] FOREIGN KEY([Sal_ID])
REFERENCES [dbo].[Salas] ([Sal_ID])
GO
ALTER TABLE [dbo].[FilaEspera] CHECK CONSTRAINT [FK_FE_Sala]
GO
ALTER TABLE [dbo].[FilaEspera]  WITH CHECK ADD  CONSTRAINT [FK_FE_Usuario] FOREIGN KEY([Usu_ID])
REFERENCES [dbo].[Usuarios] ([Usu_ID])
GO
ALTER TABLE [dbo].[FilaEspera] CHECK CONSTRAINT [FK_FE_Usuario]
GO
ALTER TABLE [dbo].[HistorialPartidas]  WITH CHECK ADD  CONSTRAINT [FK_HP_Partida] FOREIGN KEY([Par_ID])
REFERENCES [dbo].[Partidas] ([Par_ID])
GO
ALTER TABLE [dbo].[HistorialPartidas] CHECK CONSTRAINT [FK_HP_Partida]
GO
ALTER TABLE [dbo].[HistorialPartidas]  WITH CHECK ADD  CONSTRAINT [FK_HP_Sala] FOREIGN KEY([Sal_ID])
REFERENCES [dbo].[Salas] ([Sal_ID])
GO
ALTER TABLE [dbo].[HistorialPartidas] CHECK CONSTRAINT [FK_HP_Sala]
GO
ALTER TABLE [dbo].[HistorialPartidas]  WITH CHECK ADD  CONSTRAINT [FK_HP_Usuario] FOREIGN KEY([Usu_ID])
REFERENCES [dbo].[Usuarios] ([Usu_ID])
GO
ALTER TABLE [dbo].[HistorialPartidas] CHECK CONSTRAINT [FK_HP_Usuario]
GO
ALTER TABLE [dbo].[JugadoresPartida]  WITH CHECK ADD  CONSTRAINT [FK_JP_Partida] FOREIGN KEY([Par_ID])
REFERENCES [dbo].[Partidas] ([Par_ID])
GO
ALTER TABLE [dbo].[JugadoresPartida] CHECK CONSTRAINT [FK_JP_Partida]
GO
ALTER TABLE [dbo].[JugadoresPartida]  WITH CHECK ADD  CONSTRAINT [FK_JP_Usuario] FOREIGN KEY([Usu_ID])
REFERENCES [dbo].[Usuarios] ([Usu_ID])
GO
ALTER TABLE [dbo].[JugadoresPartida] CHECK CONSTRAINT [FK_JP_Usuario]
GO
ALTER TABLE [dbo].[MensajesChat]  WITH CHECK ADD  CONSTRAINT [FK_MC_Jugador] FOREIGN KEY([JP_ID])
REFERENCES [dbo].[JugadoresPartida] ([JP_ID])
GO
ALTER TABLE [dbo].[MensajesChat] CHECK CONSTRAINT [FK_MC_Jugador]
GO
ALTER TABLE [dbo].[MensajesChat]  WITH CHECK ADD  CONSTRAINT [FK_MC_Partida] FOREIGN KEY([Par_ID])
REFERENCES [dbo].[Partidas] ([Par_ID])
GO
ALTER TABLE [dbo].[MensajesChat] CHECK CONSTRAINT [FK_MC_Partida]
GO
ALTER TABLE [dbo].[Partidas]  WITH CHECK ADD  CONSTRAINT [FK_Partidas_Salas] FOREIGN KEY([Sal_ID])
REFERENCES [dbo].[Salas] ([Sal_ID])
GO
ALTER TABLE [dbo].[Partidas] CHECK CONSTRAINT [FK_Partidas_Salas]
GO
ALTER TABLE [dbo].[SegLogs]  WITH CHECK ADD  CONSTRAINT [FK_SegLogs_Usuario] FOREIGN KEY([Usu_ID])
REFERENCES [dbo].[Usuarios] ([Usu_ID])
GO
ALTER TABLE [dbo].[SegLogs] CHECK CONSTRAINT [FK_SegLogs_Usuario]
GO
ALTER TABLE [dbo].[SesionesActivas]  WITH CHECK ADD  CONSTRAINT [FK_Ses_Usuario] FOREIGN KEY([Usu_ID])
REFERENCES [dbo].[Usuarios] ([Usu_ID])
GO
ALTER TABLE [dbo].[SesionesActivas] CHECK CONSTRAINT [FK_Ses_Usuario]
GO
ALTER TABLE [dbo].[Transacciones]  WITH CHECK ADD  CONSTRAINT [FK_Tran_Partida] FOREIGN KEY([Par_ID])
REFERENCES [dbo].[Partidas] ([Par_ID])
GO
ALTER TABLE [dbo].[Transacciones] CHECK CONSTRAINT [FK_Tran_Partida]
GO
ALTER TABLE [dbo].[Transacciones]  WITH CHECK ADD  CONSTRAINT [FK_Tran_Usuario] FOREIGN KEY([Usu_ID])
REFERENCES [dbo].[Usuarios] ([Usu_ID])
GO
ALTER TABLE [dbo].[Transacciones] CHECK CONSTRAINT [FK_Tran_Usuario]
GO
ALTER TABLE [dbo].[TurnosPartida]  WITH CHECK ADD  CONSTRAINT [FK_Tur_Jugador] FOREIGN KEY([JP_ID])
REFERENCES [dbo].[JugadoresPartida] ([JP_ID])
GO
ALTER TABLE [dbo].[TurnosPartida] CHECK CONSTRAINT [FK_Tur_Jugador]
GO
ALTER TABLE [dbo].[TurnosPartida]  WITH CHECK ADD  CONSTRAINT [FK_Tur_Partida] FOREIGN KEY([Par_ID])
REFERENCES [dbo].[Partidas] ([Par_ID])
GO
ALTER TABLE [dbo].[TurnosPartida] CHECK CONSTRAINT [FK_Tur_Partida]
GO
ALTER TABLE [dbo].[UsuarioArticulos]  WITH CHECK ADD  CONSTRAINT [FK_UArt_Articulo] FOREIGN KEY([Art_ID])
REFERENCES [dbo].[Articulos] ([Art_ID])
GO
ALTER TABLE [dbo].[UsuarioArticulos] CHECK CONSTRAINT [FK_UArt_Articulo]
GO
ALTER TABLE [dbo].[UsuarioArticulos]  WITH CHECK ADD  CONSTRAINT [FK_UArt_Usuario] FOREIGN KEY([Usu_ID])
REFERENCES [dbo].[Usuarios] ([Usu_ID])
GO
ALTER TABLE [dbo].[UsuarioArticulos] CHECK CONSTRAINT [FK_UArt_Usuario]
GO
ALTER TABLE [dbo].[Articulos]  WITH CHECK ADD  CONSTRAINT [CHK_Art_Estado] CHECK  (([Art_Estado]='I' OR [Art_Estado]='A'))
GO
ALTER TABLE [dbo].[Articulos] CHECK CONSTRAINT [CHK_Art_Estado]
GO
ALTER TABLE [dbo].[Articulos]  WITH CHECK ADD  CONSTRAINT [CHK_Art_Precio] CHECK  (([Art_Precio]>=(0)))
GO
ALTER TABLE [dbo].[Articulos] CHECK CONSTRAINT [CHK_Art_Precio]
GO
ALTER TABLE [dbo].[EstadoFichas]  WITH CHECK ADD  CONSTRAINT [CHK_EF_Estado] CHECK  (([EF_EstadoFicha]='CORONADA' OR [EF_EstadoFicha]='EN_JUEGO' OR [EF_EstadoFicha]='EN_CASA'))
GO
ALTER TABLE [dbo].[EstadoFichas] CHECK CONSTRAINT [CHK_EF_Estado]
GO
ALTER TABLE [dbo].[EstadoFichas]  WITH CHECK ADD  CONSTRAINT [CHK_EF_Numero] CHECK  (([EF_NumeroFicha]>=(1) AND [EF_NumeroFicha]<=(4)))
GO
ALTER TABLE [dbo].[EstadoFichas] CHECK CONSTRAINT [CHK_EF_Numero]
GO
ALTER TABLE [dbo].[EstadoFichas]  WITH CHECK ADD  CONSTRAINT [CHK_EF_Posicion] CHECK  (([EF_Posicion]>=(0) AND [EF_Posicion]<=(69)))
GO
ALTER TABLE [dbo].[EstadoFichas] CHECK CONSTRAINT [CHK_EF_Posicion]
GO
ALTER TABLE [dbo].[FilaEspera]  WITH CHECK ADD  CONSTRAINT [CHK_FE_Estado] CHECK  (([FE_Estado]='RETIRADO' OR [FE_Estado]='EN_PARTIDA' OR [FE_Estado]='ESPERANDO'))
GO
ALTER TABLE [dbo].[FilaEspera] CHECK CONSTRAINT [CHK_FE_Estado]
GO
ALTER TABLE [dbo].[HistorialPartidas]  WITH CHECK ADD  CONSTRAINT [CHK_HP_Resultado] CHECK  (([HP_Resultado]='ABANDONO' OR [HP_Resultado]='DERROTA' OR [HP_Resultado]='VICTORIA'))
GO
ALTER TABLE [dbo].[HistorialPartidas] CHECK CONSTRAINT [CHK_HP_Resultado]
GO
ALTER TABLE [dbo].[JugadoresPartida]  WITH CHECK ADD  CONSTRAINT [CHK_JP_Color] CHECK  (([JP_ColorFicha]='AMARILLO' OR [JP_ColorFicha]='VERDE' OR [JP_ColorFicha]='AZUL' OR [JP_ColorFicha]='ROJO'))
GO
ALTER TABLE [dbo].[JugadoresPartida] CHECK CONSTRAINT [CHK_JP_Color]
GO
ALTER TABLE [dbo].[JugadoresPartida]  WITH CHECK ADD  CONSTRAINT [CHK_JP_Conexion] CHECK  (([JP_EstadoConexion]='BOT' OR [JP_EstadoConexion]='DESCONECTADO' OR [JP_EstadoConexion]='RECONECTANDO' OR [JP_EstadoConexion]='CONECTADO'))
GO
ALTER TABLE [dbo].[JugadoresPartida] CHECK CONSTRAINT [CHK_JP_Conexion]
GO
ALTER TABLE [dbo].[JugadoresPartida]  WITH CHECK ADD  CONSTRAINT [CHK_JP_Posicion] CHECK  (([JP_Posicion]>=(1) AND [JP_Posicion]<=(4)))
GO
ALTER TABLE [dbo].[JugadoresPartida] CHECK CONSTRAINT [CHK_JP_Posicion]
GO
ALTER TABLE [dbo].[Partidas]  WITH CHECK ADD  CONSTRAINT [CHK_Par_Estado] CHECK  (([Par_Estado]='CANCELADA' OR [Par_Estado]='FINALIZADA' OR [Par_Estado]='EN_JUEGO' OR [Par_Estado]='ESPERANDO'))
GO
ALTER TABLE [dbo].[Partidas] CHECK CONSTRAINT [CHK_Par_Estado]
GO
ALTER TABLE [dbo].[Salas]  WITH CHECK ADD  CONSTRAINT [CHK_Sal_Comision] CHECK  (([Sal_Comision]>=(0) AND [Sal_Comision]<=(1)))
GO
ALTER TABLE [dbo].[Salas] CHECK CONSTRAINT [CHK_Sal_Comision]
GO
ALTER TABLE [dbo].[Salas]  WITH CHECK ADD  CONSTRAINT [CHK_Sal_Costo] CHECK  (([Sal_CostoEntrada]>(0)))
GO
ALTER TABLE [dbo].[Salas] CHECK CONSTRAINT [CHK_Sal_Costo]
GO
ALTER TABLE [dbo].[Salas]  WITH CHECK ADD  CONSTRAINT [CHK_Sal_Estado] CHECK  (([Sal_Estado]='I' OR [Sal_Estado]='A'))
GO
ALTER TABLE [dbo].[Salas] CHECK CONSTRAINT [CHK_Sal_Estado]
GO
ALTER TABLE [dbo].[SegLogs]  WITH CHECK ADD  CONSTRAINT [CHK_Log_Evento] CHECK  (([Log_Evento]='RATE_LIMIT' OR [Log_Evento]='ACCESO_DENEGADO' OR [Log_Evento]='LOGOUT' OR [Log_Evento]='PASSWORD_CAMBIADO' OR [Log_Evento]='REGISTRO' OR [Log_Evento]='CUENTA_BLOQUEADA' OR [Log_Evento]='LOGIN_FALLIDO' OR [Log_Evento]='LOGIN_EXITOSO'))
GO
ALTER TABLE [dbo].[SegLogs] CHECK CONSTRAINT [CHK_Log_Evento]
GO
ALTER TABLE [dbo].[Transacciones]  WITH CHECK ADD  CONSTRAINT [CHK_Tran_Tipo] CHECK  (([Tran_Tipo]='LOGRO' OR [Tran_Tipo]='BIENVENIDA' OR [Tran_Tipo]='RECOMPENSA_DIA' OR [Tran_Tipo]='ENTRADA_SALA' OR [Tran_Tipo]='PREMIO_PARTIDA' OR [Tran_Tipo]='PENALIZACION' OR [Tran_Tipo]='COMPRA_ARTICULO' OR [Tran_Tipo]='COMPRA_MONEDAS' OR [Tran_Tipo]='DEVOLUCION'))
GO
ALTER TABLE [dbo].[Transacciones] CHECK CONSTRAINT [CHK_Tran_Tipo]
GO
ALTER TABLE [dbo].[TurnosPartida]  WITH CHECK ADD  CONSTRAINT [CHK_Tur_Dado] CHECK  (([Tur_ResultadoDado]>=(1) AND [Tur_ResultadoDado]<=(6)))
GO
ALTER TABLE [dbo].[TurnosPartida] CHECK CONSTRAINT [CHK_Tur_Dado]
GO
ALTER TABLE [dbo].[TurnosPartida]  WITH CHECK ADD  CONSTRAINT [CHK_Tur_Ficha] CHECK  (([Tur_FichaMovida] IS NULL OR [Tur_FichaMovida]>=(1) AND [Tur_FichaMovida]<=(4)))
GO
ALTER TABLE [dbo].[TurnosPartida] CHECK CONSTRAINT [CHK_Tur_Ficha]
GO
ALTER TABLE [dbo].[Usuarios]  WITH CHECK ADD  CONSTRAINT [CHK_Usu_Avatar] CHECK  (([Usu_Avatar]>=(1) AND [Usu_Avatar]<=(20)))
GO
ALTER TABLE [dbo].[Usuarios] CHECK CONSTRAINT [CHK_Usu_Avatar]
GO
ALTER TABLE [dbo].[Usuarios]  WITH CHECK ADD  CONSTRAINT [CHK_Usu_Estado] CHECK  (([Usu_Estado]='I' OR [Usu_Estado]='A'))
GO
ALTER TABLE [dbo].[Usuarios] CHECK CONSTRAINT [CHK_Usu_Estado]
GO
ALTER TABLE [dbo].[Usuarios]  WITH CHECK ADD  CONSTRAINT [CHK_Usu_Monedas] CHECK  (([Usu_MonedasTotal]>=(0)))
GO
ALTER TABLE [dbo].[Usuarios] CHECK CONSTRAINT [CHK_Usu_Monedas]
GO
ALTER TABLE [dbo].[Usuarios]  WITH CHECK ADD  CONSTRAINT [CHK_Usu_Racha] CHECK  (([Usu_RachaDias]>=(0)))
GO
ALTER TABLE [dbo].[Usuarios] CHECK CONSTRAINT [CHK_Usu_Racha]
GO
