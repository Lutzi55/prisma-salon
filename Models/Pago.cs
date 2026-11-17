using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PlanificacionEventos.Models;

namespace PlanificacionEventos.Models
{
    public class Pago
    {
        [Key]
        public int IdPago { get; set; }

        public int IdContrato { get; set; }
        public Contrato Contrato { get; set; } = null!;

        public string? RutaComprobanteImg { get; set; }

        [StringLength(20)]
        public string Estado { get; set; } = "Confirmado";
        public DateTime FechaPago { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [StringLength(100)]
        public string? Concepto { get; set; }

        [Required, StringLength(50)]
        public string MetodoPago { get; set; }

        [DataType(DataType.Date), Display(Name = "Vencimiento")]
        public DateTime? FechaVencimiento { get; set; }


        [StringLength(300)]
        [Display(Name = "Nota")]
        public string? Nota { get; set; }

        [DataType(DataType.Date), Display(Name = "Vencimiento original")]
        public DateTime? FechaVencimientoOriginal { get; set; }

        // La seña se paga solo por transferencia o tarjeta de débito (ver PoliticaPagos)
        [Display(Name = "Es la seña")]
        public bool EsSena { get; set; }

        // Turno para pagar en el salón (efectivo o cheque): fecha y hora acordadas con el cliente
        [Display(Name = "Turno para pagar en el salón")]
        public DateTime? TurnoPago { get; set; }
    }
}