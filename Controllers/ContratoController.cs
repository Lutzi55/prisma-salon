using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Models;
using PlanificacionEventos.Data;

namespace PlanificacionEventos.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ContratosController : Controller
    {
        private readonly AppDbContext _db;
        private readonly AlmacenArchivos _arch;
        public ContratosController(AppDbContext db, AlmacenArchivos arch) { _db = db; _arch = arch; }

        // ===================== Listado y detalle =====================

        // Primero lo que requiere una acción del salón, después el resto por fecha del evento
        public async Task<IActionResult> Index()
        {
            var lista = await _db.Contratos.AsNoTracking()
                .Include(c => c.Cliente).Include(c => c.Paquete).Include(c => c.Pagos)
                .ToListAsync();
            return View(lista
                .OrderBy(c => EtapaContrato.TocaAlAdmin(EtapaContrato.Actual(c)) ? 0
                            : EtapaContrato.Actual(c) == EtapaContrato.Paso.Completo ? 2 : 1)
                .ThenBy(c => c.FechaEvento)
                .ToList());
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var c = await ConDatos().AsNoTracking()
                .Include(x => x.Pagos)
                .Include(x => x.ContratoEmpleados).ThenInclude(x => x.Empleado)
                .FirstOrDefaultAsync(x => x.IdContrato == id);
            if (c == null)
            {
                TempData["error"] = "No encontramos ese contrato";
                return RedirectToAction(nameof(Index));
            }
            return View(c);
        }

        // ===================== Alta y edición =====================

        private async Task Listas()
        {
            ViewData["IdCliente"] = new SelectList(await _db.Clientes.OrderBy(c => c.RazonSocialApellido).ToListAsync(), "IdCliente", "RazonSocialApellido");
            ViewData["Paquetes"] = await _db.Paquetes.OrderBy(p => p.Nombre).ToListAsync();
            ViewData["Categorias"] = await _db.CategoriasOpcion
                .Include(c => c.Opciones.Where(o => o.Activa))
                .OrderBy(c => c.Orden).ToListAsync();
            ViewData["Shows"] = await _db.Shows.OrderBy(s => s.NombreShow).ToListAsync();
            ViewData["Empleados"] = await _db.Empleados.OrderBy(e => e.NombreApellido).ToListAsync();
        }

        private async Task<bool> Validar(ContratoVM vm)
        {
            if (vm.FechaEvento.HasValue)
            {
                var f = vm.FechaEvento.Value.Date;
                if (vm.IdContrato == 0 && f < Reloj.Ahora().Date)
                    ModelState.AddModelError(nameof(vm.FechaEvento), "La fecha no puede ser anterior a hoy");
                if (await _db.Contratos.AnyAsync(c => c.FechaEvento.Date == f && c.IdContrato != vm.IdContrato))
                    ModelState.AddModelError(nameof(vm.FechaEvento), "Ya hay un evento contratado en esa fecha");
            }
            if (vm.ArchivoPdf != null && !_arch.Valido(vm.ArchivoPdf, new[] { ".pdf" }, 5_000_000, out var err))
                ModelState.AddModelError(nameof(vm.ArchivoPdf), err);

            // En las categorías de opción única se puede elegir una sola
            var elegidas = await _db.Opciones.Include(o => o.Categoria)
                .Where(o => vm.OpcionesIds.Contains(o.IdOpcion)).ToListAsync();
            foreach (var g in elegidas.Where(o => o.Categoria != null && !o.Categoria.EsMultiple).GroupBy(o => o.Categoria!.Nombre))
                if (g.Count() > 1)
                    ModelState.AddModelError(nameof(vm.OpcionesIds), $"En «{g.Key}» se puede elegir una sola opción");

            return ModelState.IsValid;
        }

        // Copia los datos y sincroniza shows, personal y opciones (agrega los nuevos, quita los desmarcados)
        private async Task Aplicar(Contrato c, ContratoVM vm)
        {
            c.FechaEvento = vm.FechaEvento!.Value;
            c.ValorRealAcordado = vm.ValorRealAcordado;
            c.FormaPago = vm.FormaPago;
            c.IdCliente = vm.IdCliente!.Value;
            c.IdPaquete = vm.IdPaquete!.Value;

            var shows = vm.ShowsIds.Distinct().ToHashSet();
            foreach (var x in c.ContratoShows.Where(x => !shows.Contains(x.IdShow)).ToList()) _db.Remove(x);
            foreach (var id in shows.Except(c.ContratoShows.Select(x => x.IdShow)).ToList())
                c.ContratoShows.Add(new ContratoShow { IdShow = id });

            var emps = vm.EmpleadosIds.Distinct().ToHashSet();
            foreach (var x in c.ContratoEmpleados.Where(x => !emps.Contains(x.IdEmpleado)).ToList()) _db.Remove(x);
            foreach (var x in c.ContratoEmpleados.Where(x => emps.Contains(x.IdEmpleado))) x.HoraCitacion = vm.HoraCitacion;
            foreach (var id in emps.Except(c.ContratoEmpleados.Select(x => x.IdEmpleado)).ToList())
                c.ContratoEmpleados.Add(new ContratoEmpleado { IdEmpleado = id, HoraCitacion = vm.HoraCitacion });

            // Opciones: las nuevas toman el precio vigente (queda congelado); las que ya estaban conservan su precio
            var opciones = vm.OpcionesIds.Distinct().ToHashSet();
            foreach (var x in c.ContratoOpciones.Where(x => !opciones.Contains(x.IdOpcion)).ToList()) _db.Remove(x);
            var nuevas = opciones.Except(c.ContratoOpciones.Select(x => x.IdOpcion)).ToList();
            var precios = await _db.Opciones.Where(o => nuevas.Contains(o.IdOpcion))
                .ToDictionaryAsync(o => o.IdOpcion, o => o.PrecioAdicional);
            foreach (var id in nuevas.Where(precios.ContainsKey))
                c.ContratoOpciones.Add(new ContratoOpcion { IdOpcion = id, PrecioAcordado = precios[id] });
        }

        // fecha: viene del calendario cuando se elige un día libre
        public async Task<IActionResult> Create(DateTime? fecha)
        {
            await Listas();
            return View("Form", new ContratoVM { FechaEvento = fecha?.Date });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContratoVM vm)
        {
            if (!await Validar(vm)) { await Listas(); return View("Form", vm); }
            var c = new Contrato();
            await Aplicar(c, vm);
            if (vm.ArchivoPdf != null) c.RutaContratoPdf = await _arch.GuardarAsync(vm.ArchivoPdf, "contratos");
            _db.Add(c);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Contrato creado. Ahora redactalo y envialo al cliente";
            return RedirectToAction(nameof(Detalle), new { id = c.IdContrato });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.Contratos.Include(x => x.ContratoShows).Include(x => x.ContratoEmpleados).Include(x => x.ContratoOpciones)
                .AsNoTracking().FirstOrDefaultAsync(x => x.IdContrato == id);
            if (c == null) { TempData["error"] = "No encontramos ese contrato"; return RedirectToAction(nameof(Index)); }
            if (c.Firmado)
            {
                TempData["error"] = "El contrato ya fue firmado por el cliente y no se puede modificar";
                return RedirectToAction(nameof(Detalle), new { id });
            }
            await Listas();
            return View("Form", new ContratoVM
            {
                IdContrato = c.IdContrato,
                FechaEvento = c.FechaEvento,
                ValorRealAcordado = c.ValorRealAcordado,
                FormaPago = c.FormaPago,
                IdCliente = c.IdCliente,
                IdPaquete = c.IdPaquete,
                RutaContratoPdf = c.RutaContratoPdf,
                ShowsIds = c.ContratoShows.Select(x => x.IdShow).ToList(),
                EmpleadosIds = c.ContratoEmpleados.Select(x => x.IdEmpleado).ToList(),
                OpcionesIds = c.ContratoOpciones.Select(x => x.IdOpcion).ToList(),
                HoraCitacion = c.ContratoEmpleados.Select(x => x.HoraCitacion).FirstOrDefault()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ContratoVM vm)
        {
            var c = await _db.Contratos.Include(x => x.ContratoShows).Include(x => x.ContratoEmpleados).Include(x => x.ContratoOpciones)
                .FirstOrDefaultAsync(x => x.IdContrato == vm.IdContrato);
            if (c == null) { TempData["error"] = "No encontramos ese contrato"; return RedirectToAction(nameof(Index)); }
            if (c.Firmado)
            {
                TempData["error"] = "El contrato ya fue firmado por el cliente y no se puede modificar";
                return RedirectToAction(nameof(Detalle), new { id = c.IdContrato });
            }
            if (!await Validar(vm)) { vm.RutaContratoPdf = c.RutaContratoPdf; await Listas(); return View("Form", vm); }
            await Aplicar(c, vm);
            if (vm.ArchivoPdf != null)
            {
                _arch.Borrar(c.RutaContratoPdf);
                c.RutaContratoPdf = await _arch.GuardarAsync(vm.ArchivoPdf, "contratos");
            }
            await _db.SaveChangesAsync();
            TempData["ok"] = "Contrato actualizado";
            return RedirectToAction(nameof(Detalle), new { id = c.IdContrato });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Contratos.Include(x => x.Pagos).Include(x => x.ContratoShows)
                .Include(x => x.ContratoEmpleados).Include(x => x.ContratoOpciones)
                .FirstOrDefaultAsync(x => x.IdContrato == id);
            if (c == null) { TempData["error"] = "No encontramos ese contrato"; return RedirectToAction(nameof(Index)); }
            if (c.Firmado || c.Pagos.Any())
            {
                TempData["error"] = c.Firmado
                    ? "No se puede eliminar: el contrato ya está firmado"
                    : "No se puede eliminar: el contrato tiene pagos registrados";
                return RedirectToAction(nameof(Detalle), new { id });
            }
            _db.RemoveRange(c.ContratoShows);
            _db.RemoveRange(c.ContratoEmpleados);
            _db.RemoveRange(c.ContratoOpciones);
            _db.Remove(c);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["error"] = "No se puede eliminar: el contrato tiene registros asociados (por ejemplo, una reseña)";
                return RedirectToAction(nameof(Detalle), new { id });
            }
            _arch.Borrar(c.RutaContratoPdf);
            TempData["ok"] = "Contrato eliminado";
            return RedirectToAction(nameof(Index));
        }

        // ===================== Redacción del contrato =====================

        private IQueryable<Contrato> ConDatos() => _db.Contratos
            .Include(c => c.Cliente)
            .Include(c => c.Paquete).ThenInclude(p => p.Coordinador)
            .Include(c => c.ContratoShows).ThenInclude(x => x.Show)
            .Include(c => c.ContratoOpciones).ThenInclude(x => x.Opcion).ThenInclude(o => o.Categoria);

        public async Task<IActionResult> Redactar(int id, bool plantilla = false)
        {
            var c = await ConDatos().AsNoTracking().FirstOrDefaultAsync(x => x.IdContrato == id);
            if (c == null) { TempData["error"] = "No encontramos ese contrato"; return RedirectToAction(nameof(Index)); }
            ViewData["Cliente"] = c.Cliente.RazonSocialApellido;
            ViewData["Enviado"] = c.ContratoEnviado;
            ViewData["Firmado"] = c.Firmado;
            if (c.Firmado)
            {
                var coincide = Huella.De(c.TextoContrato ?? "") == c.FirmaHash;
                ViewData["FirmaInfo"] = $"Firmado por {c.FirmanteNombre} el {c.FirmadoEl:dd/MM/yyyy HH:mm}. Huella: {c.FirmaHash}. " +
                    (coincide ? "El texto coincide con lo que se firmó." : "ATENCIÓN: el texto no coincide con lo que se firmó.");
            }
            var texto = c.Firmado ? c.TextoContrato
                : plantilla || string.IsNullOrWhiteSpace(c.TextoContrato) ? PlantillaContrato.Generar(c) : c.TextoContrato;
            return View(new RedactarVM { IdContrato = id, Texto = texto ?? "" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Redactar(RedactarVM vm, string accion)
        {
            var c = await _db.Contratos.Include(x => x.Cliente).FirstOrDefaultAsync(x => x.IdContrato == vm.IdContrato);
            if (c == null) { TempData["error"] = "No encontramos ese contrato"; return RedirectToAction(nameof(Index)); }
            if (c.Firmado)
            {
                TempData["error"] = "El contrato ya fue firmado y no se puede modificar";
                return RedirectToAction(nameof(Detalle), new { id = c.IdContrato });
            }
            if (!ModelState.IsValid)
            {
                ViewData["Cliente"] = c.Cliente.RazonSocialApellido;
                ViewData["Enviado"] = c.ContratoEnviado;
                ViewData["Firmado"] = false;
                return View(vm);
            }
            c.TextoContrato = vm.Texto;
            if (accion == "enviar") c.ContratoEnviado = true;
            await _db.SaveChangesAsync();
            TempData["ok"] = accion == "enviar" ? "Contrato enviado: el cliente ya lo puede leer y firmar" : "Borrador guardado";
            return RedirectToAction(nameof(Detalle), new { id = c.IdContrato });
        }
    }
}
