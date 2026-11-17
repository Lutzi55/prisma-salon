using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using PlanificacionEventos.Models;

// Auditoría automática: intercepta CADA SaveChanges del contexto, sin depender de los controladores.
// 1) Antes de guardar (SavingChanges): mira qué entidades se crean, editan o eliminan y arma
//    una frase legible + el detalle de cambios campo por campo (antes → después).
// 2) Después de guardar (SavedChanges): ya con los Id generados, inserta las filas de Auditoria
//    con el usuario logueado (o "Sistema" si no hay sesión, por ejemplo el seed).
public class AuditoriaInterceptor : SaveChangesInterceptor
{
    private class Pendiente
    {
        public string Entidad = "", Accion = "", Descripcion = "", Detalle = "";
        public string? Id;
        public EntityEntry Entry = null!; // en las altas el Id se conoce después de guardar
    }

    private static readonly CultureInfo Es = Cultura();
    private static CultureInfo Cultura()
    {
        try { return new CultureInfo("es-AR"); } catch { return CultureInfo.InvariantCulture; }
    }

    private readonly IHttpContextAccessor _http;
    private List<Pendiente> _pendientes = new();

    public AuditoriaInterceptor(IHttpContextAccessor http) => _http = http;

    // ===================== Ciclo de guardado =====================

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData e, InterceptionResult<int> r, CancellationToken ct = default)
    {
        _pendientes = await CapturarAsync(e.Context, ct);
        return await base.SavingChangesAsync(e, r, ct);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData e, int result, CancellationToken ct = default)
    {
        var ctx = e.Context;
        if (ctx != null && _pendientes.Count > 0)
        {
            var lista = _pendientes;
            _pendientes = new();

            var u = _http.HttpContext?.User;
            var logueado = u?.Identity?.IsAuthenticated == true;
            var email = logueado ? (u!.FindFirstValue(ClaimTypes.Email) ?? u!.Identity!.Name ?? "Desconocido") : "Sistema";
            var uid = logueado ? u!.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            var ahora = Reloj.Ahora();

            foreach (var p in lista)
                ctx.Add(new Auditoria
                {
                    UsuarioId = uid,
                    UsuarioEmail = email,
                    Entidad = p.Entidad,
                    EntidadId = p.Id ?? ClaveDe(p.Entry),
                    Accion = p.Accion,
                    Descripcion = Recortar(p.Descripcion, 300),
                    Detalle = p.Detalle,
                    Fecha = ahora
                });

            await ctx.SaveChangesAsync(ct); // las auditorías se excluyen solas: no hay bucle
        }
        return await base.SavedChangesAsync(e, result, ct);
    }

    // Si el guardado falla (por ejemplo un borrado bloqueado por Restrict) no se registra nada
    public override Task SaveChangesFailedAsync(DbContextErrorEventData e, CancellationToken ct = default)
    {
        _pendientes = new();
        return base.SaveChangesFailedAsync(e, ct);
    }

    private static async Task<List<Pendiente>> CapturarAsync(DbContext? ctx, CancellationToken ct)
    {
        var res = new List<Pendiente>();
        if (ctx == null) return res;

        foreach (var en in ctx.ChangeTracker.Entries().ToList())
        {
            if (en.Entity is Auditoria) continue;
            if (en.Entity.GetType().Namespace?.StartsWith("Microsoft.AspNetCore.Identity") == true) continue;

            string? accion = en.State switch
            {
                EntityState.Added => "Crear",
                EntityState.Modified => "Editar",
                EntityState.Deleted => "Eliminar",
                _ => null
            };
            if (accion == null) continue;

            PropertyValues? antes = null;
            string detalle;
            if (en.State == EntityState.Modified)
            {
                // Se compara contra lo que hay en la base: sirve también para _db.Update(e)
                antes = await en.GetDatabaseValuesAsync(ct);
                if (antes == null) continue;
                detalle = await CambiosAsync(ctx, en, antes, ct);
                if (detalle == "") continue; // se guardó sin cambios reales: no se registra
            }
            else detalle = await ResumenAsync(ctx, en, ct);

            res.Add(new Pendiente
            {
                Entidad = en.Metadata.ClrType.Name,
                Accion = accion,
                Descripcion = await DescribirAsync(ctx, en, accion, antes, ct),
                Detalle = detalle,
                Id = en.State == EntityState.Added ? null : ClaveDe(en),
                Entry = en
            });
        }
        return res;
    }

    // ===================== Detalle de cambios =====================

    private static async Task<string> CambiosAsync(DbContext ctx, EntityEntry en, PropertyValues antes, CancellationToken ct)
    {
        var lineas = new List<string>();
        var tipo = en.Metadata.ClrType;

        foreach (var prop in en.Properties)
        {
            var meta = prop.Metadata;
            if (meta.IsPrimaryKey()) continue;

            var viejo = Normal(antes[meta.Name]);
            var nuevo = Normal(prop.CurrentValue);
            if (Equals(viejo, nuevo)) continue;

            var etiqueta = Etiqueta(tipo, meta.Name);

            if (meta.Name.StartsWith("Ruta"))
                lineas.Add($"{etiqueta}: {(viejo == null ? "archivo agregado" : nuevo == null ? "archivo quitado" : "archivo reemplazado")}");
            else if (meta.Name == "FirmaHash")
                lineas.Add($"{etiqueta}: registrada");
            else if (viejo is string sv && sv.Length > 80 || nuevo is string sn && sn.Length > 80)
                lineas.Add($"{etiqueta}: texto modificado");
            else
                lineas.Add($"{etiqueta}: {await ValorAsync(ctx, meta, viejo, ct)} → {await ValorAsync(ctx, meta, nuevo, ct)}");
        }
        return string.Join("; ", lineas);
    }

    // En altas y bajas: resumen de hasta 8 campos
    private static async Task<string> ResumenAsync(DbContext ctx, EntityEntry en, CancellationToken ct)
    {
        var lineas = new List<string>();
        var tipo = en.Metadata.ClrType;

        foreach (var prop in en.Properties)
        {
            if (lineas.Count == 8) break;
            var meta = prop.Metadata;
            if (meta.IsPrimaryKey() && !meta.IsForeignKey()) continue;

            var v = Normal(en.State == EntityState.Deleted ? prop.OriginalValue : prop.CurrentValue);
            if (v == null) continue;
            if (meta.Name == "FirmaHash") continue;
            if (v is string s && s.Length > 80) continue;

            var etiqueta = Etiqueta(tipo, meta.Name);
            lineas.Add(meta.Name.StartsWith("Ruta")
                ? $"{etiqueta}: archivo adjunto"
                : $"{etiqueta}: {await ValorAsync(ctx, meta, v, ct)}");
        }
        return string.Join("; ", lineas);
    }

    private static object? Normal(object? v) => v is string s && string.IsNullOrWhiteSpace(s) ? null : v;

    private static async Task<string> ValorAsync(DbContext ctx, IProperty meta, object? v, CancellationToken ct)
    {
        if (v == null) return "(vacío)";
        if (meta.IsForeignKey()) return await NombreDeClave(ctx, meta.Name, v, ct);

        return v switch
        {
            bool b => b ? "Sí" : "No",
            decimal d => EsMoneda(meta.Name) ? "$ " + d.ToString("N2", Es) : d.ToString("N2", Es),
            DateTime f => f.TimeOfDay == TimeSpan.Zero ? f.ToString("dd/MM/yyyy", Es) : f.ToString("dd/MM/yyyy HH:mm", Es),
            TimeSpan t => t.ToString(@"hh\:mm"),
            _ => v.ToString() ?? ""
        };
    }

    private static bool EsMoneda(string nombre) =>
        nombre.Contains("Precio") || nombre.Contains("Monto") || nombre.Contains("Valor") || nombre.Contains("PagoPor");

    // ===================== Nombres legibles (en vez de ids) =====================

    private const string Nuevo = "(registro nuevo)";

    private static async Task<string> NombreDeClave(DbContext c, string prop, object v, CancellationToken ct)
    {
        if (prop == "IdentityUserId")
        {
            var uid = v.ToString();
            return await c.Set<IdentityUser>().AsNoTracking().Where(u => u.Id == uid).Select(u => u.Email).FirstOrDefaultAsync(ct) ?? "(usuario)";
        }
        if (v is not int id) return v.ToString() ?? "";
        return prop switch
        {
            "IdCliente" => await NomCliente(c, id, ct),
            "IdPaquete" => await NomPaquete(c, id, ct),
            "IdCoordinador" => await NomCoordinador(c, id, ct),
            "IdShow" => await NomShow(c, id, ct),
            "IdEmpleado" => await NomEmpleado(c, id, ct),
            "IdOpcion" => await NomOpcion(c, id, ct),
            "IdCategoria" => await NomCategoria(c, id, ct),
            "IdContrato" => await DescContrato(c, id, null, ct),
            _ => id.ToString()
        };
    }

    private static async Task<string> NomCliente(DbContext c, int id, CancellationToken ct) =>
        await c.Set<Cliente>().AsNoTracking().Where(x => x.IdCliente == id).Select(x => x.RazonSocialApellido).FirstOrDefaultAsync(ct) ?? Nuevo;
    private static async Task<string> NomPaquete(DbContext c, int id, CancellationToken ct) =>
        await c.Set<PaqueteEvento>().AsNoTracking().Where(x => x.IdPaquete == id).Select(x => x.Nombre).FirstOrDefaultAsync(ct) ?? Nuevo;
    private static async Task<string> NomCoordinador(DbContext c, int id, CancellationToken ct) =>
        await c.Set<Coordinador>().AsNoTracking().Where(x => x.IdCoordinador == id).Select(x => x.NombreApellido).FirstOrDefaultAsync(ct) ?? Nuevo;
    private static async Task<string> NomShow(DbContext c, int id, CancellationToken ct) =>
        await c.Set<ShowStaff>().AsNoTracking().Where(x => x.IdShow == id).Select(x => x.NombreShow).FirstOrDefaultAsync(ct) ?? Nuevo;
    private static async Task<string> NomEmpleado(DbContext c, int id, CancellationToken ct) =>
        await c.Set<Empleado>().AsNoTracking().Where(x => x.IdEmpleado == id).Select(x => x.NombreApellido).FirstOrDefaultAsync(ct) ?? Nuevo;
    private static async Task<string> NomOpcion(DbContext c, int id, CancellationToken ct) =>
        await c.Set<Opcion>().AsNoTracking().Where(x => x.IdOpcion == id).Select(x => x.Nombre).FirstOrDefaultAsync(ct) ?? Nuevo;
    private static async Task<string> NomCategoria(DbContext c, int id, CancellationToken ct) =>
        await c.Set<CategoriaOpcion>().AsNoTracking().Where(x => x.IdCategoria == id).Select(x => x.Nombre).FirstOrDefaultAsync(ct) ?? Nuevo;
    private static async Task<string> ClienteDeContrato(DbContext c, int idContrato, CancellationToken ct) =>
        await c.Set<Contrato>().AsNoTracking().Where(x => x.IdContrato == idContrato).Select(x => x.Cliente.RazonSocialApellido).FirstOrDefaultAsync(ct) ?? Nuevo;

    // "el contrato de Familia Rodríguez (evento del 12/03/2027)"
    private static async Task<string> DescContrato(DbContext c, int id, Contrato? nav, CancellationToken ct)
    {
        if (nav != null && nav.IdCliente != 0)
            return $"el contrato de {await NomCliente(c, nav.IdCliente, ct)} (evento del {nav.FechaEvento.ToString("dd/MM/yyyy", Es)})";

        var d = await c.Set<Contrato>().AsNoTracking().Where(x => x.IdContrato == id)
            .Select(x => new { x.Cliente.RazonSocialApellido, x.FechaEvento }).FirstOrDefaultAsync(ct);
        return d == null ? "un contrato nuevo" : $"el contrato de {d.RazonSocialApellido} (evento del {d.FechaEvento.ToString("dd/MM/yyyy", Es)})";
    }

    // ===================== Frase legible: verbo + qué + a quién =====================

    private static async Task<string> DescribirAsync(DbContext ctx, EntityEntry en, string accion, PropertyValues? antes, CancellationToken ct)
    {
        var verbo = accion == "Crear" ? "Creó" : accion == "Editar" ? "Editó" : "Eliminó";
        var alta = accion == "Crear" ? "Dio de alta" : accion == "Editar" ? "Editó los datos de" : "Dio de baja";

        switch (en.Entity)
        {
            case Contrato c:
                {
                    var ev = await DescContrato(ctx, c.IdContrato, c, ct);
                    if (accion == "Crear" && c.PedidoPorCliente)
                        return $"Pidió el paquete {await NomPaquete(ctx, c.IdPaquete, ct)} personalizado para el evento del {c.FechaEvento.ToString("dd/MM/yyyy", Es)}";
                    if (accion == "Editar" && antes != null)
                    {
                        if (antes[nameof(Contrato.FirmadoEl)] == null && c.FirmadoEl != null)
                            return $"Firmó electrónicamente {ev}";
                        if (antes[nameof(Contrato.ContratoEnviado)] is false && c.ContratoEnviado)
                            return $"Envió al cliente {ev} para su firma";
                        if (!Equals(Normal(antes[nameof(Contrato.TextoContrato)]), Normal(c.TextoContrato)))
                            return $"Redactó el texto de {ev}";
                    }
                    return $"{verbo} {ev}";
                }

            case Pago p:
                {
                    var cli = await ClienteDeContrato(ctx, p.IdContrato, ct);
                    var monto = "$ " + p.Monto.ToString("N2", Es);
                    var vence = p.FechaVencimiento?.ToString("dd/MM/yyyy", Es);

                    if (accion == "Crear")
                        return p.Estado switch
                        {
                            "Solicitado" when p.Concepto?.StartsWith("Reserva:") == true =>
                                $"Eligió pagar {(p.EsSena ? "la seña" : "el total")} de {monto} por {p.MetodoPago.ToLower()} para reservar la fecha (contrato de {cli})",
                            "Solicitado" => $"Solicitó a {cli} {(p.EsSena ? "la seña" : "un pago")} de {monto} ({p.Concepto}) con vencimiento el {vence}",
                            "Pendiente" => $"Informó un pago de {monto} con comprobante (contrato de {cli})",
                            "Confirmado" => $"Registró un pago recibido de {monto} de {cli} ({p.MetodoPago})",
                            _ => $"Creó un pago de {monto} del contrato de {cli}"
                        };

                    if (accion == "Editar" && antes != null)
                    {
                        var estadoAntes = antes[nameof(Pago.Estado)] as string;
                        var turnoAntes = antes[nameof(Pago.TurnoPago)] as DateTime?;
                        if (p.TurnoPago != null && turnoAntes != p.TurnoPago && estadoAntes == p.Estado)
                            return $"Agendó un turno para pagar {monto} en {p.MetodoPago.ToLower()} el {p.TurnoPago.Value.ToString("dd/MM/yyyy HH:mm", Es)} (contrato de {cli})";
                        if (estadoAntes != "Confirmado" && p.Estado == "Confirmado" && p.TurnoPago != null)
                            return $"Registró el cobro en el salón de {monto} ({p.MetodoPago}) de {cli}";
                        var venceAntes = antes[nameof(Pago.FechaVencimiento)] as DateTime?;
                        if (p.Estado == "Reprogramado" && (estadoAntes != "Reprogramado" || venceAntes != p.FechaVencimiento))
                            return $"Reprogramó el vencimiento de un pago de {monto} de {cli} al {vence}";
                        if (estadoAntes != p.Estado)
                            return p.Estado switch
                            {
                                "Pendiente" when estadoAntes == "Rechazado" => $"Reenvió el comprobante de un pago de {monto} de {cli}",
                                "Pendiente" => $"Envió el comprobante de un pago de {monto} de {cli}",
                                "Confirmado" => $"Confirmó un pago de {monto} de {cli}",
                                "Rechazado" => $"Rechazó el comprobante de un pago de {monto} de {cli}",
                                "Cancelado" => $"Canceló la solicitud de un pago de {monto} de {cli}",
                                "Reembolsado" => $"Registró el reembolso de un pago de {monto} de {cli}",
                                _ => $"Editó un pago de {monto} del contrato de {cli}"
                            };
                    }
                    return $"{verbo} un pago de {monto} ({p.MetodoPago}) del contrato de {cli}";
                }

            case ContratoShow cs:
                {
                    var show = await NomShow(ctx, cs.IdShow, ct);
                    var con = await DescContrato(ctx, cs.IdContrato, cs.Contrato, ct);
                    return accion switch
                    {
                        "Crear" => $"Asignó el show {show} a {con}",
                        "Eliminar" => $"Quitó el show {show} de {con}",
                        _ => $"Editó el show {show} de {con}"
                    };
                }

            case ContratoEmpleado ce:
                {
                    var emp = await NomEmpleado(ctx, ce.IdEmpleado, ct);
                    var con = await DescContrato(ctx, ce.IdContrato, ce.Contrato, ct);
                    return accion switch
                    {
                        "Crear" => $"Asignó a {emp} al personal de {con}",
                        "Eliminar" => $"Quitó a {emp} del personal de {con}",
                        _ => $"Cambió la hora de citación de {emp} en {con}"
                    };
                }

            case ContratoOpcion co:
                {
                    var op = await NomOpcion(ctx, co.IdOpcion, ct);
                    var con = await DescContrato(ctx, co.IdContrato, co.Contrato, ct);
                    return accion switch
                    {
                        "Crear" => $"Agregó la opción {op} a {con}",
                        "Eliminar" => $"Quitó la opción {op} de {con}",
                        _ => $"Editó la opción {op} de {con}"
                    };
                }

            case Resena r:
                {
                    var cli = await NomCliente(ctx, r.IdCliente, ct);
                    var con = await DescContrato(ctx, r.IdContrato, r.Contrato, ct);
                    if (accion == "Crear") return $"Publicó una reseña de {r.Puntaje} estrellas sobre {con}";
                    if (accion == "Eliminar") return $"Eliminó la reseña de {cli}";
                    if (antes != null && !Equals(Normal(antes[nameof(Resena.Respuesta)]), Normal(r.Respuesta)))
                        return $"Respondió la reseña de {cli}";
                    if (antes != null && antes[nameof(Resena.Visible)] is bool vis && vis != r.Visible)
                        return r.Visible ? $"Volvió a mostrar en el sitio la reseña de {cli}" : $"Ocultó del sitio la reseña de {cli}";
                    return $"Editó la reseña de {cli} ({r.Puntaje} estrellas)";
                }

            case Cliente x: return $"{alta} el cliente {x.RazonSocialApellido}";
            case Coordinador x: return $"{alta} el coordinador {x.NombreApellido}";
            case Empleado x: return $"{alta} el empleado {x.NombreApellido} ({x.Puesto})";
            case PaqueteEvento x: return $"{verbo} el paquete {x.Nombre}";
            case ShowStaff x: return $"{verbo} el show {x.NombreShow}";
            case CategoriaOpcion x: return $"{verbo} la categoría de opciones {x.Nombre}";
            case Opcion x:
                {
                    var cat = x.IdCategoria.HasValue ? await NomCategoria(ctx, x.IdCategoria.Value, ct) : "";
                    return $"{verbo} la opción {x.Nombre}" + (cat == "" ? "" : $" ({cat})");
                }

            default:
                return $"{verbo} un registro de {en.Metadata.ClrType.Name}";
        }
    }

    // ===================== Auxiliares =====================

    // Clave primaria; las compuestas se guardan como "3-7"
    private static string ClaveDe(EntityEntry en)
    {
        var pk = en.Metadata.FindPrimaryKey();
        if (pk == null) return "";
        return string.Join("-", pk.Properties.Select(p => en.Property(p.Name).CurrentValue?.ToString() ?? ""));
    }

    private static string Recortar(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    private static readonly Dictionary<string, string> Etiquetas = new()
    {
        ["IdCliente"] = "Cliente", ["IdPaquete"] = "Paquete", ["IdCoordinador"] = "Coordinador",
        ["IdCategoria"] = "Categoría", ["IdShow"] = "Show", ["IdEmpleado"] = "Empleado",
        ["IdOpcion"] = "Opción", ["IdContrato"] = "Contrato",
        ["Telefono"] = "Teléfono", ["Dni"] = "DNI", ["RazonSocialApellido"] = "Razón social / Apellido",
        ["DomicilioLegal"] = "Domicilio legal", ["NombreApellido"] = "Nombre y apellido", ["AnioInicio"] = "Año de inicio",
        ["Descripcion"] = "Descripción", ["DescripcionDetallada"] = "Descripción detallada",
        ["CapacidadInvitados"] = "Capacidad de invitados", ["DuracionHoras"] = "Duración (horas)",
        ["HorarioReferencia"] = "Horario de referencia", ["Incluye"] = "Qué incluye",
        ["NombreShow"] = "Artista", ["TipoShow"] = "Tipo de show", ["HorarioShow"] = "Horario del show",
        ["PrecioContratacion"] = "Precio de contratación", ["PagoPorEvento"] = "Pago por evento",
        ["HoraCitacion"] = "Hora de citación", ["FechaEvento"] = "Fecha del evento",
        ["ValorRealAcordado"] = "Valor acordado", ["FormaPago"] = "Forma de pago",
        ["TextoContrato"] = "Texto del contrato", ["ContratoEnviado"] = "Enviado al cliente",
        ["MetodoPago"] = "Método de pago", ["FechaPago"] = "Fecha de pago", ["FechaVencimiento"] = "Vencimiento",
        ["PrecioAdicional"] = "Precio adicional", ["PrecioAcordado"] = "Precio acordado",
        ["EsMultiple"] = "Permite varias opciones", ["RutaContratoPdf"] = "Contrato PDF",
        ["RutaComprobanteImg"] = "Comprobante", ["Opcion"] = "Opción"
    };

    // Etiqueta visible: primero [Display(Name)], después el diccionario, y si no "CamelCase" → "Camel case"
    private static string Etiqueta(Type tipo, string prop)
    {
        var display = tipo.GetProperty(prop)?.GetCustomAttribute<DisplayAttribute>()?.GetName();
        if (!string.IsNullOrWhiteSpace(display)) return display;
        if (Etiquetas.TryGetValue(prop, out var e)) return e;

        var t = Regex.Replace(prop, "(?<!^)([A-Z])", " $1").ToLower();
        return char.ToUpper(t[0]) + t[1..];
    }
}
