using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;
using PlanificacionEventos.Models;

// Opciones de personalización que el cliente ve en el detalle de cada paquete.
// Crea solo las categorías que falten (por nombre), así no pisa lo que el administrador
// haya cargado o editado. La categoría Fotografía la crea DatosDemo.AgregarFotografiaAsync.
public static class DatosOpciones
{
    public static async Task CargarAsync(AppDbContext db)
    {
        var categorias = new[]
        {
            new CategoriaOpcion
            {
                Nombre = "DJ y sonido", EsMultiple = false, Orden = 1,
                Opciones =
                {
                    new Opcion { Nombre = "Música ambiental", Descripcion = "Playlist curada y sonido ambiente durante todo el evento.", PrecioAdicional = 0 },
                    new Opcion { Nombre = "DJ Tomás Vera", Descripcion = "DJ en vivo con equipo de sonido profesional para la pista.", PrecioAdicional = 350_000m },
                    new Opcion { Nombre = "DJ con cabina premium", Descripcion = "DJ, cabina iluminada y sonido de alta potencia para eventos grandes.", PrecioAdicional = 480_000m }
                }
            },
            new CategoriaOpcion
            {
                Nombre = "Iluminación", EsMultiple = false, Orden = 2,
                Opciones =
                {
                    new Opcion { Nombre = "Iluminación estándar", Descripcion = "Luz cálida general del salón.", PrecioAdicional = 0 },
                    new Opcion { Nombre = "Iluminación arquitectónica cálida", Descripcion = "Luces bañando paredes y columnas para dar profundidad al salón.", PrecioAdicional = 180_000m },
                    new Opcion { Nombre = "Show de luces y pantallas LED", Descripcion = "Robóticas, efectos y pantallas LED para la pista de baile.", PrecioAdicional = 320_000m }
                }
            },
            new CategoriaOpcion
            {
                Nombre = "Paleta de colores", EsMultiple = false, Orden = 3,
                Opciones =
                {
                    new Opcion { Nombre = "Verde bosque y beige", Descripcion = "La paleta de la casa: natural y elegante.", PrecioAdicional = 0, Colores = "#2B3319,#3F4A2A,#E0DDD5,#EBE9E2" },
                    new Opcion { Nombre = "Blanco y dorado", Descripcion = "Clásica y luminosa, ideal para casamientos.", PrecioAdicional = 60_000m, Colores = "#FFFFFF,#F3EBDD,#C9A227,#8C6D1F" },
                    new Opcion { Nombre = "Azul noche y plata", Descripcion = "Sobria y moderna, para eventos de noche.", PrecioAdicional = 60_000m, Colores = "#141E33,#2C3E66,#C0C4CC,#E6E8EC" },
                    new Opcion { Nombre = "Rosa viejo y borgoña", Descripcion = "Romántica y cálida.", PrecioAdicional = 60_000m, Colores = "#C99A9A,#E8CFCB,#7A2A3A,#4A1622" }
                }
            },
            new CategoriaOpcion
            {
                Nombre = "Mobiliario", EsMultiple = true, Orden = 4,
                Opciones =
                {
                    new Opcion { Nombre = "Sillas Tiffany", Descripcion = "Sillas Tiffany para todos los invitados.", PrecioAdicional = 120_000m },
                    new Opcion { Nombre = "Living lounge", Descripcion = "Sillones y mesas bajas para un sector de descanso.", PrecioAdicional = 150_000m },
                    new Opcion { Nombre = "Mesas imperiales", Descripcion = "Mesas largas de madera para cenas compartidas.", PrecioAdicional = 110_000m },
                    new Opcion { Nombre = "Barra de tragos", Descripcion = "Barra con iluminación propia para coctelería.", PrecioAdicional = 90_000m }
                }
            }
        };

        var existentes = await db.CategoriasOpcion.Select(c => c.Nombre).ToListAsync();
        var nuevas = categorias.Where(c => !existentes.Contains(c.Nombre)).ToList();
        if (nuevas.Count == 0) return;

        db.CategoriasOpcion.AddRange(nuevas);
        await db.SaveChangesAsync();
    }
}
