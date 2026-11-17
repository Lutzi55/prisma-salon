using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;
using PlanificacionEventos.Models;

namespace PlanificacionEventos.Controllers;

[Authorize]
public class PerfilController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _um;
    private readonly SignInManager<IdentityUser> _sm;

    public PerfilController(AppDbContext db, UserManager<IdentityUser> um, SignInManager<IdentityUser> sm)
    { _db = db; _um = um; _sm = sm; }

    private async Task<PerfilVM> Armar(DatosClienteVM? d = null, CambiarClaveVM? c = null, CambiarCorreoVM? m = null)
    {
        var u = (await _um.GetUserAsync(User))!;
        var cli = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.IdentityUserId == u.Id);
        return new PerfilVM
        {
            Email = u.Email ?? "",
            Rol = User.IsInRole("Administrador") ? "Administrador" : "Cliente",
            EsCliente = cli != null,
            Datos = d ?? (cli == null ? new DatosClienteVM() : new DatosClienteVM
            { RazonSocialApellido = cli.RazonSocialApellido, DomicilioLegal = cli.DomicilioLegal, Telefono = cli.Telefono }),
            Clave = c ?? new CambiarClaveVM(),
            Correo = m ?? new CambiarCorreoVM()
        };
    }

    private async Task Registrar(IdentityUser u, string descripcion, string? detalle = null)
    {
        _db.Auditorias.Add(new Auditoria
        {
            UsuarioId = u.Id,
            UsuarioEmail = u.Email ?? "",
            Entidad = "Usuario",
            EntidadId = u.Id,
            Accion = "Editar",
            Descripcion = descripcion,
            Detalle = detalle,
            Fecha = Reloj.Ahora()
        });
        await _db.SaveChangesAsync();
    }

    public async Task<IActionResult> Index() => View(await Armar());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Datos([Bind(Prefix = "Datos")] DatosClienteVM vm)
    {
        var u = (await _um.GetUserAsync(User))!;
        var cli = await _db.Clientes.FirstOrDefaultAsync(x => x.IdentityUserId == u.Id);
        if (cli == null) return NotFound();
        if (!ModelState.IsValid) return View("Index", await Armar(d: vm));

        cli.RazonSocialApellido = vm.RazonSocialApellido;
        cli.DomicilioLegal = vm.DomicilioLegal;
        cli.Telefono = vm.Telefono;
        await _db.SaveChangesAsync(); // la auditoría automática registra los campos que cambiaron
        TempData["ok"] = "Datos actualizados";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Clave([Bind(Prefix = "Clave")] CambiarClaveVM vm)
    {
        var u = (await _um.GetUserAsync(User))!;
        if (!ModelState.IsValid) return View("Index", await Armar(c: vm));

        var r = await _um.ChangePasswordAsync(u, vm.ClaveActual, vm.ClaveNueva);
        if (!r.Succeeded)
        {
            foreach (var e in r.Errors)
                ModelState.AddModelError(e.Code == "PasswordMismatch" ? "Clave.ClaveActual" : "Clave.ClaveNueva", e.Description);
            return View("Index", await Armar(c: vm));
        }
        await _sm.RefreshSignInAsync(u);
        await Registrar(u, "Cambió su contraseña");
        TempData["ok"] = "Contraseña actualizada";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Correo([Bind(Prefix = "Correo")] CambiarCorreoVM vm)
    {
        var u = (await _um.GetUserAsync(User))!;
        if (!ModelState.IsValid) return View("Index", await Armar(m: vm));

        if (!await _um.CheckPasswordAsync(u, vm.ClaveActual))
            ModelState.AddModelError("Correo.ClaveActual", "La contraseña es incorrecta");
        var otro = await _um.FindByEmailAsync(vm.NuevoCorreo);
        if (otro != null && otro.Id != u.Id)
            ModelState.AddModelError("Correo.NuevoCorreo", "Ese correo ya está registrado");
        if (!ModelState.IsValid) return View("Index", await Armar(m: vm));

        var anterior = u.Email;
        var r1 = await _um.SetEmailAsync(u, vm.NuevoCorreo);
        var r2 = await _um.SetUserNameAsync(u, vm.NuevoCorreo);
        if (!r1.Succeeded || !r2.Succeeded)
        {
            foreach (var e in r1.Errors.Concat(r2.Errors))
                ModelState.AddModelError("Correo.NuevoCorreo", e.Description);
            return View("Index", await Armar(m: vm));
        }
        await _sm.RefreshSignInAsync(u);
        await Registrar(u, "Cambió su correo", $"Correo: {anterior} → {vm.NuevoCorreo}");
        TempData["ok"] = "Correo actualizado";
        return RedirectToAction(nameof(Index));
    }
}