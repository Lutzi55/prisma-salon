using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;
using PlanificacionEventos.Models;

namespace PlanificacionEventos.Controllers;

[AllowAnonymous]
public class CatalogoController : Controller
{
    private static readonly string[] Extensiones = { ".jpg", ".jpeg", ".png", ".webp" };
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public CatalogoController(AppDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

    private static bool EsImagen(string f) =>
        Extensiones.Contains(Path.GetExtension(f).ToLowerInvariant());

    // Fotos del carrusel: img/galeria/{idPaquete} si tiene imágenes; si no, img/galeria
    private List<string> Galeria(int idPaquete)
    {
        var rel = Path.Combine("img", "galeria", idPaquete.ToString());
        var dir = Path.Combine(_env.WebRootPath, rel);
        if (!Directory.Exists(dir) || !Directory.EnumerateFiles(dir).Any(EsImagen))
        {
            rel = Path.Combine("img", "galeria");
            dir = Path.Combine(_env.WebRootPath, rel);
        }
        if (!Directory.Exists(dir)) return new List<string>();

        return Directory.EnumerateFiles(dir).Where(EsImagen)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Select(f => "~/" + rel.Replace('\\', '/') + "/" + Uri.EscapeDataString(Path.GetFileName(f)))
            .ToList();
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var logueado = User.Identity?.IsAuthenticated == true;

        var p = await _db.Paquetes.AsNoTracking().Include(x => x.Coordinador)
            .FirstOrDefaultAsync(x => x.IdPaquete == id);
        if (p == null) return NotFound();

        ViewData["Logueado"] = logueado;
        ViewData["Galeria"] = Galeria(id);
        // Los visitantes sin cuenta no reciben las opciones ni sus precios
        ViewData["Categorias"] = logueado
            ? await _db.CategoriasOpcion.AsNoTracking()
                .Include(c => c.Opciones.Where(o => o.Activa))
                .OrderBy(c => c.Orden).ToListAsync()
            : new List<CategoriaOpcion>();
        return View(p);
    }
    // ===================== Personalizar paquete (cliente) =====================
    // El cliente elige fecha y opciones; se crea un contrato "pedido por el cliente"
    // con el precio de cada opción congelado. El salón lo revisa, redacta y envía.

    private async Task<List<CategoriaOpcion>> CategoriasActivas() =>
        await _db.CategoriasOpcion.AsNoTracking()
            .Include(c => c.Opciones.Where(o => o.Activa))
            .OrderBy(c => c.Orden).ToListAsync();

    private async Task<Cliente?> FichaDelUsuario()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await _db.Clientes.FirstOrDefaultAsync(c => c.IdentityUserId == uid);
    }

    private async Task<IActionResult> FormPersonalizar(PaqueteEvento p, PersonalizarVM vm)
    {
        ViewData["Paquete"] = p;
        ViewData["Categorias"] = await CategoriasActivas();
        ViewData["TieneFicha"] = await FichaDelUsuario() != null;
        return View("Personalizar", vm);
    }

    // fecha: viene del calendario de disponibilidad cuando el cliente elige un día libre
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Personalizar(int id, DateTime? fecha)
    {
        var p = await _db.Paquetes.AsNoTracking().FirstOrDefaultAsync(x => x.IdPaquete == id);
        if (p == null) return RedirectToAction("Index", "Home");

        // Por defecto queda marcada la opción incluida (precio 0) de cada categoría de opción única
        var incluidas = (await CategoriasActivas())
            .Where(c => !c.EsMultiple)
            .Select(c => c.Opciones.Where(o => o.PrecioAdicional == 0).Select(o => o.IdOpcion).FirstOrDefault())
            .Where(idOpcion => idOpcion != 0).ToList();

        var fechaValida = fecha.HasValue && fecha.Value.Date > Reloj.Ahora().Date ? fecha.Value.Date : (DateTime?)null;
        return await FormPersonalizar(p, new PersonalizarVM { IdPaquete = id, OpcionesIds = incluidas, FechaEvento = fechaValida });
    }

    [Authorize(Roles = "Cliente"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Personalizar(PersonalizarVM vm)
    {
        var p = await _db.Paquetes.AsNoTracking().FirstOrDefaultAsync(x => x.IdPaquete == vm.IdPaquete);
        if (p == null) return RedirectToAction("Index", "Home");

        var hoy = Reloj.Ahora().Date;
        if (vm.FechaEvento.HasValue)
        {
            var f = vm.FechaEvento.Value.Date;
            if (f <= hoy)
                ModelState.AddModelError(nameof(vm.FechaEvento), "Elegí una fecha a partir de mañana");
            else if (await _db.Contratos.AnyAsync(c => c.FechaEvento.Date == f))
                ModelState.AddModelError(nameof(vm.FechaEvento), "Esa fecha ya está reservada. Probá con otra");
        }

        // Opciones: deben existir y estar activas; en categorías de opción única, una sola
        var elegidas = await _db.Opciones.Include(o => o.Categoria)
            .Where(o => vm.OpcionesIds.Contains(o.IdOpcion) && o.Activa).ToListAsync();
        foreach (var g in elegidas.Where(o => o.Categoria != null && !o.Categoria.EsMultiple).GroupBy(o => o.Categoria!.Nombre))
            if (g.Count() > 1)
                ModelState.AddModelError(nameof(vm.OpcionesIds), $"En «{g.Key}» se puede elegir una sola opción");

        if (vm.PagoInicial is not ("Sena" or "Total"))
            ModelState.AddModelError(nameof(vm.PagoInicial), "Elegí si pagás la seña o el total");
        if (!PoliticaPagos.MediosSena.Contains(vm.MedioReserva))
            ModelState.AddModelError(nameof(vm.MedioReserva), "Para reservar se paga solo por transferencia o tarjeta de débito");

        if (!vm.AceptaSena)
            ModelState.AddModelError(nameof(vm.AceptaSena), $"Para enviar el pedido tenés que aceptar el pago de la seña del {PoliticaPagos.PorcentajeSena} %");

        // Si la cuenta todavía no tiene ficha de cliente, se piden los datos y se crea
        var cliente = await FichaDelUsuario();
        if (cliente == null)
        {
            if (string.IsNullOrWhiteSpace(vm.RazonSocial)) ModelState.AddModelError(nameof(vm.RazonSocial), "Ingresá tu nombre y apellido");
            if (string.IsNullOrWhiteSpace(vm.Domicilio)) ModelState.AddModelError(nameof(vm.Domicilio), "Ingresá tu domicilio");
            if (string.IsNullOrWhiteSpace(vm.Telefono)) ModelState.AddModelError(nameof(vm.Telefono), "Ingresá un teléfono de contacto");
        }

        if (!ModelState.IsValid) return await FormPersonalizar(p, vm);

        if (cliente == null)
        {
            cliente = new Cliente
            {
                RazonSocialApellido = vm.RazonSocial!.Trim(),
                DomicilioLegal = vm.Domicilio!.Trim(),
                Telefono = vm.Telefono!.Trim(),
                IdentityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };
            _db.Clientes.Add(cliente);
        }

        var contrato = new Contrato
        {
            Cliente = cliente,
            IdPaquete = p.IdPaquete,
            FechaEvento = vm.FechaEvento!.Value.Date,
            FormaPago = vm.MedioReserva,
            ValorRealAcordado = p.Precio + elegidas.Sum(o => o.PrecioAdicional),
            PedidoPorCliente = true,
            NotaCliente = string.IsNullOrWhiteSpace(vm.Comentario) ? null : vm.Comentario.Trim()
        };
        foreach (var o in elegidas)
            contrato.ContratoOpciones.Add(new ContratoOpcion { IdOpcion = o.IdOpcion, PrecioAcordado = o.PrecioAdicional });

        _db.Contratos.Add(contrato);
        await _db.SaveChangesAsync();

        // Pago para reservar la fecha: la seña (30 %) o el total, por transferencia o débito.
        // Se guarda aparte para que la auditoría ya conozca el contrato.
        var esSena = vm.PagoInicial == "Sena";
        var pago = new Pago
        {
            IdContrato = contrato.IdContrato,
            EsSena = esSena,
            Monto = esSena ? PoliticaPagos.Sena(contrato.ValorRealAcordado) : contrato.ValorRealAcordado,
            MetodoPago = vm.MedioReserva,
            Concepto = esSena ? PoliticaPagos.ConceptoSena : PoliticaPagos.ConceptoTotal,
            Estado = "Solicitado",
            FechaPago = Reloj.Ahora(),
            FechaVencimiento = PoliticaPagos.VencimientoSena()
        };
        _db.Pagos.Add(pago);
        await _db.SaveChangesAsync();

        TempData["ok"] = "¡Pedido registrado! Ahora confirmá el pago para reservar la fecha";
        return RedirectToAction("PagarSolicitud", "MisContratos", new { id = pago.IdPago, confirmar = true });
    }
}
