using System.ComponentModel.DataAnnotations;

namespace PlanificacionEventos.Models
{
    public class ShowStaff
    {
        [Key]
        public int IdShow { get; set; }

        [Required(ErrorMessage = "El artista es obligatorio"), StringLength(150)]
        [Display(Name = "Artista")]
        public string NombreShow{ get; set; } = "";

        [Required(ErrorMessage = "El tipo de show es obligatorio"), StringLength(100)]
        [Display(Name = "Tipo de show")]
        public string TipoShow { get; set; } = "";

        [Display(Name = "Horario")]
        public TimeSpan HorarioShow { get; set; }

        [Range(0.01, 999999999, ErrorMessage = "Ingresá un precio mayor a 0")]
        [Display(Name = "Precio de contratación")]
        public decimal PrecioContratacion { get; set; }


        public ICollection<ContratoShow> ContratoShows { get; set; } = new List<ContratoShow>();
    }

}