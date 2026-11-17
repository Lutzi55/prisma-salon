using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;
using PlanificacionEventos.Models;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// ===================== 1. SERVICIOS =====================

// Claves de protección de datos guardadas en disco (App_Data/claves).
// Sin esto, cada vez que el hosting reinicia la aplicación se generan claves nuevas:
// las sesiones se cierran y los formularios (y el chat) abiertos dejan de funcionar hasta recargar.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "claves")))
    .SetApplicationName("PrismaSalon");

// MVC y Razor Pages (Identity usa Razor Pages)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Servicios propios
builder.Services.AddSingleton<AlmacenArchivos>();

// Asistente con IA (Gemini): HttpClient tipado + caché para limitar consultas
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<AsistenteIA>(c => c.Timeout = TimeSpan.FromSeconds(60))
    // Si no logra conectarse en 10 s, avisa enseguida en vez de quedarse esperando
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        ConnectTimeout = TimeSpan.FromSeconds(20),
        ConnectCallback = ConexionIPv4.ConectarAsync // prueba primero IPv4 (ver Services/ConexionIPv4.cs)
    });

// Base de datos + auditoria
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditoriaInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, o) =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
     .AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>()));

// Identity con roles
builder.Services.AddIdentity<IdentityUser, IdentityRole>(o =>
{
    o.SignIn.RequireConfirmedAccount = false;
    o.Password.RequireNonAlphanumeric = false;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<ErroresIdentityEs>()
    .AddDefaultUI();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Identity/Account/Login";
    o.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

var app = builder.Build();

// ===================== 2. PIPELINE =====================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Páginas de error en español (404, 403, etc.)
app.UseStatusCodePagesWithReExecute("/Home/Error", "?codigo={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

// Decimales con punto (montos de contratos y pagos)
var cultura = new System.Globalization.CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new(cultura),
    SupportedCultures = new[] { cultura },
    SupportedUICultures = new[] { cultura }
});

app.UseRouting();

// Autenticacion SIEMPRE antes de Autorizacion
app.UseAuthentication();
app.UseAuthorization();

// ===================== 3. RUTAS =====================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();   // obligatorio para las vistas de Identity

// ===================== 4. BASE DE DATOS Y DATOS SEMILLA =====================

using (var scope = app.Services.CreateScope())
{
    var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Aplica las migraciones pendientes (en el hosting crea la base sola)
    await db.Database.MigrateAsync();

    foreach (var r in new[] { "Administrador", "Cliente" })
        if (!await rm.RoleExistsAsync(r)) await rm.CreateAsync(new IdentityRole(r));

    async Task Crear(string mail, string pass, string rol)
    {
        if (await um.FindByEmailAsync(mail) == null)
        {
            var u = new IdentityUser { UserName = mail, Email = mail, EmailConfirmed = true };
            await um.CreateAsync(u, pass);
            await um.AddToRoleAsync(u, rol);
        }
    }
    await Crear("admin@eventos.com", "Admin123!", "Administrador");
    await Crear("cliente@eventos.com", "Cliente123!", "Cliente");

    // Primera vez: vincular el usuario cliente y cargar los datos de demostración
    var uc = await um.FindByEmailAsync("cliente@eventos.com");
    if (uc != null && !await db.Clientes.AnyAsync(c => c.IdentityUserId == uc.Id))
    {
        db.Clientes.Add(new Cliente
        {
            RazonSocialApellido = "Cliente de Prueba",
            DomicilioLegal = "Calle 123",
            Telefono = "2954000000",
            IdentityUserId = uc.Id
        });
        await db.SaveChangesAsync();
        await DatosDemo.CargarAsync(db);
        await DatosDemo.CompletarPaquetesAsync(db);
        await DatosDemo.ActualizarTextosPaquetesAsync(db);
        await DatosDemo.AgregarFotografiaAsync(db);
        await DatosDemo.PrepararFirmaYCobroAsync(db);
        await DatosDemo.AgregarPagosVariadosAsync(db);
    }

    // Opciones de personalización (DJ, iluminación, paletas, mobiliario): crea las que falten
    await DatosOpciones.CargarAsync(db);

    // Reseñas de ejemplo (idempotente: si ya hay reseñas no hace nada)
    await DatosResenas.CargarAsync(db);
}

app.Run();
