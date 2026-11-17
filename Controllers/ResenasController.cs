using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;
using PlanificacionEventos.Models;

namespace PlanificacionEventos.Controllers;

// Opiniones de clientes. El cliente opina sobre SUS eventos ya realizados (una vez por contrato,
// con posibilidad de editar). El administrador puede ocultar o volver a mostrar cada reseña.
[Authorize]
public class ResenasController : Controller
{
    private readonly AppDbContext _db;
    public ResenasController(AppDbContext db) => _db = db;

    private string? Uid => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private Task<Contrato?> ContratoPropio(int id)
    {
        var uid = Uid;
        return _db.Contratos.Include(c => c.Paquete)
            .FirstOrDefaultAsync(c => c.IdContrato == id && c.Cliente.IdentityUserId == uid);
    }

    // ===================== Cliente =====================

    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Mias()
    {
        var uid = Uid;
        var contratos = await _db.Contratos.AsNoTracking().Include(c => c.Paquete)
            .Where(c => c.Cliente.IdentityUserId == uid)
            .OrderByDescending(c => c.FechaEvento).ToListAsync();
        ViewData["Resenas"] = await _db.Resenas.AsNoTracking()
            .Where(r => r.Cliente.IdentityUserId == uid)
            .ToDictionaryAsync(r => r.IdContrato);
        ViewData["Hoy"] = Reloj.Ahora().Date;
        return View(contratos);
    }

    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Opinar(int id)
    {
        var c = await ContratoPropio(id);
        if (c == null) return NotFound();
        if (c.FechaEvento.Date >= Reloj.Ahora().Date)
        {
            TempData["error"] = "Vas a poder dejar tu opinión después del evento";
            return RedirectToAction(nameof(Mias));
        }

        var r = await _db.Resenas.AsNoTracking().FirstOrDefaultAsync(x => x.IdContrato == id);
        ViewData["Contrato"] = c;
        ViewData["Existe"] = r != null;
        return View(new ResenaVM
        {
            IdContrato = id,
            Puntaje = r?.Puntaje ?? 5,
            Comentario = r?.Comentario ?? ""
        });
    }

    [Authorize(Roles = "Cliente"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Opinar(ResenaVM vm)
    {
        var c = await ContratoPropio(vm.IdContrato);
        if (c == null) return NotFound();
        if (c.FechaEvento.Date >= Reloj.Ahora().Date)
        {
            TempData["error"] = "Vas a poder dejar tu opinión después del evento";
            return RedirectToAction(nameof(Mias));
        }

        var r = await _db.Resenas.FirstOrDefaultAsync(x => x.IdContrato == vm.IdContrato);
        if (!ModelState.IsValid)
        {
            ViewData["Contrato"] = c;
            ViewData["Existe"] = r != null;
            return View(vm);
        }

        if (r == null)
        {
            _db.Resenas.Add(new Resena
            {
                IdContrato = c.IdContrato,
                IdCliente = c.IdCliente,
                Puntaje = vm.Puntaje,
                Comentario = vm.Comentario.Trim(),
                Fecha = Reloj.Ahora(),
                Visible = true
            });
            TempData["ok"] = "¡Gracias! Tu opinión ya está publicada en el sitio";
        }
        else
        {
            r.Puntaje = vm.Puntaje;
            r.Comentario = vm.Comentario.Trim();
            r.Fecha = Reloj.Ahora();
            TempData["ok"] = "Tu opinión se actualizó";
        }
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Mias));
    }

    // ===================== Administrador =====================

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Index()
    {
        var lista = await _db.Resenas.AsNoTracking()
            .Include(r => r.Cliente)
            .Include(r => r.Contrato).ThenInclude(c => c.Paquete)
            .OrderByDescending(r => r.Fecha).ToListAsync();
        return View(lista);
    }

    [Authorize(Roles = "Administrador"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarVisibilidad(int id)
    {
        var r = await _db.Resenas.FindAsync(id);
        if (r == null) return NotFound();
        r.Visible = !r.Visible;
        await _db.SaveChangesAsync();
        TempData["ok"] = r.Visible ? "La reseña vuelve a verse en el sitio" : "La reseña quedó oculta";
        return RedirectToAction(nameof(Index));
    }
    // El salón responde públicamente: para resolver una inconformidad o agradecer la crítica
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Responder(int id)
    {
        var r = await _db.Resenas.AsNoTracking().Include(x => x.Cliente)
            .Include(x => x.Contrato).ThenInclude(c => c.Paquete)
            .FirstOrDefaultAsync(x => x.IdResena == id);
        if (r == null) { TempData["error"] = "No encontramos esa reseña"; return RedirectToAction(nameof(Index)); }
        return View(r);
    }

    [Authorize(Roles = "Administrador"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Responder(int id, string? respuesta)
    {
        var r = await _db.Resenas.Include(x => x.Cliente)
            .Include(x => x.Contrato).ThenInclude(c => c.Paquete)
            .FirstOrDefaultAsync(x => x.IdResena == id);
        if (r == null) { TempData["error"] = "No encontramos esa reseña"; return RedirectToAction(nameof(Index)); }

        respuesta = respuesta?.Trim();
        if (string.IsNullOrEmpty(respuesta) || respuesta.Length < 5)
            ModelState.AddModelError("respuesta", "Escribí una respuesta de al menos 5 caracteres");
        else if (respuesta.Length > 800)
            ModelState.AddModelError("respuesta", "La respuesta puede tener hasta 800 caracteres");
        if (!ModelState.IsValid) { ViewData["Borrador"] = respuesta; return View(r); }

        r.Respuesta = respuesta;
        r.FechaRespuesta = Reloj.Ahora();
        await _db.SaveChangesAsync();
        TempData["ok"] = "Respuesta publicada debajo de la reseña";
        return RedirectToAction(nameof(Index));
    }
}
