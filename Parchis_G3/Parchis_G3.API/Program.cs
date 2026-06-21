using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Parchis_G3.AccesoDatos.Implementaciones;
using Parchis_G3.AccesoDatos.Model;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.LogicaNegocios.Implementaciones;
using Parchis_G3.API.Servicios;

var builder = WebApplication.CreateBuilder(args);


// Registramos el contexto de Entity Framework apuntando a SQL Server.
// La cadena de conexión viene de appsettings.json por seguridad —
// nunca escribas usuario/contraseña directamente en el código.
builder.Services.AddDbContext<ParchisOnlineContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);


// Registra el perfil que mapea entre entidades EF y entidades tipadas.
// Se usa en la LogicaNegocios para convertir Sala ↔ TSala, etc.
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// -- AccesoDatos --
// Scoped es obligatorio para EF Core el DbContext no puede
// ser Singleton porque no es thread-safe (varios requests
// simultáneos pisarían el mismo contexto de BD).
builder.Services.AddScoped<IUnidadTrabajoEF, UnidadTrabajoEF>();

// -- LogicaNegocios --
// Las LN dependen de IUnidadTrabajoEF (Scoped), por lo tanto
// también deben ser Scoped. No se puede inyectar Scoped en Singleton.
builder.Services.AddScoped<IUsuarioLN, UsuarioLN>();
builder.Services.AddScoped<ISalaLN, SalaLN>();
builder.Services.AddScoped<IArticuloLN, ArticuloLN>();
builder.Services.AddScoped<ITiposArticuloLN, TiposArticuloLN>();
builder.Services.AddScoped<IUsuarioArticuloLN, UsuarioArticuloLN>();
builder.Services.AddScoped<IEquipamientoActivoLN, EquipamientoActivoLN>();
builder.Services.AddScoped<IPartidaLN, PartidaLN>();
builder.Services.AddScoped<IJugadoresPartidaLN, JugadoresPartidaLN>();
builder.Services.AddScoped<IEstadoFichaLN, EstadoFichaLN>();
builder.Services.AddScoped<ITurnosPartidaLN, TurnosPartidaLN>();
builder.Services.AddScoped<ITransaccionLN, TransaccionLN>();
builder.Services.AddScoped<IHistorialPartidaLN, HistorialPartidaLN>();
builder.Services.AddScoped<IMensajesChatLN, MensajesChatLN>();
builder.Services.AddScoped<IFilaEsperaLN, FilaEsperaLN>();
builder.Services.AddScoped<ISesionesActivaLN, SesionesActivaLN>();


// JwtService NO depende de la BD ni de EF, solo genera y valida tokens.
// Al ser stateless (sin estado interno), es perfecto como Singleton —
// una sola instancia compartida por todos los requests es suficiente.
builder.Services.AddSingleton<JwtService>();


// Configuramos cómo la API valida los tokens JWT que llegan en
// cada request. La clave secreta debe tener mínimo 256 bits.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key no configurada en appsettings.");

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    // Definimos JWT Bearer como el esquema de autenticación por defecto
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Cambiar a true en producción
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Validamos que el token fue firmado con NUESTRA clave secreta
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),

        // Para desarrollo no validamos issuer ni audience —
        // en producción deberías activarlos
        ValidateIssuer = false,
        ValidateAudience = false,

        // El token expira en el tiempo definido al crearlo
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Sin margen de tolerancia al expirar
    };
});

builder.Services.AddAuthorization();

// Permite que la app Android haga requests a esta API.
// En producción reemplazá "*" con la URL exacta de tu app.
builder.Services.AddCors(options =>
{
    options.AddPolicy("PoliticaAndroid", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── MIDDLEWARES (en este orden exacto) ──
// El orden importa: cada request pasa por estos en secuencia.

if (app.Environment.IsDevelopment())
{
    // Swagger solo en desarrollo — interfaz visual para probar endpoints
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); // Redirige HTTP → HTTPS
app.UseCors("PoliticaAndroid"); // Aplica la política CORS
app.UseAuthentication();  // Valida el token JWT ← debe ir ANTES de Authorization
app.UseAuthorization();   // Verifica roles y permisos
app.MapControllers();     // Registra todos los controllers

app.Run();