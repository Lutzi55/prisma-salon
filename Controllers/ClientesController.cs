using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;
using PlanificacionEventos.Models;

namespace PlanificacionEventos.Controllers;

public class ClientesController : CrudController<Cliente>
{
    public ClientesController(AppDbContext db)
        : base(db, "IdCliente", "Clientes", "cliente") { }

    protected override async Task Cargar() =>
        ViewData["IdentityUserId"] = new SelectList(
            await _db.Users.OrderBy(u => u.Email).ToListAsync(),
            "Id", "Email");
}