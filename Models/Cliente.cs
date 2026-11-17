using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace PlanificacionEventos.Models
{
    public class Cliente
    {
        [Key]
        public int IdCliente { get; set; }
        [Required, StringLength(150)] public string RazonSocialApellido { get; set; }
        [Required, StringLength(255)] public string DomicilioLegal { get; set; }
        [Required, StringLength(20)] public string Telefono { get; set; }

        [Display(Name = "Usuario de acceso")]
        public string? IdentityUserId { get; set; }
        public IdentityUser? IdentityUser { get; set; }
        public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
    }

    public class Contrato
    {
        public int IdContrato { get; set; }
        public DateTime FechaEvento { get; set; }
        public decimal ValorRealAcordado { get; set; }
        public string? RutaContratoPdf { get; set; }
        [StringLength(50)] public string FormaPago { get; set; }
        public int IdCliente { get; set; }
        public Cliente Cliente { get; set; }
        public int IdPaquete { get; set; }
        public PaqueteEvento Paquete { get; set; }

        public string? TextoContrato { get; set; }
        public bool ContratoEnviado { get; set; }

        [StringLength(150)]
        [Display(Name = "Firmante")]
        public string? FirmanteNombre { get; set; }

        [Display(Name = "Firmado el")]
        public DateTime? FirmadoEl { get; set; }

        [StringLength(64)]
        [Display(Name = "Huella del texto firmado")]
        public string? FirmaHash { get; set; }

        [Display(Name = "Firma del cliente")]
        public string? RutaFirmaCliente { get; set; }

        // Pedido armado por el cliente desde "Personalizar paquete"
        [Display(Name = "Pedido hecho por el cliente")]
        public bool PedidoPorCliente { get; set; }

        [StringLength(500)]
        [Display(Name = "Comentarios del cliente")]
        public string? NotaCliente { get; set; }

        [NotMapped] public bool Firmado => FirmadoEl != null;
        [NotMapped]
        public string EstadoContrato => Firmado ? "Firmado"
            : string.IsNullOrWhiteSpace(TextoContrato) ? "Sin redactar"
            : ContratoEnviado ? "Enviado" : "Borrador";
        private static readonly string[] Vigentes = { "Confirmado", "Pendiente", "Solicitado", "Reprogramado", "Rechazado" };

        [NotMapped] public decimal Pagado => Pagos.Where(p => p.Estado == "Confirmado").Sum(p => p.Monto);
        [NotMapped] public decimal Saldo => ValorRealAcordado - Pagado;
        [NotMapped] public decimal EnRevision => Pagos.Where(p => p.Estado == "Pendiente").Sum(p => p.Monto);
        [NotMapped] public decimal Solicitado => Pagos.Where(p => p.Estado is "Solicitado" or "Reprogramado" or "Rechazado").Sum(p => p.Monto);
        [NotMapped] public decimal Reembolsado => Pagos.Where(p => p.Estado == "Reembolsado").Sum(p => p.Monto);
        [NotMapped] public decimal PorSolicitar => ValorRealAcordado - Pagos.Where(p => Vigentes.Contains(p.Estado)).Sum(p => p.Monto);
        public ICollection<ContratoShow> ContratoShows { get; set; } = new List<ContratoShow>();
        public ICollection<ContratoEmpleado> ContratoEmpleados { get; set; } = new List<ContratoEmpleado>();
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();

        public ICollection<ContratoOpcion> ContratoOpciones { get; set; } = new List<ContratoOpcion>();
    }

    public class ContratoShow
    {
        public int IdContrato { get; set; }
        public Contrato Contrato { get; set; }
        public int IdShow { get; set; }
        public ShowStaff Show { get; set; }
    }

    public class Auditoria
    {
        public int IdAuditoria { get; set; }

        // Quién hizo el cambio: FK a AspNetUsers (nula si fue el sistema o un visitante sin cuenta).
        // El mail se guarda igual, para que el historial se lea aunque la cuenta cambie o se borre.
        [StringLength(450)] public string? UsuarioId { get; set; }
        public IdentityUser? Usuario { get; set; }
        [Required, StringLength(255)] public string UsuarioEmail { get; set; } = "";
        [Required, StringLength(100)] public string Entidad { get; set; } = "";
        [StringLength(100)] public string EntidadId { get; set; } = "";
        [Required, StringLength(20)] public string Accion { get; set; } = "";
        public DateTime Fecha { get; set; }

        [StringLength(300)] public string? Descripcion { get; set; }
        public string? Detalle { get; set; }
    }
}