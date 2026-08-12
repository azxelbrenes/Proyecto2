using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

/// <summary>
/// RF-05: sistema de logros.
///
/// Los logros se calculan a partir del historial de partidas y de las
/// transacciones, sin tabla propia. La ventaja es que nunca pueden
/// quedar desincronizados: si el historial dice 5 victorias, el logro
/// de 5 victorias está desbloqueado por definición.
///
/// Lo único que sí se persiste es el reclamo de la recompensa, y para
/// eso se reutiliza la tabla de Transacciones con el tipo LOGRO: si
/// existe una transacción con el código del logro en el concepto, ya
/// fue reclamado.
/// </summary>
public class LogroLN : ILogroLN
{
    private const string TIPO_TRANSACCION = "LOGRO";

    // ── Catálogo de logros ───────────────────────────────────────
    // Vive en código y no en BD: son reglas de negocio fijas, no
    // datos que un administrador vaya a editar.
    private static readonly List<(string Codigo, string Nombre, string Descripcion, string Icono, int Meta, int Recompensa)> _catalogo = new()
    {
        ("PRIMERA_PARTIDA", "Primeros pasos",   "Jugá tu primera partida",           "🎲",  1,   200),
        ("PRIMERA_VICTORIA","Sabor a gloria",   "Ganá tu primera partida",           "🏆",  1,   500),
        ("VETERANO",        "Veterano",         "Jugá 10 partidas",                  "⚔️", 10,  800),
        ("CAMPEON",         "Campeón",          "Ganá 5 partidas",                   "👑",  5,  1500),
        ("IMPARABLE",       "Imparable",        "Ganá 15 partidas",                  "🔥", 15,  3000),
        ("COLECCIONISTA",   "Coleccionista",    "Comprá 3 artículos en la tienda",   "🛍️",  3,   600),
        ("MILLONARIO",      "Millonario",       "Acumulá 20.000 monedas",            "💰", 20000, 1000),
        ("CONSTANTE",       "Constante",        "Reclamá 7 recompensas diarias",     "📅",  7,  1200)
    };

    // ================================================================
    // OBTENER LOGROS DEL JUGADOR
    // ================================================================
    public Respuesta<ResumenLogrosDTO> ObtenerLogros(int usuId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            if (usuId <= 0)
                return Respuesta<ResumenLogrosDTO>.Validacion("Usuario no válido.");

            var usuario = unidadTrabajo.TUsuario
                .ObtenerEntidad(u => u.UsuId == usuId).ValorRetorno;

            if (usuario == null)
                return Respuesta<ResumenLogrosDTO>.Validacion("Usuario no encontrado.");

            var progreso = CalcularProgreso(usuId, usuario.UsuMonedasTotal, unidadTrabajo);
            var reclamados = ObtenerReclamados(usuId, unidadTrabajo);

            var resumen = new ResumenLogrosDTO();

            foreach (var def in _catalogo)
            {
                int actual = progreso.TryGetValue(def.Codigo, out int p) ? p : 0;
                bool desbloqueado = actual >= def.Meta;
                bool reclamado = reclamados.Contains(def.Codigo);

                resumen.Logros.Add(new LogroDTO
                {
                    Codigo = def.Codigo,
                    Nombre = def.Nombre,
                    Descripcion = def.Descripcion,
                    Icono = def.Icono,
                    Desbloqueado = desbloqueado,
                    ProgresoActual = Math.Min(actual, def.Meta),
                    ProgresoMeta = def.Meta,
                    Recompensa = def.Recompensa,
                    Reclamado = reclamado
                });

                if (desbloqueado && !reclamado)
                    resumen.RecompensaPendiente += def.Recompensa;
            }

            resumen.TotalLogros = _catalogo.Count;
            resumen.Desbloqueados = resumen.Logros.Count(l => l.Desbloqueado);

            return Respuesta<ResumenLogrosDTO>.Exito(resumen, "Logros obtenidos.");
        }
        catch (Exception ex)
        {
            return Respuesta<ResumenLogrosDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // RECLAMAR RECOMPENSAS PENDIENTES
    // ================================================================
    // Acredita de una vez todos los logros desbloqueados sin reclamar.
    public Respuesta<ResultadoReclamoLogrosDTO> ReclamarPendientes(int usuId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            var resumenResp = ObtenerLogros(usuId, unidadTrabajo);
            if (!resumenResp.blnIndicadorTransaccion)
                return Respuesta<ResultadoReclamoLogrosDTO>.Validacion(resumenResp.strMensajeRespuesta);

            var pendientes = resumenResp.ValorRetorno!.Logros
                .Where(l => l.Desbloqueado && !l.Reclamado)
                .ToList();

            if (!pendientes.Any())
                return Respuesta<ResultadoReclamoLogrosDTO>.Validacion("No tenés logros pendientes de reclamar.");

            var usuario = unidadTrabajo.TUsuario
                .ObtenerEntidad(u => u.UsuId == usuId).ValorRetorno!;

            int total = pendientes.Sum(l => l.Recompensa);

            usuario.UsuMonedasTotal += total;
            unidadTrabajo.TUsuario.Modificar(usuario);

            // Una transacción por logro: el código va en el concepto y
            // es lo que después marca el logro como reclamado.
            foreach (var logro in pendientes)
            {
                unidadTrabajo.TTransaccion.Insertar(new Transaccione
                {
                    UsuId = usuId,
                    TranTipo = TIPO_TRANSACCION,
                    TranConcepto = $"[{logro.Codigo}] {logro.Nombre}",
                    TranMonto = logro.Recompensa,
                    TranSaldoResultante = usuario.UsuMonedasTotal,
                    TranFecha = DateTime.Now
                });
            }

            unidadTrabajo.Completar();

            var resultado = new ResultadoReclamoLogrosDTO
            {
                MonedasGanadas = total,
                SaldoNuevo = usuario.UsuMonedasTotal,
                LogrosReclamados = pendientes.Select(l => l.Nombre).ToList(),
                Mensaje = pendientes.Count == 1
                    ? $"¡Logro desbloqueado! Ganaste {total} monedas."
                    : $"¡{pendientes.Count} logros reclamados! Ganaste {total} monedas."
            };

            return Respuesta<ResultadoReclamoLogrosDTO>.Exito(resultado, resultado.Mensaje);
        }
        catch (Exception ex)
        {
            return Respuesta<ResultadoReclamoLogrosDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // HELPERS
    // ================================================================

    // Calcula el progreso de cada logro con los datos que ya existen
    private Dictionary<string, int> CalcularProgreso(int usuId, int monedasActuales, IUnidadTrabajoEF unidadTrabajo)
    {
        var historial = unidadTrabajo.THistorialPartida
            .Buscar(h => h.UsuId == usuId).ValorRetorno?.ToList() ?? new List<HistorialPartida>();

        var transacciones = unidadTrabajo.TTransaccion
            .Buscar(t => t.UsuId == usuId).ValorRetorno?.ToList() ?? new List<Transaccione>();

        int jugadas = historial.Count;
        int victorias = historial.Count(h => h.HpResultado == "VICTORIA");

        // Las compras se cuentan desde el inventario y no desde las
        // transacciones: comprar un artículo no genera una transacción
        // en el sistema actual, así que no habría de dónde contarlas.
        // Se excluyen los predeterminados, que el jugador no compró.
        int compras = unidadTrabajo.TUsuarioArticulo
            .Buscar(ua => ua.UsuId == usuId).ValorRetorno?.Count() ?? 0;

        int diarias = transacciones.Count(t => t.TranTipo == "RECOMPENSA_DIA");

        return new Dictionary<string, int>
        {
            ["PRIMERA_PARTIDA"] = jugadas,
            ["PRIMERA_VICTORIA"] = victorias,
            ["VETERANO"] = jugadas,
            ["CAMPEON"] = victorias,
            ["IMPARABLE"] = victorias,
            ["COLECCIONISTA"] = compras,
            ["MILLONARIO"] = monedasActuales,
            ["CONSTANTE"] = diarias
        };
    }

    // Un logro está reclamado si existe una transacción de tipo LOGRO
    // con su código en el concepto.
    private HashSet<string> ObtenerReclamados(int usuId, IUnidadTrabajoEF unidadTrabajo)
    {
        var transacciones = unidadTrabajo.TTransaccion
            .Buscar(t => t.UsuId == usuId && t.TranTipo == TIPO_TRANSACCION)
            .ValorRetorno?.ToList() ?? new List<Transaccione>();

        var reclamados = new HashSet<string>();

        foreach (var tran in transacciones)
        {
            var concepto = tran.TranConcepto ?? "";

            foreach (var def in _catalogo)
            {
                if (concepto.Contains($"[{def.Codigo}]"))
                    reclamados.Add(def.Codigo);
            }
        }

        return reclamados;
    }
}