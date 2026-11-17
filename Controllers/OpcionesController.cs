using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;
using PlanificacionEventos.Models;

// ABM de las opciones de personalización (usa el mismo CRUD genérico que Coordinadores, Shows, etc.)

public class CategoriasController : BaseController<CategoriaOpcion>
{
    public CategoriasController(AppDbContext db)
        : base(db, "IdCategoria", "Categorías de opciones", "categoría", femenino: true,
            descripcion: "Los grupos en los que se ordenan las opciones de personalización (por ejemplo: DJ y sonido, Iluminación, Paleta de colores, Mobiliario). " +
                         "«Permite varias opciones» indica si el cliente puede elegir más de una dentro del grupo; «Orden» define en qué lugar aparece.") { }
}

public class OpcionesController : BaseController<Opcion>
{
    public OpcionesController(AppDbContext db)
        : base(db, "IdOpcion", "Opciones de personalización", "opción", femenino: true,
            descripcion: "Lo que el cliente puede sumar a su paquete, con su precio adicional (0 = incluida). " +
                         "Al contratar, el precio queda congelado en el contrato aunque después lo cambies acá.") { }

    // El formulario muestra la categoría como lista desplegable
    protected override async Task Cargar() =>
        ViewData["IdCategoria"] = new SelectList(
            await _db.CategoriasOpcion.OrderBy(c => c.Orden).ToListAsync(),
            "IdCategoria", "Nombre");
}
