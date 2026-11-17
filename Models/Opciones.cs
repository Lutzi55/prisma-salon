using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace PlanificacionEventos.Models
{
    public class CategoriaOpcion
    {
        [Key] public int IdCategoria { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio"), StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string Nombre { get; set; } = "";

        [Display(Name = "Permite elegir varias")]
        public bool EsMultiple { get; set; }

        [Range(0, 99, ErrorMessage = "Ingresá un orden entre 0 y 99")]
        public int Orden { get; set; }

        public ICollection<Opcion> Opciones { get; set; } = new List<Opcion>();
    }

    public class Opcion
    {
        [Key] public int IdOpcion { get; set; }

        [Required(ErrorMessage = "Seleccioná una categoría")]
        [Display(Name = "Categoría")]
        public int? IdCategoria { get; set; }
        public CategoriaOpcion? Categoria { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio"), StringLength(120, ErrorMessage = "Máximo 120 caracteres")]
        public string Nombre { get; set; } = "";

        [StringLength(300, ErrorMessage = "Máximo 300 caracteres")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Range(0, 999999999, ErrorMessage = "Ingresá un monto válido")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Precio adicional")]
        public decimal PrecioAdicional { get; set; }

        [RegularExpression(@"^#[0-9A-Fa-f]{6}(,#[0-9A-Fa-f]{6})*$", ErrorMessage = "Usá colores como #2B3319, separados por coma")]
        [Display(Name = "Colores (solo paletas)")]
        public string? Colores { get; set; }

        public bool Activa { get; set; } = true;

        public ICollection<ContratoOpcion> ContratoOpciones { get; set; } = new List<ContratoOpcion>();
    }

    public class ContratoOpcion
    {
        public int IdContrato { get; set; }
        public Contrato Contrato { get; set; } = null!;
        public int IdOpcion { get; set; }
        public Opcion Opcion { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioAcordado { get; set; }
    }
}