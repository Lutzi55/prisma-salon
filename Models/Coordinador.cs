using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace PlanificacionEventos.Models
{
    public class Coordinador
    {
        public int IdCoordinador { get; set; }

        [Required(ErrorMessage = "El DNI es obligatorio")]
        [RegularExpression(@"^\d{7,8}$", ErrorMessage = "DNI de 7 u 8 dígitos")]
        public string Dni { get; set; } = "";

        [Required(ErrorMessage = "El nombre es obligatorio"), StringLength(150)]
        [Display(Name = "Nombre y apellido")]
        public string NombreApellido { get; set; } = "";

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{8,15}$", ErrorMessage = "Solo números (8 a 15 dígitos)")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = "";

        [Range(1990, 2100, ErrorMessage = "Año inválido")]
        [Display(Name = "Año de inicio")]
        public int AnioInicio { get; set; } = DateTime.Now.Year;

        public ICollection<PaqueteEvento> Paquetes { get; set; } = new List<PaqueteEvento>();
    }
}