using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;
using PlanificacionEventos.Models;

namespace PlanificacionEventos.Controllers;

[Authorize(Roles = "Administrador")]
public class PagosController : Controller
{
    private readonly AppDbContext _db;
    private readonly AlmacenArchivos _arch;
    public PagosController(AppDbContext db, AlmacenArchivos arch) { _db = db; _arch = arch; }

    // Acción de gestión -> estados desde los que se permite
    private static readonly Dictionary<string, string[]> Permitidos = new()
    {
        ["reprogramar"] = new[] { "Solicitado", "Reprogramado", "Rechazado" },
        ["cancelar"] = new[] { "Solicitado", "Reprogramado", "Rechazado" },
        ["rechazar"] = new[] { "Pendiente" },
        ["reembolsar"] = new[] { "Confirmado" },
    };

    // Los pagos se gestionan desde la ficha del contrato
    public IActionResult Index(int idContrato) =>
        RedirectToAction("Detalle", "Contratos", new { id = idContrato });

    // ---------- Registrar un pago ya recibido (queda confirmado) ----------

    public async Task<IActionResult> Create(int idContrato)
    {
        var c = await _db.Contratos.AsNoTracking().Include(x => x.Cliente).Include(x => x.Pagos)
            .FirstOrDefaultAsync(x => x.IdContrato == idContrato);
        if (c == null) return RedirectToAction("Index", "Contratos");
        ViewData["Contrato"] = c;
        return View(new PagoVM { IdContrato = idContrato, Monto = c.ValorRealAcordado - c.Pagado - c.EnRevision });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PagoVM vm)
    {
        var c = await _db.Contratos.Include(x => x.Pagos).FirstOrDefaultAsync(x => x.IdContrato == vm.IdContrato);
        if (c == null) return NotFound();

        var disponible = c.ValorRealAcordado - c.Pagado - c.EnRevision;
        if (vm.Monto > disponible)
            ModelState.AddModelError(nameof(vm.Monto), $"El monto supera el saldo pendiente (${disponible:N2})");
        if (vm.Comprobante != null && !_arch.Valido(vm.Comprobante, new[] { ".jpg", ".jpeg", ".png" }, 3_000_000, out var err))
            ModelState.AddModelError(nameof(vm.Comprobante), err);
        if (!ModelState.IsValid) { ViewData["Contrato"] = c; return View(vm); }

        _db.Add(new Pago
        {
            IdContrato = vm.IdContrato,
            Monto = vm.Monto,
            FechaPago = vm.FechaPago,
            MetodoPago = vm.MetodoPago,
            Concepto = vm.Concepto,
            Estado = "Confirmado",
            RutaComprobanteImg = await _arch.GuardarAsync(vm.Comprobante!, "comprobantes")
        });
        await _db.SaveChangesAsync();
        TempData["ok"] = "Pago registrado y confirmado";
        return RedirectToAction(nameof(Index), new { idContrato = vm.IdContrato });
    }

    // ---------- Solicitar un pago al cliente ----------

    public async Task<IActionResult> Solicitar(int idContrato)
    {
        var c = await _db.Contratos.AsNoTracking().Include(x => x.Cliente).Include(x => x.Pagos)
            .FirstOrDefaultAsync(x => x.IdContrato == idContrato);
        if (c == null) return NotFound();
        if (c.PorSolicitar <= 0)
        {
            TempData["error"] = "Ya se cobró o solicitó el total del contrato";
            return RedirectToAction(nameof(Index), new { idContrato });
        }
        ViewData["Contrato"] = c;
        // El primer recibo es la seña obligatoria (30 %, transferencia o débito); después, el saldo
        var primero = !c.Pagos.Any(p => p.Estado is "Confirmado" or "Pendiente" or "Solicitado" or "Reprogramado" or "Rechazado");
        var sena = Math.Min(PoliticaPagos.Sena(c.ValorRealAcordado), c.PorSolicitar);
        return View(new SolicitudVM
        {
            IdContrato = idContrato,
            EsSena = primero,
            Monto = primero ? sena : c.PorSolicitar,
            Concepto = primero ? $"Seña del {PoliticaPagos.PorcentajeSena} % para reservar la fecha" : "Saldo del evento",
            FechaVencimiento = primero ? PoliticaPagos.VencimientoSena() : PoliticaPagos.VencimientoSaldo(c.FechaEvento)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Solicitar(SolicitudVM vm)
    {
        var c = await _db.Contratos.Include(x => x.Cliente).Include(x => x.Pagos)
            .FirstOrDefaultAsync(x => x.IdContrato == vm.IdContrato);
        if (c == null) return NotFound();

        if (vm.Monto > c.PorSolicitar)
            ModelState.AddModelError(nameof(vm.Monto), $"El monto supera lo que queda por solicitar (${c.PorSolicitar:N2})");
        if (vm.FechaVencimiento.HasValue && vm.FechaVencimiento.Value.Date < Reloj.Ahora().Date)
            ModelState.AddModelError(nameof(vm.FechaVencimiento), "El vencimiento no puede ser anterior a hoy");
        else if (vm.FechaVencimiento.HasValue && vm.FechaVencimiento.Value.Date > c.FechaEvento.Date)
            ModelState.AddModelError(nameof(vm.FechaVencimiento), "El vencimiento no puede ser después del evento");
        if (!ModelState.IsValid) { ViewData["Contrato"] = c; return View(vm); }

        _db.Add(new Pago
        {
            IdContrato = c.IdContrato,
            Monto = vm.Monto,
            Concepto = vm.Concepto,
            MetodoPago = "A definir",
            EsSena = vm.EsSena,
            Estado = "Solicitado",
            FechaPago = Reloj.Ahora(),
            FechaVencimiento = vm.FechaVencimiento!.Value.Date
        });
        await _db.SaveChangesAsync();
        TempData["ok"] = "Recibo enviado: el cliente ya lo ve en «Mis contratos» para pagar";
        return RedirectToAction(nameof(Index), new { idContrato = c.IdContrato });
    }

    // ---------- Cobro en el salón (el cliente vino con su turno a pagar en efectivo o cheque) ----------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CobrarEnSalon(int id)
    {
        var p = await _db.Pagos.Include(x => x.Contrato).ThenInclude(x => x.Pagos)
            .FirstOrDefaultAsync(x => x.IdPago == id);
        if (p == null) return RedirectToAction("Index", "Contratos");
        if (!EstadoPago.AdmiteComprobante(p.Estado) || p.TurnoPago == null)
        {
            TempData["error"] = "Ese pago no tiene un turno pendiente de cobro";
            return RedirectToAction(nameof(Index), new { idContrato = p.IdContrato });
        }
        if (p.Contrato.Pagado + p.Monto > p.Contrato.ValorRealAcordado)
        {
            TempData["error"] = "No se puede registrar: superaría el valor del contrato";
            return RedirectToAction(nameof(Index), new { idContrato = p.IdContrato });
        }
        p.Estado = "Confirmado";
        p.FechaPago = Reloj.Ahora();
        p.Nota = null;
        await _db.SaveChangesAsync();
        TempData["ok"] = $"Cobro registrado ({p.MetodoPago})";
        return RedirectToAction(nameof(Index), new { idContrato = p.IdContrato });
    }

    // ---------- Confirmar un comprobante ----------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(int id)
    {
        var p = await _db.Pagos.Include(x => x.Contrato).ThenInclude(x => x.Pagos)
            .FirstOrDefaultAsync(x => x.IdPago == id);
        if (p == null) return NotFound();

        if (p.Estado != "Pendiente")
        {
            TempData["error"] = "Solo se pueden confirmar los pagos que están en revisión";
            return RedirectToAction(nameof(Index), new { idContrato = p.IdContrato });
        }
        if (p.Contrato.Pagado + p.Monto > p.Contrato.ValorRealAcordado)
        {
            TempData["error"] = "No se puede confirmar: superaría el valor del contrato";
            return RedirectToAction(nameof(Index), new { idContrato = p.IdContrato });
        }

        p.Estado = "Confirmado";
        p.Nota = null;
        await _db.SaveChangesAsync();
        TempData["ok"] = "Pago confirmado";
        if (p.Contrato.Pagado >= p.Contrato.ValorRealAcordado) TempData["ok"] = "Pago confirmado: el contrato quedó pagado en su totalidad";
        return RedirectToAction(nameof(Index), new { idContrato = p.IdContrato });
    }

    // ---------- Reprogramar, cancelar, rechazar y reembolsar ----------

    public async Task<IActionResult> Gestionar(int id, string accion)
    {
        var p = await _db.Pagos.AsNoTracking().Include(x => x.Contrato).ThenInclude(x => x.Cliente)
            .FirstOrDefaultAsync(x => x.IdPago == id);
        if (p == null) return NotFound();
        if (!Permitidos.TryGetValue(accion ?? "", out var ok) || !ok.Contains(p.Estado))
        {
            TempData["error"] = "Esa acción no está disponible para este pago";
            return RedirectToAction(nameof(Index), new { idContrato = p.IdContrato });
        }
        ViewData["Pago"] = p;
        var hoy = Reloj.Ahora().Date;
        return View(new GestionVM
        {
            IdPago = id,
            Accion = accion!,
            // Por defecto: 10 días antes del evento; si eso ya pasó, una semana más de plazo
            NuevoVencimiento = accion == "reprogramar"
                ? (PoliticaPagos.VencimientoSaldo(p.Contrato.FechaEvento) > (p.FechaVencimiento ?? hoy)
                    ? PoliticaPagos.VencimientoSaldo(p.Contrato.FechaEvento)
                    : (p.FechaVencimiento > hoy ? p.FechaVencimiento!.Value : hoy).AddDays(7))
                : null
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Gestionar(GestionVM vm)
    {
        var p = await _db.Pagos.Include(x => x.Contrato).ThenInclude(x => x.Cliente)
            .FirstOrDefaultAsync(x => x.IdPago == vm.IdPago);
        if (p == null) return NotFound();
        if (!Permitidos.TryGetValue(vm.Accion ?? "", out var ok) || !ok.Contains(p.Estado))
        {
            TempData["error"] = "Esa acción no está disponible para este pago";
            return RedirectToAction(nameof(Index), new { idContrato = p.IdContrato });
        }

        var nota = vm.Nota?.Trim();
        if (vm.Accion == "reprogramar")
        {
            if (!vm.NuevoVencimiento.HasValue) ModelState.AddModelError(nameof(vm.NuevoVencimiento), "Indicá el nuevo vencimiento");
            else if (vm.NuevoVencimiento.Value.Date < Reloj.Ahora().Date) ModelState.AddModelError(nameof(vm.NuevoVencimiento), "El vencimiento no puede ser anterior a hoy");
        }
        else if (string.IsNullOrWhiteSpace(nota))
            ModelState.AddModelError(nameof(vm.Nota), "Escribí el motivo: lo va a ver el cliente");
        if (!ModelState.IsValid) { ViewData["Pago"] = p; return View(vm); }

        switch (vm.Accion)
        {
            case "reprogramar":
                p.FechaVencimientoOriginal ??= p.FechaVencimiento;
                p.FechaVencimiento = vm.NuevoVencimiento!.Value.Date;
                p.Estado = "Reprogramado";
                p.Nota = nota;
                TempData["ok"] = "Vencimiento reprogramado";
                break;
            case "cancelar":
                p.Estado = "Cancelado"; p.Nota = nota;
                TempData["ok"] = "Solicitud cancelada";
                break;
            case "rechazar":
                p.Estado = "Rechazado"; p.Nota = nota;
                TempData["ok"] = "Comprobante rechazado: el cliente podrá enviar otro";
                break;
            case "reembolsar":
                p.Estado = "Reembolsado"; p.Nota = nota;
                TempData["ok"] = "Reembolso registrado: el monto volvió al saldo del contrato";
                break;
        }
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { idContrato = p.IdContrato });
    }

    // Solo se eliminan solicitudes ya canceladas; el resto queda como historial
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Pagos.FindAsync(id);
        if (p == null) return NotFound();
        if (p.Estado != "Cancelado")
        {
            TempData["error"] = "Solo se pueden eliminar las solicitudes canceladas. El resto queda como historial";
            return RedirectToAction(nameof(Index), new { idContrato = p.IdContrato });
        }
        _db.Remove(p);
        await _db.SaveChangesAsync();
        _arch.Borrar(p.RutaComprobanteImg);
        TempData["ok"] = "Solicitud eliminada";
        return RedirectToAction(nameof(Index), new { idContrato = p.IdContrato });
    }
}