namespace PlanificacionEventos.Models
{
    // Datos para dibujar el calendario mensual de disponibilidad
    public class CalendarioVM
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public DateTime Hoy { get; set; }
        public bool EsAdmin { get; set; }
        public bool EsCliente { get; set; }
        public List<DiaCalendario> Dias { get; set; } = new();
        public List<PaqueteEvento> Paquetes { get; set; } = new();

        public DateTime Primero => new(Anio, Mes, 1);
        public int Libres => Dias.Count(d => d.EnMes && !d.Pasado && d.Evento == null);
        public int Reservados => Dias.Count(d => d.EnMes && d.Evento != null);
    }

    public class DiaCalendario
    {
        public DateTime Fecha { get; set; }
        public bool EnMes { get; set; }
        public bool Pasado { get; set; }
        public EventoCalendario? Evento { get; set; } // un evento por día como máximo
        public List<string> Turnos { get; set; } = new(); // turnos de cobro en el salón (solo admin)
    }

    // Lo que se muestra de un evento. Para clientes y visitantes, los datos de otros clientes quedan vacíos.
    public class EventoCalendario
    {
        public int IdContrato { get; set; }
        public bool EsMio { get; set; }
        public string? Cliente { get; set; }
        public string? Paquete { get; set; }
        public string? Horario { get; set; }
        public string? Etapa { get; set; }
        public bool RequiereAccion { get; set; }
        public bool Completo { get; set; }
        public List<string> Personal { get; set; } = new();
        public List<string> Shows { get; set; } = new();
    }
}
