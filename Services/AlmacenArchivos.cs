using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class AlmacenArchivos
{
    private readonly string _root;
    public AlmacenArchivos(IWebHostEnvironment env) =>
        _root = Path.Combine(env.ContentRootPath, "App_Data");

    public bool Valido(IFormFile f, string[] extensiones, long maxBytes, out string error)
    {
        error = "";
        var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
        if (!extensiones.Contains(ext)) { error = "Formato no permitido. Usá: " + string.Join(", ", extensiones); return false; }
        if (f.Length == 0 || f.Length > maxBytes) { error = $"El archivo debe pesar hasta {maxBytes / 1_000_000} MB"; return false; }
        return true;
    }

    public async Task<string> GuardarAsync(IFormFile f, string carpeta)
    {
        var dir = Path.Combine(_root, carpeta);
        Directory.CreateDirectory(dir);
        var nombre = Guid.NewGuid() + Path.GetExtension(f.FileName).ToLowerInvariant();
        await using var s = File.Create(Path.Combine(dir, nombre));
        await f.CopyToAsync(s);
        return carpeta + "/" + nombre;
    }

    public string Completa(string ruta) => Path.Combine(_root, ruta);

    public void Borrar(string? ruta)
    {
        if (string.IsNullOrEmpty(ruta)) return;
        var p = Completa(ruta);
        if (File.Exists(p)) File.Delete(p);
    }


    public async Task<string> GuardarBytesAsync(byte[] datos, string carpeta, string extension)
    {
        var dir = Path.Combine(_root, carpeta);
        Directory.CreateDirectory(dir);
        var nombre = Guid.NewGuid() + extension;
        await File.WriteAllBytesAsync(Path.Combine(dir, nombre), datos);
        return carpeta + "/" + nombre;
    }
}