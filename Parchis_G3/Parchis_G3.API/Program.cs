using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Parchis_G3.AccesoDatos.Implementaciones;
using Parchis_G3.AccesoDatos.Model;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.LogicaNegocios.Implementaciones;
using Parchis_G3.LogicaNegocios.Motor;
using Parchis_G3.API.Services;
using Parchis_G3.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// BASE DE DATOS

builder.Services.AddDbContext<ParchisOnlineContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ================================================================
// AUTOMAPPER
// ================================================================
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// ================================================================
// ACCESO A DATOS (Scoped)
// ================================================================
// Scoped es obligatorio para EF Core: el DbContext no es thread-safe.
builder.Services.AddScoped<IUnidadTrabajoEF, UnidadTrabajoEF>();

// ================================================================
// LÓGICA DE NEGOCIOS (Scoped)
// ================================================================
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
builder.Services.AddScoped<IInventarioLN, InventarioLN>();
builder.Services.AddScoped<IRankingLN, RankingLN>();
builder.Services.AddScoped<IRecompensaLN, RecompensaLN>();
builder.Services.AddScoped<ILogroLN, LogroLN>();
// RF-03: reloj de 30 segundos por turno y bucle de turnos de bots
builder.Services.AddSingleton<TemporizadorTurnoService>();

// -- Seguridad (Scoped) --
// NO es Singleton porque no guarda estado en memoria: los intentos
// fallidos van a la BD para que sobrevivan a un reinicio del
// servidor. Si estuvieran en memoria, reiniciar la API desbloquearía
// a todos los atacantes.
builder.Services.AddScoped<ISeguridadLN, SeguridadLN>();

// ================================================================
// PAGOS CON PAYPAL
// ================================================================
// AddHttpClient registra el servicio con manejo correcto del pool
// de conexiones (evita agotamiento de sockets).
builder.Services.AddHttpClient<IPagoLN, PagoLN>();

// ================================================================
// SERVICIOS SINGLETON
// ================================================================

// JwtService es stateless — una instancia basta para toda la app.
builder.Services.AddSingleton<JwtService>();

// -- Motor del juego --
// Mantiene en memoria: turno actual, racha de 5's y dado pendiente
// de cada partida. Si fuera Scoped, esa info se perdería en cada
// request. Por eso sus métodos reciben IUnidadTrabajoEF como
// parámetro (un Singleton no puede depender de un Scoped).
builder.Services.AddSingleton<IMotorParchisLN, MotorParchisLN>();

// -- Bots --
// Depende del Motor (Singleton), así que también debe serlo.
builder.Services.AddSingleton<IBotServiceLN, BotServiceLN>();

// -- Matchmaking --
// Mantiene el cronómetro de 30 segundos de cada partida en espera.
builder.Services.AddSingleton<IMatchmakingLN, MatchmakingLN>();

// -- Chat --
// Mantiene el timestamp del último mensaje de cada jugador para
// el cooldown anti-spam de 5 segundos.
builder.Services.AddSingleton<IChatLN, ChatLN>();

// -- Abandono y reconexión --
// Mantiene el temporizador de 60 segundos de cada desconectado.
builder.Services.AddSingleton<IAbandonoLN, AbandonoLN>();

// ================================================================
// SIGNALR
// ================================================================
// IMPORTANTE: SignalR tiene su PROPIO serializador, independiente
// del de los controllers. Configurar AddControllers().AddJsonOptions
// no afecta al Hub.
//
// Sin AddJsonProtocol, el Hub manda camelCase (jugadores, fichas,
// turnoActualJpId) mientras el frontend lee PascalCase (Jugadores,
// Fichas, TurnoActualJpId). El objeto llega pero todas sus
// propiedades resultan undefined: el tablero se dibuja vacío, sin
// fichas y sin barra de jugadores.
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });

// ================================================================
// RATE LIMITING — protección contra fuerza bruta y abuso
// ================================================================
// ¿QUÉ HACE?
// Limita cuántos requests puede hacer una IP en un período.
// Sin esto, un bot puede probar 10,000 contraseñas por minuto.
//
// Usamos particionamiento por IP: cada dirección tiene su propio
// contador, así un atacante no afecta a los usuarios legítimos.
builder.Services.AddRateLimiter(options =>
{
    // Cuando se supera el límite devolvemos 429 Too Many Requests
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // ── Política para LOGIN: 5 intentos por minuto por IP ────────
    // Es la primera barrera contra fuerza bruta. La segunda es
    // el bloqueo de cuenta tras 5 fallos (SeguridadLN).
    options.AddPolicy("login", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0   // Sin cola: se rechaza de una
            })
    );

    // ── Política para REGISTRO: 3 cuentas por hora por IP ────────
    // Evita que un bot cree miles de cuentas falsas.
    options.AddPolicy("registro", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            })
    );

    // ── Política global: 100 requests por minuto por IP ──────────
    // Protege todos los demás endpoints contra abuso general.
    // 100/min es holgado para uso normal pero frena scrapers.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
    {
        // Los WebSockets de SignalR no pasan por rate limiting —
        // una partida hace muchos mensajes y sería contraproducente
        if (contexto.Request.Path.StartsWithSegments("/hubs"))
            return RateLimitPartition.GetNoLimiter<string>("signalr");

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    // Mensaje claro cuando se rechaza por exceso de requests
    options.OnRejected = async (contexto, token) =>
    {
        contexto.HttpContext.Response.ContentType = "application/json";
        await contexto.HttpContext.Response.WriteAsync(
            "{\"mensaje\":\"Demasiadas solicitudes. Esperá un momento e intentá de nuevo.\"}",
            token);
    };
});

// ================================================================
// AUTENTICACIÓN JWT
// ================================================================
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key no configurada en appsettings.");

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Cambiar a true en producción
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Los WebSockets no soportan headers personalizados, así que
    // SignalR manda el token como query string (?access_token=...).
    // Este bloque lo lee de ahí cuando la petición va a un Hub.
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

// ================================================================
// CORS
// ================================================================
// SignalR NO funciona con AllowAnyOrigin() porque necesita
// AllowCredentials(), y ambos son incompatibles.
builder.Services.AddCors(options =>
{
    options.AddPolicy("PoliticaAndroid", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:8100",
                  "http://localhost:8101",
                  "http://localhost:8102",
                  "capacitor://localhost",
                  "http://localhost"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ================================================================
// CONTROLLERS Y SWAGGER
// ================================================================
// PropertyNamingPolicy = null mantiene PascalCase en el JSON,
// que es lo que espera el frontend Ionic.
// Ojo: esto SOLO aplica a los controllers. El Hub de SignalR se
// configura por separado más arriba.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ================================================================
// MIDDLEWARES (el orden importa)
// ================================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ── Headers de seguridad ─────────────────────────────────────────
// Cabeceras HTTP que protegen contra ataques comunes del navegador.
// Se agregan a TODAS las respuestas de la API.
app.Use(async (context, next) =>
{
    // Impide que el navegador "adivine" el tipo de contenido —
    // previene que un archivo subido se ejecute como script
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";

    // Impide que la API se cargue dentro de un iframe —
    // previene ataques de clickjacking
    context.Response.Headers["X-Frame-Options"] = "DENY";

    // No enviar la URL completa como referer a sitios externos
    context.Response.Headers["Referrer-Policy"] = "no-referrer";

    // Desactiva APIs del navegador que la API no necesita
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

    await next();
});

app.UseHttpsRedirection();
app.UseCors("PoliticaAndroid");

// El rate limiter va ANTES de autenticación — así frena los
// ataques antes de gastar recursos validando tokens
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHub<PartidaHub>("/hubs/partida");

app.Run();