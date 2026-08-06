using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Parchis_G3.AccesoDatos.Implementaciones;
using Parchis_G3.AccesoDatos.Model;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.LogicaNegocios.Implementaciones;
using Parchis_G3.LogicaNegocios.Motor;   // ← NUEVO: Motor del juego y Bots
using Parchis_G3.API.Services;
using Parchis_G3.API.Hubs;               // ← NUEVO: Hub de SignalR

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

// -- Motor del juego (Singleton) --
// El Motor necesita mantener EN MEMORIA, mientras el servidor esté
// encendido: de quién es el turno en cada partida, la racha de 5's
// consecutivos y el valor de dado pendiente de usarse.
// Si fuera Scoped, toda esa información se perdería en cada request
// y el juego no podría funcionar.
// Por eso sus métodos reciben IUnidadTrabajoEF como PARÁMETRO en
// lugar de inyectarlo en el constructor (un Singleton no puede
// depender de un Scoped).
builder.Services.AddSingleton<IMotorParchisLN, MotorParchisLN>();

// -- Servicio de Bots (Singleton) --
// Depende de IMotorParchisLN que es Singleton, así que también
// debe serlo — un Singleton solo puede depender de otro Singleton.
builder.Services.AddSingleton<IBotServiceLN, BotServiceLN>();

builder.Services.AddSingleton<IMatchmakingLN, MatchmakingLN>();

// -- SignalR --
// Habilita la comunicación en tiempo real por WebSockets entre
// el servidor y los 4 celulares de una misma partida.
builder.Services.AddSignalR();


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

    // SignalR no puede mandar el token en el header Authorization
    // porque los WebSockets no lo soportan. En su lugar lo manda
    // como query string (?access_token=...). Este bloque lo lee
    // de ahí cuando la petición va dirigida a un Hub.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Permite que la app Ionic/Android haga requests a esta API.
// IMPORTANTE: SignalR NO funciona con AllowAnyOrigin() porque
// necesita AllowCredentials(), y ambos son incompatibles entre sí.
// Por eso listamos los orígenes exactos del frontend.
builder.Services.AddCors(options =>
{
    options.AddPolicy("PoliticaAndroid", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:8100",
                  "http://localhost:8101",
                  "http://localhost:8102",
                  "capacitor://localhost",   // para el APK en Android
                  "http://localhost"          // para el APK en Android
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configuramos que el JSON mantenga los nombres exactos de C# (PascalCase)
// en lugar de convertirlos a camelCase automáticamente.
// Esto es necesario porque el frontend Ionic espera nombres como
// "SalNombre" y "UsuMonedasTotal" tal cual están en las entidades C#.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

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

// Registra el Hub de SignalR en la ruta /hubs/partida
// El frontend Ionic se conectará a: http://localhost:5051/hubs/partida
app.MapHub<PartidaHub>("/hubs/partida");

app.Run();