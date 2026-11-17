using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Models;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using PlanificacionEventos.Data;

namespace PlanificacionEventos.Controllers;

[Authorize(Roles = "Administrador")]
public abstract class CrudController<T> : Controller where T : class, new()
{
    protected readonly AppDbContext _db;
    private readonly string _key, _titulo, _singular;

    protected CrudController(AppDbContext db, string key, string titulo, string singular)
    {
        _db = db; _key = key; _titulo = titulo; _singular = singular;
    }

    protected virtual Task Cargar() => Task.CompletedTask;

    private async Task<IActionResult> Formulario(T e, string titulo)
    {
        await Cargar();
        Preparar(titulo);
        return View("~/Views/Shared/CrudForm.cshtml", e);
    }

    private void Preparar(string titulo)
    {
        var props = typeof(T).GetProperties().Where(p =>
        {
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            return t.IsPrimitive || t == typeof(string) || t == typeof(decimal)
                || t == typeof(DateTime) || t == typeof(TimeSpan);
        }).ToList();
        string Label(PropertyInfo p) => p.GetCustomAttribute<DisplayAttribute>()?.Name ?? p.Name;

        ViewData["Titulo"] = titulo;
        ViewData["Key"] = _key;
        ViewData["ColsForm"] = props.Where(p => p.Name != _key).Select(p => (p.Name, Label(p))).ToList();
        ViewData["ColsIndex"] = props.Where(p => !p.Name.StartsWith("Id")).Select(p => (p.Name, Label(p))).ToList();
    }

    public async Task<IActionResult> Index()
    {
        Preparar(_titulo);
        return View("~/Views/Shared/CrudIndex.cshtml",
            await _db.Set<T>().AsNoTracking().ToListAsync());
    }

    public Task<IActionResult> Create() => Formulario(new T(), "Nuevo " + _singular);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(T e)
    {
        if (!ModelState.IsValid) return await Formulario(e, "Nuevo " + _singular);
        _db.Add(e);
        await _db.SaveChangesAsync();
        TempData["ok"] = _singular + " creado correctamente";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var e = await _db.Set<T>().FindAsync(id);
        return e == null ? NotFound() : await Formulario(e, "Editar " + _singular);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(T e)
    {
        if (!ModelState.IsValid) return await Formulario(e, "Editar " + _singular);
        _db.Update(e);
        await _db.SaveChangesAsync();
        TempData["ok"] = _singular + " actualizado";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Set<T>().FindAsync(id);
        if (e == null) return NotFound();
        try
        {
            _db.Remove(e);
            await _db.SaveChangesAsync();
            TempData["ok"] = _singular + " eliminado";
        }
        catch (DbUpdateException)
        {
            TempData["error"] = "No se puede eliminar: tiene registros asociados";
        }
        return RedirectToAction(nameof(Index));
    }
}