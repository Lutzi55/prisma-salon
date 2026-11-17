using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;
using PlanificacionEventos.Models;

namespace PlanificacionEventos.Controllers;

// Calendario de disponibilidad. El salón hace un evento por día:
// - visitantes y clientes ven qué días están libres o reservados (y los clientes, sus propios eventos);
// - el administrador ve el detalle de cada evento y la agenda del personal para avisarles.
[AllowAnonymous]
public class CalendarioController : Controller
{
    private static readonly CultureInfo Es = new("es-AR");
    private readonly AppDbContext _db;
    public CalendarioController(AppDbContext db) => _db = db;

    private static (int anio, int mes) Normalizar(int? anio, int? mes, DateTime hoy)
    {
        var a = anio ?? hoy.Year;
        var m = mes ?? hoy.Month;
        if (m < 1) { m = 12; a--; }
        if (m > 12) { m = 1; a++; }
        return (Math.Clamp(a, 2000, 2100), m);
    }

    // "21:00 a 03:00" según el horario de referencia y la duración del paquete
    private static string Horario(PaqueteEvento p) =>
        p.DuracionHoras > 0
            ? $"{p.HorarioReferencia:hh\\:mm} a {p.HorarioReferencia.Add(TimeSpan.FromHours(p.DuracionHoras)):hh\\:mm} hs"
            : $"desde las {p.HorarioReferencia:hh\\:mm} hs";

    public async Task<IActionResult> Index(int? anio, int? mes)
    {
        var hoy = Reloj.Ahora().Date;
        var (a, m) = Normalizar(anio, mes, hoy);
        var primero = new DateTime(a, m, 1);

        // La grilla arranca el lunes anterior al día 1 y muestra 6 semanas
        var inicio = primero.AddDays(-(((int)primero.DayOfWeek + 6) % 7));
        var fin = inicio.AddDays(42);

        var esAdmin = User.IsInRole("Administrador");
        var esCliente = User.IsInRole("Cliente");
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var contratos = await _db.Contratos.AsNoTracking()
            .Include(c => c.Cliente).Include(c => c.Paquete).Include(c => c.Pagos)
            .Include(c => c.ContratoEmpleados).ThenInclude(x => x.Empleado)
            .Include(c => c.ContratoShows).ThenInclude(x => x.Show)
            .Where(c => c.FechaEvento >= inicio && c.FechaEvento < fin)
            .ToListAsync();

        // Turnos para pagar en el salón (efectivo o cheque): solo los ve el administrador
        var turnos = esAdmin
            ? await _db.Pagos.AsNoTracking().Include(p => p.Contrato).ThenInclude(c => c.Cliente)
                .Where(p => p.TurnoPago != null && p.TurnoPago >= inicio && p.TurnoPago < fin
                         && (p.Estado == "Solicitado" || p.Estado == "Reprogramado" || p.Estado == "Rechazado"))
                .ToListAsync()
            : new List<Pago>();

        var vm = new CalendarioVM
        {
            Anio = a, Mes = m, Hoy = hoy, EsAdmin = esAdmin, EsCliente = esCliente,
            Paquetes = esCliente ? await _db.Paquetes.AsNoTracking().OrderBy(p => p.Precio).ToListAsync() : new()
        };

        for (var f = inicio; f < fin; f = f.AddDays(1))
        {
            var c = contratos.FirstOrDefault(x => x.FechaEvento.Date == f);
            EventoCalendario? ev = null;
            if (c != null)
            {
                var mio = esCliente && c.Cliente.IdentityUserId == uid;
                var paso = EtapaContrato.Actual(c);
                ev = new EventoCalendario { IdContrato = c.IdContrato, EsMio = mio };
                if (esAdmin || mio)
                {
                    ev.Paquete = c.Paquete.Nombre;
                    ev.Horario = Horario(c.Paquete);
                    ev.Etapa = esAdmin ? EtapaContrato.TextoAdmin(paso) : EtapaContrato.TextoCliente(paso);
                    ev.Completo = paso == EtapaContrato.Paso.Completo;
                }
                if (esAdmin)
                {
                    ev.Cliente = c.Cliente.RazonSocialApellido;
                    ev.RequiereAccion = EtapaContrato.TocaAlAdmin(paso);
                    ev.Personal = c.ContratoEmpleados.OrderBy(x => x.HoraCitacion)
                        .Select(x => $"{x.Empleado.NombreApellido} ({x.Empleado.Puesto}), citación {x.HoraCitacion:hh\\:mm}").ToList();
                    ev.Shows = c.ContratoShows.OrderBy(x => x.Show.HorarioShow)
                        .Select(x => $"{x.Show.NombreShow}, {x.Show.HorarioShow:hh\\:mm} hs").ToList();
                }
            }
            vm.Dias.Add(new DiaCalendario
            {
                Fecha = f, EnMes = f.Month == m, Pasado = f < hoy, Evento = ev,
                Turnos = turnos.Where(t => t.TurnoPago!.Value.Date == f).OrderBy(t => t.TurnoPago)
                    .Select(t => $"{t.TurnoPago:HH:mm} hs: {t.Contrato.Cliente.RazonSocialApellido} paga $ {t.Monto.ToString("N0", Es)} en {t.MetodoPago.ToLower()}")
                    .ToList()
            });
        }

        return View(vm);
    }

    // Agenda del mes por persona, para imprimir o mandar por mensaje al personal (no tiene cuenta en el sitio)
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Personal(int? anio, int? mes)
    {
        var hoy = Reloj.Ahora().Date;
        var (a, m) = Normalizar(anio, mes, hoy);
        var primero = new DateTime(a, m, 1);

        var contratos = await _db.Contratos.AsNoTracking()
            .Include(c => c.Paquete).ThenInclude(p => p.Coordinador)
            .Include(c => c.ContratoEmpleados).ThenInclude(x => x.Empleado)
            .Include(c => c.ContratoShows).ThenInclude(x => x.Show)
            .Where(c => c.FechaEvento >= primero && c.FechaEvento < primero.AddMonths(1))
            .OrderBy(c => c.FechaEvento)
            .ToListAsync();

        ViewData["Anio"] = a;
        ViewData["Mes"] = m;
        ViewData["Titulo"] = primero.ToString("MMMM 'de' yyyy", Es);
        return View(contratos);
    }
}
