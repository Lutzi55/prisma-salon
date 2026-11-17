using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;
using PlanificacionEventos.Models;

namespace PlanificacionEventos.Controllers
{
    public class HomeController : Controller
    {
        private static readonly string[] Extensiones = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public HomeController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var paquetes = await _db.Paquetes.AsNoTracking()
                .OrderBy(p => p.Precio).Take(6).ToListAsync();

            // Opiniones visibles para la sección Experiencias
            var resenas = await _db.Resenas.AsNoTracking()
                .Where(r => r.Visible)
                .Include(r => r.Cliente)
                .Include(r => r.Contrato).ThenInclude(c => c.Paquete)
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();
            ViewData["Resenas"] = resenas.Take(6).ToList();
            ViewData["Promedio"] = resenas.Count > 0 ? resenas.Average(r => r.Puntaje) : 0d;
            ViewData["CantResenas"] = resenas.Count;

            ViewData["FotosSalon"] = FotosSalon(paquetes, 3);
            ViewData["CapacidadMaxima"] = paquetes.Count > 0 ? paquetes.Max(p => p.CapacidadInvitados) : 0;

            return View(paquetes);
        }

        // Fotos para "El salón": primero las fotos de los paquetes (son fotos reales);
        // si faltan, se completan con las imágenes de la galería
        private List<string> FotosSalon(List<PaqueteEvento> paquetes, int cantidad)
        {
            var fotos = paquetes
                .Where(p => System.IO.File.Exists(Path.Combine(_env.WebRootPath, "img", $"paquete{p.IdPaquete}.jpg")))
                .Select(p => $"/img/paquete{p.IdPaquete}.jpg")
                .ToList();

            var galeria = Path.Combine(_env.WebRootPath, "img", "galeria");
            if (fotos.Count < cantidad && Directory.Exists(galeria))
                fotos.AddRange(Directory.EnumerateFiles(galeria)
                    .Where(f => Extensiones.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .OrderBy(f => f)
                    .Select(f => "/img/galeria/" + Uri.EscapeDataString(Path.GetFileName(f))));

            return fotos.Take(cantidad).ToList();
        }

        public IActionResult Privacy() => View();

        // Página de error en español (errores 500 y códigos como 404 o 403)
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? codigo)
        {
            ViewData["Codigo"] = codigo ?? 500;
            return View();
        }
    }
}
