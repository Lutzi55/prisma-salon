using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;

namespace PlanificacionEventos.Controllers;

[Authorize(Roles = "Administrador")]
public class AuditoriaController : Controller
{
    private readonly AppDbContext _db;
    public AuditoriaController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? usuario, string? entidad, string? accion, string? texto, DateTime? desde, DateTime? hasta)
    {
        var q = _db.Auditorias.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(usuario)) q = q.Where(a => a.UsuarioEmail.Contains(usuario));
        if (!string.IsNullOrWhiteSpace(entidad)) q = q.Where(a => a.Entidad == entidad);
        if (!string.IsNullOrWhiteSpace(accion)) q = q.Where(a => a.Accion == accion);
        if (!string.IsNullOrWhiteSpace(texto)) q = q.Where(a => a.Descripcion!.Contains(texto) || a.Detalle!.Contains(texto));
        if (desde.HasValue) q = q.Where(a => a.Fecha >= desde.Value.Date);
        if (hasta.HasValue) q = q.Where(a => a.Fecha < hasta.Value.Date.AddDays(1));

        ViewData["Entidades"] = await _db.Auditorias.Select(a => a.Entidad).Distinct().OrderBy(x => x).ToListAsync();
        return View(await q.OrderByDescending(a => a.Fecha).Take(200).ToListAsync());
    }
}