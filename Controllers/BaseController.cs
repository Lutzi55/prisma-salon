using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using PlanificacionEventos.Models;

[Authorize(Roles = "Administrador")]
public abstract class BaseController<T> : Controller where T : class, new()
{
    protected readonly AppDbContext _db;
    private readonly string _key, _titulo, _singular;
    private readonly bool _femenino;
    private readonly string? _descripcion;

    // femenino: para que los mensajes digan "Nueva categoría" / "Opción creada"
    // descripcion: texto corto que se muestra debajo del título para explicar qué se registra
    protected BaseController(AppDbContext db, string key, string titulo, string singular,
        bool femenino = false, string? descripcion = null)
    {
        _db = db;
        _key = key;
        _titulo = titulo;
        _singular = singular;
        _femenino = femenino;
        _descripcion = descripcion;
    }

    private string Nuevo => (_femenino ? "Nueva " : "Nuevo ") + _singular;
    private string Mensaje(string masculino) =>
        char.ToUpper(_singular[0]) + _singular[1..] + " " + (_femenino ? masculino[..^1] + "a" : masculino);

    // Las clases hijas lo sobrescriben si necesitan listas desplegables (como Paquetes)
    protected virtual Task Cargar() => Task.CompletedTask;

    private async Task<IActionResult> Formulario(T e, string titulo)
    {
        await Cargar();
        Preparar(titulo);
        return View("~/Views/Shared/CrudForm.cshtml", e);
    }

    private void Preparar(string titulo)
    {
        var props = typeof(T).GetProperties().Where(p => {
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            return t.IsPrimitive || t == typeof(string) || t == typeof(decimal)
                || t == typeof(DateTime) || t == typeof(TimeSpan);
        }).ToList();

        string Label(PropertyInfo p) => p.GetCustomAttribute<DisplayAttribute>()?.Name ?? p.Name;

        ViewData["Titulo"] = titulo;
        ViewData["Descripcion"] = _descripcion;
        ViewData["Key"] = _key;
        ViewData["ColsForm"] = props.Where(p => p.Name != _key).Select(p => (p.Name, Label(p))).ToList();
        // En el listado no se muestran las claves ni los textos largos (ensancharían la tabla)
        ViewData["ColsIndex"] = props.Where(p => !p.Name.StartsWith("Id")
                && p.GetCustomAttribute<DataTypeAttribute>()?.DataType != DataType.MultilineText)
            .Select(p => (p.Name, Label(p))).ToList();
    }

    public async Task<IActionResult> Index()
    {
        Preparar(_titulo);
        return View("~/Views/Shared/CrudIndex.cshtml",
            await _db.Set<T>().AsNoTracking().ToListAsync());
    }

    public Task<IActionResult> Create() => Formulario(new T(), Nuevo);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(T e)
    {
        if (!ModelState.IsValid) return await Formulario(e, Nuevo);
        _db.Add(e);
        await _db.SaveChangesAsync();
        TempData["ok"] = Mensaje("creado") + " correctamente";
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
        TempData["ok"] = Mensaje("actualizado");
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
            TempData["ok"] = Mensaje("eliminado");
        }
        catch (DbUpdateException)
        {
            TempData["error"] = "No se puede eliminar: tiene registros asociados";
        }
        return RedirectToAction(nameof(Index));
    }
}

public class CoordinadoresController : BaseController<Coordinador>
{
    public CoordinadoresController(AppDbContext db)
        : base(db, "IdCoordinador", "Coordinadores", "coordinador",
            descripcion: "Las personas del salón que acompañan cada evento. Cada paquete tiene un coordinador a cargo.") { }
}

public class ShowsController : BaseController<ShowStaff>
{
    public ShowsController(AppDbContext db)
        : base(db, "IdShow", "Shows y staff", "show",
            descripcion: "Artistas y shows que se pueden sumar a un contrato (DJ, bandas, animación), con su horario y precio de contratación.") { }
}

public class EmpleadosController : BaseController<Empleado>
{
    public EmpleadosController(AppDbContext db)
        : base(db, "IdEmpleado", "Empleados", "empleado",
            descripcion: "Personal que se asigna a cada evento (mozos, bartenders, seguridad, fotografía) y lo que cobra por evento.") { }
}

public class PaquetesController : BaseController<PaqueteEvento>
{
    public PaquetesController(AppDbContext db)
        : base(db, "IdPaquete", "Paquetes de eventos", "paquete",
            descripcion: "Los paquetes que ven los clientes en el inicio: precio base, capacidad, duración, qué incluye y coordinador.") { }

    protected override async Task Cargar() =>
        ViewData["IdCoordinador"] = new SelectList(
            await _db.Coordinadores.OrderBy(c => c.NombreApellido).ToListAsync(),
            "IdCoordinador", "NombreApellido");
}