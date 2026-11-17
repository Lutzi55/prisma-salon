using System.Net;
using System.Net.Sockets;

// Conexión saliente para el asistente (Gemini).
// Algunos hostings resuelven los servidores de Google con direcciones IPv6 que no tienen salida:
// la conexión queda "colgada" hasta que vence el tiempo. Acá se prueban primero las IPv4,
// cada dirección con un límite corto, y si una no responde se pasa a la siguiente.
public static class ConexionIPv4
{
    private static readonly TimeSpan PorDireccion = TimeSpan.FromSeconds(5);

    public static async ValueTask<Stream> ConectarAsync(SocketsHttpConnectionContext ctx, CancellationToken ct)
    {
        var host = ctx.DnsEndPoint.Host;
        var direcciones = (await Dns.GetHostAddressesAsync(host, ct))
            .OrderBy(ip => ip.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
            .ToList();

        Exception? ultimo = null;
        foreach (var ip in direcciones)
        {
            var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                using var corte = CancellationTokenSource.CreateLinkedTokenSource(ct);
                corte.CancelAfter(PorDireccion);
                await socket.ConnectAsync(new IPEndPoint(ip, ctx.DnsEndPoint.Port), corte.Token);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception ex)
            {
                socket.Dispose();
                if (ct.IsCancellationRequested) throw;
                ultimo = ex;
            }
        }
        throw new HttpRequestException($"No se pudo conectar con {host}", ultimo);
    }

    // Para el diagnóstico: qué direcciones resuelve y cuáles responden
    public static async Task<List<string>> ProbarAsync(string host, int puerto = 443)
    {
        var res = new List<string>();
        IPAddress[] dirs;
        try { dirs = await Dns.GetHostAddressesAsync(host); }
        catch (Exception ex) { res.Add("DNS falló: " + ex.Message); return res; }

        foreach (var ip in dirs.OrderBy(i => i.AddressFamily == AddressFamily.InterNetwork ? 0 : 1).Take(4))
        {
            var reloj = System.Diagnostics.Stopwatch.StartNew();
            using var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                using var corte = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                await socket.ConnectAsync(new IPEndPoint(ip, puerto), corte.Token);
                res.Add($"{ip}: conecta ({reloj.ElapsedMilliseconds} ms)");
            }
            catch (Exception ex)
            {
                res.Add($"{ip}: NO conecta ({ex.GetType().Name}, {reloj.ElapsedMilliseconds} ms)");
            }
        }
        return res;
    }
}
