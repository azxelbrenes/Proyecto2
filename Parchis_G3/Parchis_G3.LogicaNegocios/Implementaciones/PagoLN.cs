using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Parchis_G3.Dominio.DTO;
using Parchis_G3.Dominio.Entidades;
using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Dominio.InterfacesLN;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.LogicaNegocios.Implementaciones;

public class PagoLN : IPagoLN
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    // URL base de PayPal — sandbox para pruebas, live para producción
    private readonly string _baseUrl;
    private readonly string _clientId;
    private readonly string _secret;

    // ── Catálogo de paquetes ─────────────────────────────────────
    // Definidos en código (no en BD) porque son datos fijos del
    // negocio que rara vez cambian y no necesitan administración.
    // Si en el futuro se quieren editar sin recompilar, se pasan
    // a una tabla PaquetesMonedas.
    private static readonly List<PaqueteMonedasDTO> _paquetes = new()
    {
        new PaqueteMonedasDTO
        {
            PaqueteId = 1, Nombre = "Paquete Pequeño",
            Monedas = 10000,  PrecioUSD = 2.99m,  PrecioCRC = 1500,
            Emoji = "💰", EsPopular = false
        },
        new PaqueteMonedasDTO
        {
            PaqueteId = 2, Nombre = "Paquete Mediano",
            Monedas = 25000,  PrecioUSD = 5.99m,  PrecioCRC = 3000,
            Emoji = "💵", EsPopular = true    // El más vendido
        },
        new PaqueteMonedasDTO
        {
            PaqueteId = 3, Nombre = "Paquete Grande",
            Monedas = 60000,  PrecioUSD = 11.99m, PrecioCRC = 6000,
            Emoji = "💎", EsPopular = false
        },
        new PaqueteMonedasDTO
        {
            PaqueteId = 4, Nombre = "Paquete Premium",
            Monedas = 150000, PrecioUSD = 23.99m, PrecioCRC = 12000,
            Emoji = "👑", EsPopular = false
        }
    };

    public PagoLN(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;

        _clientId = configuration["PayPal:ClientId"]
            ?? throw new InvalidOperationException("PayPal:ClientId no configurado en appsettings.");
        _secret = configuration["PayPal:Secret"]
            ?? throw new InvalidOperationException("PayPal:Secret no configurado en appsettings.");

        // Sandbox = pruebas con dinero falso. Live = dinero real.
        bool esSandbox = configuration.GetValue<bool>("PayPal:Sandbox", true);
        _baseUrl = esSandbox
            ? "https://api-m.sandbox.paypal.com"
            : "https://api-m.paypal.com";
    }

    // ================================================================
    // OBTENER PAQUETES
    // ================================================================
    public Respuesta<List<PaqueteMonedasDTO>> ObtenerPaquetes()
    {
        return Respuesta<List<PaqueteMonedasDTO>>.Exito(_paquetes, "Paquetes obtenidos.");
    }

    // ================================================================
    // PASO 1 — CREAR ORDEN EN PAYPAL
    // ================================================================
    public async Task<Respuesta<OrdenCreadaDTO>> CrearOrden(int usuId, int paqueteId)
    {
        try
        {
            // Buscamos el paquete en NUESTRO catálogo — el cliente
            // solo manda el ID, nunca el precio. Así nadie puede
            // manipular cuánto va a pagar.
            var paquete = _paquetes.FirstOrDefault(p => p.PaqueteId == paqueteId);
            if (paquete == null)
                return Respuesta<OrdenCreadaDTO>.Validacion("El paquete seleccionado no existe.");

            // Obtenemos el token de acceso de PayPal
            string? token = await ObtenerTokenPayPal();
            if (token == null)
                return Respuesta<OrdenCreadaDTO>.Error("No se pudo conectar con PayPal. Intentá de nuevo en unos momentos.");

            // Armamos el cuerpo de la orden según la API de PayPal
            var body = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        reference_id = $"paquete-{paqueteId}-usu-{usuId}",
                        description  = $"{paquete.Nombre} - {paquete.Monedas:N0} monedas Parchís Online",
                        amount = new
                        {
                            currency_code = "USD",
                            value         = paquete.PrecioUSD.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                        }
                    }
                },
                application_context = new
                {
                    brand_name = "Parchís Online",
                    landing_page = "NO_PREFERENCE",
                    user_action = "PAY_NOW",
                    return_url = "http://localhost:8100/tienda-monedas?pago=exitoso",
                    cancel_url = "http://localhost:8100/tienda-monedas?pago=cancelado"
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/checkout/orders");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            var contenido = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Traducimos el error técnico de PayPal a español
                return Respuesta<OrdenCreadaDTO>.Validacion(TraducirErrorPayPal(contenido));
            }

            using var doc = JsonDocument.Parse(contenido);
            var root = doc.RootElement;

            string ordenId = root.GetProperty("id").GetString() ?? "";

            // Buscamos el link de aprobación entre los que devuelve PayPal
            string urlAprobacion = "";
            if (root.TryGetProperty("links", out var links))
            {
                foreach (var link in links.EnumerateArray())
                {
                    if (link.GetProperty("rel").GetString() == "approve")
                    {
                        urlAprobacion = link.GetProperty("href").GetString() ?? "";
                        break;
                    }
                }
            }

            var resultado = new OrdenCreadaDTO
            {
                OrdenId = ordenId,
                UrlAprobacion = urlAprobacion,
                PaqueteId = paqueteId,
                Monedas = paquete.Monedas,
                Precio = paquete.PrecioUSD
            };

            return Respuesta<OrdenCreadaDTO>.Exito(resultado, "Orden creada. Redirigí al usuario a PayPal.");
        }
        catch (Exception ex)
        {
            return Respuesta<OrdenCreadaDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // PASO 2 — CAPTURAR EL PAGO Y ACREDITAR MONEDAS
    // ================================================================
    public async Task<Respuesta<ResultadoPagoDTO>> CapturarPago(int usuId, string ordenId, int paqueteId, IUnidadTrabajoEF unidadTrabajo)
    {
        try
        {
            var paquete = _paquetes.FirstOrDefault(p => p.PaqueteId == paqueteId);
            if (paquete == null)
                return Respuesta<ResultadoPagoDTO>.Validacion("El paquete no existe.");

            // ── Protección contra doble acreditación ─────────────
            // Si alguien llama dos veces a este endpoint con la misma
            // orden, no le damos monedas dos veces. Buscamos si ya
            // existe una transacción con esa referencia externa.
            var yaExiste = unidadTrabajo.TTransaccion
                .Buscar(t => t.TranReferenciaExt == ordenId)
                .ValorRetorno?.Any() ?? false;

            if (yaExiste)
                return Respuesta<ResultadoPagoDTO>.Validacion(
                    "Este pago ya fue procesado. Revisá tu saldo, las monedas deberían estar acreditadas."
                );

            string? token = await ObtenerTokenPayPal();
            if (token == null)
                return Respuesta<ResultadoPagoDTO>.Error("No se pudo conectar con PayPal. Intentá de nuevo en unos momentos.");

            // Le PREGUNTAMOS a PayPal si el pago realmente se completó.
            // Nunca confiamos en que el cliente diga "ya pagué".
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/checkout/orders/{ordenId}/capture");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var contenido = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Acá es donde antes se devolvía el JSON crudo de PayPal
                // al usuario. Ahora lo traducimos a un mensaje claro.
                return Respuesta<ResultadoPagoDTO>.Validacion(TraducirErrorPayPal(contenido));
            }

            using var doc = JsonDocument.Parse(contenido);
            string estado = doc.RootElement.GetProperty("status").GetString() ?? "";

            // PayPal devuelve "COMPLETED" solo si el dinero se cobró
            if (estado != "COMPLETED")
            {
                return Respuesta<ResultadoPagoDTO>.Validacion(
                    "El pago todavía no está confirmado por PayPal. " +
                    "Si ya pagaste, esperá unos segundos e intentá de nuevo."
                );
            }

            // ── Recién ahora acreditamos las monedas ─────────────
            var usuarioResp = unidadTrabajo.TUsuario.ObtenerEntidad(u => u.UsuId == usuId);
            if (!usuarioResp.blnIndicadorTransaccion)
                return Respuesta<ResultadoPagoDTO>.Validacion("Usuario no encontrado.");

            var usuario = usuarioResp.ValorRetorno!;
            usuario.UsuMonedasTotal += paquete.Monedas;

            // IMPORTANTE: NO tocamos UsuMonedasGanadasPartida.
            // Ese campo alimenta el ranking y solo debe subir cuando
            // se gana jugando — así nadie compra el primer lugar.
            unidadTrabajo.TUsuario.Modificar(usuario);

            // Registramos la compra con la referencia de PayPal.
            // Nunca guardamos datos de la tarjeta, solo el ID.
            unidadTrabajo.TTransaccion.Insertar(new Transaccione
            {
                UsuId = usuId,
                ParId = null,   // no es de partida
                TranTipo = "COMPRA_MONEDAS",
                TranConcepto = $"{paquete.Nombre} - {paquete.Monedas:N0} monedas (${paquete.PrecioUSD})",
                TranMonto = paquete.Monedas,
                TranSaldoResultante = usuario.UsuMonedasTotal,
                TranReferenciaExt = ordenId,   // ID de PayPal para auditoría
                TranFecha = DateTime.Now
            });

            unidadTrabajo.Completar();

            var resultado = new ResultadoPagoDTO
            {
                Exitoso = true,
                OrdenId = ordenId,
                MonedasAcreditadas = paquete.Monedas,
                SaldoNuevo = usuario.UsuMonedasTotal,
                Mensaje = $"¡Compra exitosa! Recibiste {paquete.Monedas:N0} monedas."
            };

            return Respuesta<ResultadoPagoDTO>.Exito(resultado, resultado.Mensaje);
        }
        catch (Exception ex)
        {
            return Respuesta<ResultadoPagoDTO>.Error(ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ================================================================
    // HELPER — Obtener token de acceso de PayPal
    // ================================================================
    // PayPal usa OAuth2: primero pedimos un token con nuestras
    // credenciales, y ese token se usa para las llamadas siguientes.
    private async Task<string?> ObtenerTokenPayPal()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/oauth2/token");

            // Las credenciales van en Basic Auth codificadas en Base64
            var credenciales = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_clientId}:{_secret}")
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credenciales);

            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var contenido = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(contenido);

            return doc.RootElement.GetProperty("access_token").GetString();
        }
        catch
        {
            return null;
        }
    }

    // ================================================================
    // TRADUCIR ERRORES DE PAYPAL
    // ================================================================
    // PayPal devuelve errores en JSON con códigos técnicos en inglés:
    //
    //   {"name":"UNPROCESSABLE_ENTITY","details":[{"issue":
    //   "ORDER_NOT_APPROVED","description":"Payer has not yet
    //   approved the Order for payment..."}],"debug_id":"f448045..."}
    //
    // Antes ese JSON se mostraba tal cual al usuario, lo cual es
    // ilegible y poco profesional. Este método extrae el código
    // específico y lo traduce a un mensaje claro en español.
    private string TraducirErrorPayPal(string contenidoJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(contenidoJson);
            var root = doc.RootElement;

            // El código específico viene dentro de "details"
            string issue = "";

            if (root.TryGetProperty("details", out var details) &&
                details.ValueKind == JsonValueKind.Array &&
                details.GetArrayLength() > 0)
            {
                var primerDetalle = details[0];
                if (primerDetalle.TryGetProperty("issue", out var issueProp))
                    issue = issueProp.GetString() ?? "";
            }

            // Si no hay detalles, usamos el nombre general del error
            if (string.IsNullOrEmpty(issue) &&
                root.TryGetProperty("name", out var nameProp))
            {
                issue = nameProp.GetString() ?? "";
            }

            // Traducimos según el código
            return issue switch
            {
                "ORDER_NOT_APPROVED" =>
                    "Todavía no completaste el pago en PayPal. " +
                    "Abrí la ventana de PayPal, aprobá la compra y volvé a intentar.",

                "ORDER_ALREADY_CAPTURED" =>
                    "Este pago ya fue procesado anteriormente. " +
                    "Revisá tu saldo, las monedas deberían estar acreditadas.",

                "INSTRUMENT_DECLINED" =>
                    "PayPal rechazó el método de pago. " +
                    "Probá con otra tarjeta o cuenta.",

                "PAYER_ACTION_REQUIRED" =>
                    "PayPal necesita que confirmes algo más. " +
                    "Volvé a abrir PayPal y completá los pasos que te pida.",

                "RESOURCE_NOT_FOUND" =>
                    "La orden de pago expiró o ya no existe. " +
                    "Cancelá esta compra y empezá de nuevo.",

                "PAYMENT_ALREADY_DONE" =>
                    "Este pago ya se completó anteriormente.",

                "PAYEE_ACCOUNT_RESTRICTED" =>
                    "La cuenta de PayPal tiene restricciones. " +
                    "Contactá al soporte del juego.",

                "UNPROCESSABLE_ENTITY" =>
                    "No se pudo procesar el pago. " +
                    "Verificá que hayas completado la compra en PayPal.",

                "INVALID_REQUEST" =>
                    "Los datos del pago no son válidos. " +
                    "Cancelá esta compra e intentá de nuevo.",

                // Cualquier otro error que no tengamos mapeado
                _ => "No se pudo verificar el pago con PayPal. " +
                     "Asegurate de haber completado la compra e intentá de nuevo."
            };
        }
        catch
        {
            // Si el JSON viene corrupto o en otro formato,
            // devolvemos un mensaje genérico en vez de crashear
            return "No se pudo verificar el pago con PayPal. Intentá de nuevo en unos momentos.";
        }
    }
}