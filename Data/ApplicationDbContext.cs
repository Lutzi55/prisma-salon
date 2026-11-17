using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Models;

namespace PlanificacionEventos.Data;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) { }

    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Coordinador> Coordinadores { get; set; }
    public DbSet<PaqueteEvento> Paquetes { get; set; }
    public DbSet<ShowStaff> Shows { get; set; }
    public DbSet<Contrato> Contratos { get; set; }
    public DbSet<ContratoShow> ContratosShows { get; set; }
    public DbSet<Empleado> Empleados { get; set; }
    public DbSet<ContratoEmpleado> ContratosEmpleados { get; set; }
    public DbSet<Pago> Pagos { get; set; }
    public DbSet<Auditoria> Auditorias { get; set; }
    public DbSet<CategoriaOpcion> CategoriasOpcion { get; set; }
    public DbSet<Opcion> Opciones { get; set; }
    public DbSet<ContratoOpcion> ContratosOpciones { get; set; }
    public DbSet<Resena> Resenas { get; set; }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<ContratoShow>().HasKey(x => new { x.IdContrato, x.IdShow });
        b.Entity<ContratoEmpleado>().HasKey(x => new { x.IdContrato, x.IdEmpleado });
        b.Entity<ContratoOpcion>().HasKey(x => new { x.IdContrato, x.IdOpcion });
        b.Entity<Cliente>().HasKey(c => c.IdCliente);
        b.Entity<Contrato>().HasKey(c => c.IdContrato);
        b.Entity<Auditoria>().HasKey(a => a.IdAuditoria);
        b.Entity<Coordinador>().HasKey(c => c.IdCoordinador);
        b.Entity<PaqueteEvento>().HasKey(p => p.IdPaquete);
        b.Entity<ShowStaff>().HasKey(s => s.IdShow);
        b.Entity<Empleado>().HasKey(e => e.IdEmpleado);
        b.Entity<Pago>().HasKey(p => p.IdPago);
        b.Entity<Pago>().Property(p => p.Estado).HasDefaultValue("Confirmado");

        // Relaciones explícitas: las claves foráneas se llaman IdX y EF no siempre las
        // reconoce solo por convención (crearía columnas ocultas como ClienteIdCliente)
        b.Entity<Contrato>().HasOne(c => c.Cliente).WithMany(c => c.Contratos).HasForeignKey(c => c.IdCliente);
        b.Entity<Contrato>().HasOne(c => c.Paquete).WithMany(p => p.Contratos).HasForeignKey(c => c.IdPaquete);
        b.Entity<Pago>().HasOne(p => p.Contrato).WithMany(c => c.Pagos).HasForeignKey(p => p.IdContrato);
        b.Entity<PaqueteEvento>().HasOne(p => p.Coordinador).WithMany(c => c.Paquetes).HasForeignKey(p => p.IdCoordinador);
        b.Entity<Opcion>().HasOne(o => o.Categoria).WithMany(c => c.Opciones).HasForeignKey(o => o.IdCategoria);
        b.Entity<ContratoShow>().HasOne(x => x.Contrato).WithMany(c => c.ContratoShows).HasForeignKey(x => x.IdContrato);
        b.Entity<ContratoShow>().HasOne(x => x.Show).WithMany(s => s.ContratoShows).HasForeignKey(x => x.IdShow);
        b.Entity<ContratoEmpleado>().HasOne(x => x.Contrato).WithMany(c => c.ContratoEmpleados).HasForeignKey(x => x.IdContrato);
        b.Entity<ContratoEmpleado>().HasOne(x => x.Empleado).WithMany(e => e.ContratoEmpleados).HasForeignKey(x => x.IdEmpleado);
        b.Entity<ContratoOpcion>().HasOne(x => x.Contrato).WithMany(c => c.ContratoOpciones).HasForeignKey(x => x.IdContrato);
        b.Entity<ContratoOpcion>().HasOne(x => x.Opcion).WithMany(o => o.ContratoOpciones).HasForeignKey(x => x.IdOpcion);
        b.Entity<Resena>().HasOne(r => r.Contrato).WithMany().HasForeignKey(r => r.IdContrato);
        b.Entity<Resena>().HasOne(r => r.Cliente).WithMany().HasForeignKey(r => r.IdCliente);

        // Una sola reseña por contrato
        b.Entity<Resena>().HasIndex(r => r.IdContrato).IsUnique();

        // Todos los decimales con precisión 18 y escala 2
        foreach (var p in b.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(decimal)))
        {
            p.SetPrecision(18);
            p.SetScale(2);
        }

        // Sin borrado en cascada: si hay registros asociados, la base rechaza el borrado
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        // Auditoría → Usuario (AspNetUsers): cada registro sabe qué usuario hizo el cambio.
        // Si se borra la cuenta, el historial queda (UsuarioId pasa a NULL y se conserva el mail).
        b.Entity<Auditoria>().HasOne(a => a.Usuario).WithMany().HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
        b.Entity<Auditoria>().HasIndex(a => a.Fecha);
    }
}