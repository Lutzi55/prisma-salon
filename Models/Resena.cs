using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlanificacionEventos.Models
{
    // Opinión de un cliente sobre un evento ya realizado (una por contrato)
    public class Resena
    {
        [Key]
        public int IdResena { get; set; }

        [Display(Name = "Contrato")]
        public int IdContrato { get; set; }
        [ForeignKey(nameof(IdContrato))]
        public Contrato Contrato { get; set; } = null!;

        [Display(Name = "Cliente")]
        public int IdCliente { get; set; }
        [ForeignKey(nameof(IdCliente))]
        public Cliente Cliente { get; set; } = null!;

        [Range(1, 5, ErrorMessage = "El puntaje va de 1 a 5 estrellas")]
        [Display(Name = "Puntaje")]
        public int Puntaje { get; set; }

        [Required(ErrorMessage = "Escribí tu comentario")]
        [StringLength(800, MinimumLength = 10, ErrorMessage = "El comentario debe tener entre {2} y {1} caracteres")]
        [Display(Name = "Comentario")]
        public string Comentario { get; set; } = "";

        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; }

        [Display(Name = "Visible en el sitio")]
        public bool Visible { get; set; } = true;

        // Respuesta pública del salón (para agradecer o resolver una inconformidad)
        [StringLength(800, ErrorMessage = "Máximo {1} caracteres")]
        [Display(Name = "Respuesta del salón")]
        public string? Respuesta { get; set; }

        [Display(Name = "Fecha de respuesta")]
        public DateTime? FechaRespuesta { get; set; }
    }

    // Lo único que el cliente puede enviar desde el formulario (evita overposting)
    public class ResenaVM
    {
        public int IdContrato { get; set; }

        [Range(1, 5, ErrorMessage = "Elegí de 1 a 5 estrellas")]
        [Display(Name = "Puntaje")]
        public int Puntaje { get; set; } = 5;

        [Required(ErrorMessage = "Escribí tu comentario")]
        [StringLength(800, MinimumLength = 10, ErrorMessage = "El comentario debe tener entre {2} y {1} caracteres")]
        [Display(Name = "Tu comentario")]
        public string Comentario { get; set; } = "";
    }
}
