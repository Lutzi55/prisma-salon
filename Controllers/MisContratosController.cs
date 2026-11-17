using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;
using PlanificacionEventos.Models;

namespace PlanificacionEventos.Controllers;

[Authorize(Roles = "Cliente")]
public class MisContratosController : Controller
{
    private readonly AppDbContext _db;
    private readonly AlmacenArchivos _arch;
    public MisContratosController(AppDbContext db, AlmacenArchivos arch) { _db = db; _arch = arch; }

    private string? Uid => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private Task<Contrato?> Propio(int id)
    {
        var uid = Uid;
        return _db.Contratos.Include(x => x.Paquete).Include(x => x.Pagos).Include(x => x.Cliente)
            .FirstOrDefaultAsync(x => x.IdContrato == id && x.Cliente.IdentityUserId == uid);
    }

    // En vez de una página de error, vuelve a Mis contratos con un mensaje claro
    private IActionResult Volver(string mensaje)
    {
        TempData["error"] = mensaje;
        return RedirectToAction(nameof(Index));
    }

    private const string NoEsTuyo = "No encontramos ese contrato en tu cuenta";
    private const string PagoNoDisponible = "Ese pago ya no admite comprobante (puede que ya lo hayas enviado o que el salón lo haya cancelado)";

    // Pagos sobre los que el cliente puede enviar (o reenviar) comprobante
    private Task<Pago?> SolicitudPropia(int id)
    {
        var uid = Uid;
        return _db.Pagos.Include(p => p.Contrato).ThenInclude(c => c.Cliente)
            .Include(p => p.Contrato).ThenInclude(c => c.Pagos)
            .FirstOrDefaultAsync(p => p.IdPago == id
            && p.Contrato.Cliente.IdentityUserId == uid
            && (p.Estado == "Solicitado" || p.Estado == "Reprogramado" || p.Estado == "Rechazado"));
    }

    private static bool PuedeFirmar(Contrato c) =>
        c.ContratoEnviado && !c.Firmado && !string.IsNullOrWhiteSpace(c.TextoContrato);

    public async Task<IActionResult> Index()
    {
        var uid = Uid;
        return View(await _db.Contratos.AsNoTracking()
            .Include(c => c.Paquete).Include(c => c.Pagos)
            .Include(c => c.ContratoOpciones).ThenInclude(x => x.Opcion).ThenInclude(o => o.Categoria)
            .Where(c => c.Cliente.IdentityUserId == uid)
            .OrderBy(c => c.FechaEvento)
            .ToListAsync());
    }

    // ---------- Firma ----------

    public async Task<IActionResult> Firmar(int id)
    {
        var c = await Propio(id);
        if (c == null) return Volver(NoEsTuyo);
        if (!PuedeFirmar(c)) { TempData["error"] = "Este contrato no está disponible para firmar"; return RedirectToAction(nameof(Index)); }
        ViewData["Contrato"] = c;
        return View(new FirmarVM { IdContrato = id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Firmar(FirmarVM vm)
    {
        var c = await Propio(vm.IdContrato);
        if (c == null) return Volver(NoEsTuyo);
        if (!PuedeFirmar(c)) { TempData["error"] = "Este contrato no está disponible para firmar"; return RedirectToAction(nameof(Index)); }

        if (!vm.Acepto) ModelState.AddModelError(nameof(vm.Acepto), "Tenés que aceptar el contrato para poder firmarlo");

        const string prefijo = "data:image/png;base64,";
        byte[]? png = null;
        if (string.IsNullOrWhiteSpace(vm.FirmaPng) || !vm.FirmaPng.StartsWith(prefijo))
            ModelState.AddModelError(nameof(vm.FirmaPng), "Dibujá tu firma en el recuadro");
        else
        {
            try { png = Convert.FromBase64String(vm.FirmaPng[prefijo.Length..]); } catch (FormatException) { }
            if (png == null || png.Length < 100 || png.Length > 300_000 || png[0] != 0x89 || png[1] != 0x50)
                ModelState.AddModelError(nameof(vm.FirmaPng), "La firma no es válida. Dibujala de nuevo");
        }
        if (!ModelState.IsValid) { ViewData["Contrato"] = c; return View(vm); }

        c.RutaFirmaCliente = await _arch.GuardarBytesAsync(png!, "firmas", ".png");
        c.FirmanteNombre = vm.Nombre.Trim();
        c.FirmadoEl = Reloj.Ahora();
        c.FirmaHash = Huella.De(c.TextoContrato!);
        await _db.SaveChangesAsync();

        TempData["ok"] = "¡Contrato firmado! Ya podés descargar tu copia en PDF";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Pagar un pago solicitado: comprobante (transferencia/débito/depósito) o turno (efectivo/cheque) ----------

    // Lo que queda por pagar si el cliente elige pagar todo de una en vez de la seña
    private static decimal TotalRestante(Pago p) => p.Monto + Math.Max(0, p.Contrato.PorSolicitar);

    private void DatosPago(Pago p)
    {
        ViewData["Pago"] = p;
        ViewData["Medios"] = PoliticaPagos.MediosPermitidos(PoliticaPagos.EsReserva(p));
        ViewData["Reserva"] = PoliticaPagos.EsReserva(p);
        ViewData["TotalRestante"] = TotalRestante(p);
        ViewData["Pagador"] = p.Contrato.Cliente?.RazonSocialApellido ?? "Cliente";
    }

    public async Task<IActionResult> PagarSolicitud(int id, bool confirmar = false)
    {
        var p = await SolicitudPropia(id);
        if (p == null) return Volver(PagoNoDisponible);
        DatosPago(p);
        var medios = PoliticaPagos.MediosPermitidos(PoliticaPagos.EsReserva(p));
        return View(new EnviarComprobanteVM
        {
            IdPago = id,
            Confirmar = confirmar,
            FechaPago = Reloj.Ahora().Date,
            MetodoPago = medios.Contains(p.MetodoPago) ? p.MetodoPago : medios[0],
            TurnoFecha = p.TurnoPago?.Date,
            TurnoHora = p.TurnoPago?.ToString("HH:mm")
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PagarSolicitud(EnviarComprobanteVM vm)
    {
        var p = await SolicitudPropia(vm.IdPago);
        if (p == null) return Volver(PagoNoDisponible);

        var hoy = Reloj.Ahora().Date;
        var reserva = PoliticaPagos.EsReserva(p);
        var medios = PoliticaPagos.MediosPermitidos(reserva);
        if (!medios.Contains(vm.MetodoPago))
            ModelState.AddModelError(nameof(vm.MetodoPago), reserva
                ? "Para reservar la fecha se paga solo por transferencia o tarjeta de débito"
                : "Elegí un medio de pago de la lista");

        var enPersona = PoliticaPagos.EsEnPersona(vm.MetodoPago);
        DateTime? turno = null;
        if (enPersona)
        {
            // Efectivo o cheque: turno de lunes a viernes, de 10 a 18
            if (!vm.TurnoFecha.HasValue) ModelState.AddModelError(nameof(vm.TurnoFecha), "Elegí el día del turno");
            else if (vm.TurnoFecha.Value.Date < hoy) ModelState.AddModelError(nameof(vm.TurnoFecha), "El turno no puede ser en una fecha pasada");
            else if (vm.TurnoFecha.Value.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                ModelState.AddModelError(nameof(vm.TurnoFecha), "Los turnos son de lunes a viernes");
            if (!TimeSpan.TryParse(vm.TurnoHora, out var hora) || !PoliticaPagos.HorariosTurno().Contains(vm.TurnoHora))
                ModelState.AddModelError(nameof(vm.TurnoHora), "Elegí un horario de la lista");
            else if (vm.TurnoFecha.HasValue) turno = vm.TurnoFecha.Value.Date.Add(hora);
            if (turno.HasValue && turno.Value <= Reloj.Ahora())
                ModelState.AddModelError(nameof(vm.TurnoHora), "Ese horario ya pasó. Elegí otro");
        }
        else
        {
            if (vm.FechaPago.Date > hoy)
                ModelState.AddModelError(nameof(vm.FechaPago), "La fecha no puede ser futura");
            if (vm.Comprobante == null)
                ModelState.AddModelError(nameof(vm.Comprobante), "Adjuntá el comprobante (JPG o PNG)");
            else if (!_arch.Valido(vm.Comprobante, new[] { ".jpg", ".jpeg", ".png" }, 3_000_000, out var err))
                ModelState.AddModelError(nameof(vm.Comprobante), err);
        }
        if (!ModelState.IsValid) { DatosPago(p); return View(vm); }

        // En vez de la seña, pagar todo lo que queda de una vez
        if (vm.PagarTodo && p.EsSena)
        {
            p.Monto = TotalRestante(p);
            p.EsSena = false;
            p.Concepto = PoliticaPagos.ConceptoTotal;
        }

        p.MetodoPago = vm.MetodoPago;
        p.Nota = null; // el motivo anterior (rechazo o reprogramación) ya no aplica

        if (enPersona)
        {
            p.TurnoPago = turno;
            if (p.Estado == "Rechazado") p.Estado = "Solicitado";
            await _db.SaveChangesAsync();
            TempData["ok"] = $"Turno agendado: {turno:dd/MM} a las {turno:HH:mm}. Te esperamos en el salón";
            return RedirectToAction(nameof(Index));
        }

        var anterior = p.RutaComprobanteImg;
        p.RutaComprobanteImg = await _arch.GuardarAsync(vm.Comprobante!, "comprobantes");
        p.FechaPago = vm.FechaPago;
        p.TurnoPago = null;
        p.Estado = "Pendiente";
        await _db.SaveChangesAsync();
        _arch.Borrar(anterior);

        TempData["ok"] = $"Comprobante de $ {p.Monto:N0} enviado. Cuando el salón lo confirme, se suma a lo pagado";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Reservar: pedidos sin ningún pago todavía (por ejemplo, hechos antes de la seña obligatoria) ----------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reservar(int id)
    {
        var c = await Propio(id);
        if (c == null) return Volver(NoEsTuyo);
        if (c.PorSolicitar < c.ValorRealAcordado || c.FechaEvento.Date <= Reloj.Ahora().Date)
            return Volver("Este contrato ya tiene un pago en curso");

        var pago = new Pago
        {
            IdContrato = c.IdContrato,
            EsSena = true,
            Monto = PoliticaPagos.Sena(c.ValorRealAcordado),
            MetodoPago = PoliticaPagos.MediosSena[0],
            Concepto = PoliticaPagos.ConceptoSena,
            Estado = "Solicitado",
            FechaPago = Reloj.Ahora(),
            FechaVencimiento = PoliticaPagos.VencimientoSena()
        };
        _db.Pagos.Add(pago);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(PagarSolicitud), new { id = pago.IdPago, confirmar = true });
    }

    // ---------- Informar un pago por iniciativa propia ----------

    public async Task<IActionResult> SubirPago(int id)
    {
        var c = await Propio(id);
        if (c == null) return Volver(NoEsTuyo);
        if (c.PorSolicitar <= 0)
            return Volver("Ya informaste o se te pidió el total del contrato. Enviá el comprobante desde el pago correspondiente");
        ViewData["Contrato"] = c;
        return View(new PagoVM { IdContrato = id, Monto = c.PorSolicitar });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirPago(PagoVM vm)
    {
        var c = await Propio(vm.IdContrato);
        if (c == null) return Volver(NoEsTuyo);

        if (c.PorSolicitar <= 0)
            return Volver("Ya informaste o se te pidió el total del contrato. Enviá el comprobante desde el pago correspondiente");
        if (vm.Monto > c.PorSolicitar)
            ModelState.AddModelError(nameof(vm.Monto), $"El monto supera lo que queda por pagar ($ {c.PorSolicitar:N2})");
        if (vm.FechaPago.Date > Reloj.Ahora().Date)
            ModelState.AddModelError(nameof(vm.FechaPago), "La fecha no puede ser futura");
        if (!PoliticaPagos.MediosConComprobante.Contains(vm.MetodoPago))
            ModelState.AddModelError(nameof(vm.MetodoPago), "Por acá se informan pagos con comprobante (transferencia, débito o depósito). Para efectivo o cheque, pedí un turno desde el pago solicitado");
        if (vm.Comprobante != null && !_arch.Valido(vm.Comprobante, new[] { ".jpg", ".jpeg", ".png" }, 3_000_000, out var err))
            ModelState.AddModelError(nameof(vm.Comprobante), err);
        if (!ModelState.IsValid) { ViewData["Contrato"] = c; return View(vm); }

        _db.Add(new Pago
        {
            IdContrato = c.IdContrato,
            Monto = vm.Monto,
            FechaPago = vm.FechaPago,
            MetodoPago = vm.MetodoPago,
            Concepto = vm.Concepto,
            Estado = "Pendiente",
            RutaComprobanteImg = await _arch.GuardarAsync(vm.Comprobante!, "comprobantes")
        });
        await _db.SaveChangesAsync();
        TempData["ok"] = "Comprobante enviado. El salón lo revisará en breve";
        return RedirectToAction(nameof(Index));
    }
}
