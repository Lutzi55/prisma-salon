using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PlanificacionEventos.Models
{
    public class Empleado
    {
        [Key]

        public int IdEmpleado { get; set; }

        [Required(ErrorMessage = "El DNI es obligatorio")]
        [RegularExpression(@"^\d{7,8}$", ErrorMessage = "DNI de 7 u 8 dígitos")]
        public string Dni { get; set; } = "";

        [Required(ErrorMessage = "El nombre es obligatorio"), StringLength(150)]
        [Display(Name = "Nombre y apellido")]
        public string NombreApellido { get; set; } = "";

        [Required(ErrorMessage = "El puesto es obligatorio"), StringLength(100)]
        public string Puesto { get; set; } = "";

        [Range(0.01, 999999999, ErrorMessage = "Ingresá un monto mayor a 0")]
        [Display(Name = "Pago por evento")]
        public decimal PagoPorEvento { get; set; }

        public ICollection<ContratoEmpleado> ContratoEmpleados { get; set; } = new List<ContratoEmpleado>();
    }
}
    