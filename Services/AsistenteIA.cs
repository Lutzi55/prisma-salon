using System.Net;
using System.Text;
using System.Text.Json;

// Error con un mensaje apto para mostrarle al usuario
public class AsistenteException : Exception
{
    public AsistenteException(string mensaje) : base(mensaje) { }
}

// Cliente de la API de Google Gemini (REST con HttpClient).
// La clave se lee de la configuración "Gemini:ApiKey":
//   - en desarrollo: User Secrets (clic derecho en el proyecto > Administrar secretos de usuario)
//   - en el hosting: appsettings.Production.json (excluido de GitHub) o variable de entorno Gemini__ApiKey
// Nunca se escribe la clave en el código ni en appsettings.json.
public class AsistenteIA
{
    private const string Url = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";

    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;
    private readonly ILogger<AsistenteIA> _log;

    public AsistenteIA(HttpClient http, IConfiguration cfg, ILogger<AsistenteIA> log)
    {
        _http = http; _cfg = cfg; _log = log;
    }

    private string? Clave => _cfg["Gemini:ApiKey"];
    public bool Configurado => !string.IsNullOrWhiteSpace(Clave);

    // ---------- Qué modelo usar ----------
    // Google da de baja modelos con el tiempo (por ejemplo, los 2.5 ya no están disponibles para claves nuevas).
    // Por eso se le pregunta a la API qué modelos "flash" hay para esta clave y se usan esos,
    // del más rápido al más completo. La lista se guarda 6 horas para no consultarla en cada mensaje.
    private static List<string>? _disponibles;
    private static DateTime _disponiblesHasta;
    private static readonly string[] Descartar = { "image", "tts", "audio", "live", "embedding", "exp", "robotics", "computer" };

    public async Task<List<string>> DisponiblesAsync(CancellationToken ct = default)
    {
        if (_disponibles != null && DateTime.UtcNow < _disponiblesHasta) return _disponibles;
        var lista = new List<string>();
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://generativelanguage.googleapis.com/v1beta/models?pageSize=1000");
            req.Headers.Add("x-goog-api-key", Clave);
            using var corte = CancellationTokenSource.CreateLinkedTokenSource(ct);
            corte.CancelAfter(TimeSpan.FromSeconds(10));
            using var resp = await _http.SendAsync(req, corte.Token);
            if (resp.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(corte.Token));
                foreach (var m in doc.RootElement.GetProperty("models").EnumerateArray())
                {
                    var nombre = (m.GetProperty("name").GetString() ?? "").Replace("models/", "");
                    var generaTexto = m.TryGetProperty("supportedGenerationMethods", out var met)
                        && met.EnumerateArray().Any(x => x.GetString() == "generateContent");
                    if (generaTexto && nombre.StartsWith("gemini-") && nombre.Contains("flash")
                        && !Descartar.Any(d => nombre.Contains(d)))
                        lista.Add(nombre);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _log.LogWarning(ex, "No se pudo obtener la lista de modelos de Gemini");
        }

        // Orden: estables antes que "preview", "flash-lite" (más rápido) antes que "flash", versión más nueva primero
        lista = lista.Distinct()
            .OrderBy(n => n.Contains("preview") ? 1 : 0)
            .ThenBy(n => n.Contains("flash-lite") ? 0 : 1)
            .ThenByDescending(Version)
            .ToList();
        if (lista.Count > 0) { _disponibles = lista; _disponiblesHasta = DateTime.UtcNow.AddHours(6); }
        return lista;
    }

    private static double Version(string nombre)
    {
        var m = System.Text.RegularExpressions.Regex.Match(nombre, @"gemini-(\d+(?:\.\d+)?)");
        return m.Success ? double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
    }

    // Modelo configurado (si hay) y después los disponibles para la clave
    private async Task<List<string>> ModelosAsync(CancellationToken ct)
    {
        var lista = new List<string>();
        if (!string.IsNullOrWhiteSpace(_cfg["Gemini:Modelo"])) lista.Add(_cfg["Gemini:Modelo"]!.Trim());
        lista.AddRange(await DisponiblesAsync(ct));
        if (lista.Count == 0) lista.Add("gemini-flash-latest");
        return lista.Distinct().Take(4).ToList();
    }

    // Cuánto se espera a cada modelo antes de probar el siguiente
    private static readonly TimeSpan EsperaPorModelo = TimeSpan.FromSeconds(25);

    private static string Cuerpo(string modelo, string sistema, IReadOnlyList<(string Rol, string Texto)> mensajes, bool limitarRazonamiento = true)
    {
        // Los modelos nuevos "piensan" antes de contestar y eso tarda; para un chat de atención no hace falta
        object config = !limitarRazonamiento
            ? new { temperature = 0.4, maxOutputTokens = 800 }
            : modelo.StartsWith("gemini-2.")
                ? new { temperature = 0.4, maxOutputTokens = 800, thinkingConfig = new { thinkingBudget = 0 } }
                : new { temperature = 0.4, maxOutputTokens = 800, thinkingConfig = new { thinkingLevel = "low" } };
        return JsonSerializer.Serialize(new
        {
            systemInstruction = new { parts = new[] { new { text = sistema } } },
            contents = mensajes.Select(m => new { role = m.Rol, parts = new[] { new { text = m.Texto } } }),
            generationConfig = config
        });
    }

    // mensajes: conversación en orden, con rol "user" (usuario) o "model" (asistente)
    public async Task<string> ResponderAsync(string sistema, IReadOnlyList<(string Rol, string Texto)> mensajes, CancellationToken ct = default)
    {
        var tardo = false;
        foreach (var modelo in await ModelosAsync(ct))
        {
            var limitar = true;
            for (var intento = 0; intento < 2; intento++)
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, string.Format(Url, modelo));
                req.Headers.Add("x-goog-api-key", Clave);
                req.Content = new StringContent(Cuerpo(modelo, sistema, mensajes, limitar), Encoding.UTF8, "application/json");

                using var espera = CancellationTokenSource.CreateLinkedTokenSource(ct);
                espera.CancelAfter(EsperaPorModelo);

                HttpResponseMessage resp;
                try
                {
                    resp = await _http.SendAsync(req, espera.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    _log.LogWarning("Gemini ({Modelo}) no respondió a tiempo", modelo);
                    tardo = true;
                    break; // siguiente modelo
                }
                catch (HttpRequestException ex)
                {
                    _log.LogError(ex, "No se pudo conectar con Gemini");
                    throw new AsistenteException("No hay conexión con el asistente en este momento.");
                }

                using (resp)
                {
                    var json = await resp.Content.ReadAsStringAsync(ct);

                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        _log.LogWarning("El modelo {Modelo} no está disponible; se prueba el siguiente", modelo);
                        break;
                    }
                    // Si el modelo no acepta el ajuste de razonamiento, se reintenta sin él
                    if (resp.StatusCode == HttpStatusCode.BadRequest && limitar && json.Contains("thinking", StringComparison.OrdinalIgnoreCase))
                    {
                        limitar = false;
                        continue;
                    }
                    if ((int)resp.StatusCode == 429)
                        throw new AsistenteException("El asistente recibió muchas consultas. Probá de nuevo en un minuto.");
                    if (!resp.IsSuccessStatusCode)
                    {
                        _log.LogError("Gemini respondió {Codigo}: {Cuerpo}", (int)resp.StatusCode, json);
                        throw new AsistenteException("El asistente no está disponible en este momento.");
                    }

                    var texto = Extraer(json);
                    return string.IsNullOrWhiteSpace(texto)
                        ? "No tengo una respuesta para eso. Podés escribirnos a consultas@prismasalon.com."
                        : texto.Trim();
                }
            }
        }
        throw new AsistenteException(tardo
            ? "El asistente tardó demasiado en responder. Probá de nuevo."
            : "El asistente no está disponible en este momento.");
    }

    public async Task<string> ModeloPrincipalAsync() => (await DisponiblesAsync()).FirstOrDefault() ?? "gemini-flash-latest";

    // Diagnóstico: una consulta real a Gemini, midiendo cuánto tarda y qué contesta
    public async Task<object> ProbarGenerarAsync(string modelo, string sistema, string pregunta)
    {
        var cuerpo = Cuerpo(modelo, sistema, new List<(string, string)> { ("user", pregunta) });
        var reloj = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, string.Format(Url, modelo));
            req.Headers.Add("x-goog-api-key", Clave);
            req.Content = new StringContent(cuerpo, Encoding.UTF8, "application/json");
            using var corte = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var resp = await _http.SendAsync(req, corte.Token);
            var json = await resp.Content.ReadAsStringAsync();
            var texto = resp.IsSuccessStatusCode ? Extraer(json) : json;
            return new
            {
                modelo, bytesEnviados = Encoding.UTF8.GetByteCount(cuerpo), codigo = (int)resp.StatusCode,
                segundos = Math.Round(reloj.Elapsed.TotalSeconds, 1),
                respuesta = texto.Length > 160 ? texto[..160] : texto
            };
        }
        catch (Exception ex)
        {
            return new
            {
                modelo, bytesEnviados = Encoding.UTF8.GetByteCount(cuerpo), segundos = Math.Round(reloj.Elapsed.TotalSeconds, 1),
                error = ex.GetType().Name + ": " + ex.Message
            };
        }
    }

    // Diagnóstico para el administrador: ¿el servidor llega a Google y la clave sirve?
    public async Task<object> ProbarAsync()
    {
        if (!Configurado) return new { configurado = false, mensaje = "Falta la clave Gemini:ApiKey en la configuración." };
        var red = await ConexionIPv4.ProbarAsync("generativelanguage.googleapis.com");
        var reloj = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://generativelanguage.googleapis.com/v1beta/models?pageSize=1");
            req.Headers.Add("x-goog-api-key", Clave);
            using var corte = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            using var resp = await _http.SendAsync(req, corte.Token);
            var cuerpo = await resp.Content.ReadAsStringAsync();
            return new
            {
                configurado = true, conecta = true, codigo = (int)resp.StatusCode, red,
                segundos = Math.Round(reloj.Elapsed.TotalSeconds, 1),
                mensaje = resp.IsSuccessStatusCode ? "El servidor llega a Gemini y la clave funciona."
                        : "Google respondió con error: " + (cuerpo.Length > 300 ? cuerpo[..300] : cuerpo)
            };
        }
        catch (Exception ex)
        {
            return new
            {
                configurado = true, conecta = false, red, segundos = Math.Round(reloj.Elapsed.TotalSeconds, 1),
                mensaje = "El servidor no pudo conectarse con Gemini.", error = ex.GetType().Name + ": " + ex.Message
            };
        }
    }

    // Toma el texto de candidates[0].content.parts[*].text (ignora las partes de "pensamiento")
    private static string Extraer(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("candidates", out var cands) || cands.GetArrayLength() == 0) return "";
        if (!cands[0].TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts)) return "";

        var sb = new StringBuilder();
        foreach (var p in parts.EnumerateArray())
        {
            if (p.TryGetProperty("thought", out var th) && th.ValueKind == JsonValueKind.True) continue;
            if (p.TryGetProperty("text", out var t)) sb.Append(t.GetString());
        }
        return sb.ToString();
    }
}
