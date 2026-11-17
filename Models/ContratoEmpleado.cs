using System.ComponentModel.DataAnnotations;

namespace PlanificacionEventos.Models
{
    public class ContratoEmpleado
    {
        public int IdContrato { get; set; }
        public Contrato Contrato { get; set; } = null!;

        public int IdEmpleado { get; set; }
        public Empleado Empleado { get; set; } = null!;

        public TimeSpan HoraCitacion { get; set; }
    }
}