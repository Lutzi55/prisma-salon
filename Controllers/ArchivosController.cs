using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;

namespace PlanificacionEventos.Controllers;

[Authorize]
public class ArchivosController : Controller
{
    private readonly AppDbContext _db;
    private readonly AlmacenArchivos _arch;
    public ArchivosController(AppDbContext db, AlmacenArchivos arch) { _db = db; _arch = arch; }

    private bool Permitido(string? duenio) =>
        User.IsInRole("Administrador") ||
        (duenio != null && duenio == User.FindFirstValue(ClaimTypes.NameIdentifier));

    private static string Limpiar(string s) => string.Concat(s.Where(char.IsLetterOrDigit));

    // En lugar de un error 404, vuelve a la página anterior con un mensaje
    private IActionResult SinArchivo(string mensaje)
    {
        TempData["error"] = mensaje;
        var anterior = Request.Headers.Referer.ToString();
        if (Uri.TryCreate(anterior, UriKind.Absolute, out var u) && Url.IsLocalUrl(u.PathAndQuery))
            return Redirect(u.PathAndQuery);
        return User.IsInRole("Administrador")
            ? RedirectToAction("Index", "Contratos")
            : RedirectToAction("Index", "MisContratos");
    }

    public async Task<IActionResult> Contrato(int id)
    {
        var c = await _db.Contratos.AsNoTracking().Include(x => x.Cliente)
            .FirstOrDefaultAsync(x => x.IdContrato == id);
        if (c == null) return SinArchivo("No encontramos ese contrato");
        if (!Permitido(c.Cliente.IdentityUserId)) return Forbid();

        if (!string.IsNullOrWhiteSpace(c.TextoContrato))
        {
            if (!User.IsInRole("Administrador") && !c.ContratoEnviado) return SinArchivo("El salón todavía está preparando tu contrato");

            byte[]? firma = null;
            if (!string.IsNullOrEmpty(c.RutaFirmaCliente))
            {
                var rf = _arch.Completa(c.RutaFirmaCliente);
                if (System.IO.File.Exists(rf)) firma = await System.IO.File.ReadAllBytesAsync(rf);
            }
            var pdf = GeneradorPdf.Contrato(c.TextoContrato, c.Cliente.RazonSocialApellido,
                firma, c.FirmanteNombre, c.FirmadoEl, c.FirmaHash);
            return File(pdf, "application/pdf",
                $"Contrato_{Limpiar(c.Cliente.RazonSocialApellido)}_{c.FechaEvento:yyyyMMdd}.pdf");
        }

        if (string.IsNullOrEmpty(c.RutaContratoPdf)) return SinArchivo("Este contrato todavía no tiene texto ni PDF cargado");
        var ruta = _arch.Completa(c.RutaContratoPdf);
        return System.IO.File.Exists(ruta) ? PhysicalFile(ruta, "application/pdf") : SinArchivo("No se encontró el PDF del contrato en el servidor");
    }
    public async Task<IActionResult> Comprobante(int id, bool pdf = false)
    {
        var p = await _db.Pagos.AsNoTracking().Include(x => x.Contrato).ThenInclude(x => x.Cliente)
            .FirstOrDefaultAsync(x => x.IdPago == id);
        if (p == null || string.IsNullOrEmpty(p.RutaComprobanteImg)) return SinArchivo("Ese pago no tiene comprobante cargado");
        if (!Permitido(p.Contrato.Cliente.IdentityUserId)) return Forbid();

        var ruta = _arch.Completa(p.RutaComprobanteImg);
        if (!System.IO.File.Exists(ruta)) return SinArchivo("No se encontró el archivo del comprobante en el servidor (puede haberse borrado al recrear la base)");
        var bytes = await System.IO.File.ReadAllBytesAsync(ruta);

        if (pdf)
            return File(GeneradorPdf.ImagenAPdf(bytes, $"Comprobante de pago del {p.FechaPago:dd/MM/yyyy}"),
                "application/pdf", $"Comprobante_{p.IdPago}.pdf");

        return File(bytes, ruta.EndsWith(".png") ? "image/png" : "image/jpeg");
    }
}