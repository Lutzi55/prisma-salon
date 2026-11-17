using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlanificacionEventos.Models
{
    public class PaqueteEvento
    {
        [Key]
        public int IdPaquete { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio"), StringLength(150)]
        public string Nombre { get; set; } = "";

        [Display(Name = "Horario de referencia")]
        public TimeSpan HorarioReferencia { get; set; }

        [StringLength(300, ErrorMessage = "Máximo 300 caracteres")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Range(0.01, 999999999, ErrorMessage = "Ingresá un precio mayor a 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }

        [Range(1, 2000, ErrorMessage = "Ingresá una capacidad válida")]
        [Display(Name = "Invitados (máximo)")]
        public int CapacidadInvitados { get; set; } = 50;

        [Range(1, 24, ErrorMessage = "Ingresá una duración entre 1 y 24 horas")]
        [Display(Name = "Duración (horas)")]
        public int DuracionHoras { get; set; } = 5;

        [DataType(DataType.MultilineText), StringLength(2000, ErrorMessage = "Máximo 2000 caracteres")]
        [Display(Name = "Qué incluye (un ítem por línea)")]
        public string? Incluye { get; set; }

        [DataType(DataType.MultilineText), StringLength(1500, ErrorMessage = "Máximo 1500 caracteres")]
        [Display(Name = "Descripción detallada (la ve cualquier visitante)")]
        public string? DescripcionDetallada { get; set; }

        [Required(ErrorMessage = "Seleccioná un coordinador")]
        [Display(Name = "Coordinador")]
        public int? IdCoordinador { get; set; }
        public Coordinador? Coordinador { get; set; }

        public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
    }
}