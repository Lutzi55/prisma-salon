using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;


public class ContratoVM
{
    public int IdContrato { get; set; }

    [Required(ErrorMessage = "La fecha del evento es obligatoria")]
    [DataType(DataType.Date), Display(Name = "Fecha del evento")]
    public DateTime? FechaEvento { get; set; }

    [Range(0.01, 999999999, ErrorMessage = "Ingresá un valor mayor a 0")]
    [Display(Name = "Valor acordado")]
    public decimal ValorRealAcordado { get; set; }

    [Required(ErrorMessage = "La forma de pago es obligatoria"), StringLength(50)]
    [Display(Name = "Forma de pago")]
    public string FormaPago { get; set; } = "";

    [Required(ErrorMessage = "Seleccioná un cliente")]
    [Display(Name = "Cliente")]
    public int? IdCliente { get; set; }

    [Required(ErrorMessage = "Seleccioná un paquete")]
    [Display(Name = "Paquete")]
    public int? IdPaquete { get; set; }

    [Display(Name = "Contrato en PDF")]
    public IFormFile? ArchivoPdf { get; set; }
    public string? RutaContratoPdf { get; set; }

    public List<int> ShowsIds { get; set; } = new();
    public List<int> EmpleadosIds { get; set; } = new();

    [Display(Name = "Hora de citación del personal")]
    public TimeSpan HoraCitacion { get; set; } = new TimeSpan(18, 0, 0);

    public List<int> OpcionesIds { get; set; } = new();
}

public class PagoVM
{
    public int IdContrato { get; set; }

    [Range(0.01, 999999999, ErrorMessage = "Ingresá un monto mayor a 0")]
    public decimal Monto { get; set; }

    [DataType(DataType.Date), Display(Name = "Fecha de pago")]
    public DateTime FechaPago { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Elegí el método de pago")]
    [Display(Name = "Método de pago")]
    public string MetodoPago { get; set; } = "";

    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    public string? Concepto { get; set; }

    [Required(ErrorMessage = "Adjuntá el comprobante (JPG o PNG)")]
    public IFormFile? Comprobante { get; set; }
}



public class RedactarVM
{
    public int IdContrato { get; set; }

    [Required(ErrorMessage = "El texto del contrato no puede estar vacío")]
    public string Texto { get; set; } = "";
}


public class DatosClienteVM
{
    [Required(ErrorMessage = "La razón social es obligatoria"), StringLength(150, ErrorMessage = "Máximo 150 caracteres")]
    [Display(Name = "Razón social / Apellido")]
    public string RazonSocialApellido { get; set; } = "";

    [Required(ErrorMessage = "El domicilio es obligatorio"), StringLength(255, ErrorMessage = "Máximo 255 caracteres")]
    [Display(Name = "Domicilio legal")]
    public string DomicilioLegal { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [RegularExpression(@"^\d{8,15}$", ErrorMessage = "Ingresá solo números (8 a 15 dígitos)")]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = "";
}

public class CambiarClaveVM
{
    [Required(ErrorMessage = "Ingresá tu contraseña actual")]
    [DataType(DataType.Password), Display(Name = "Contraseña actual")]
    public string ClaveActual { get; set; } = "";

    [Required(ErrorMessage = "Ingresá la contraseña nueva")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre {2} y {1} caracteres")]
    [DataType(DataType.Password), Display(Name = "Contraseña nueva")]
    public string ClaveNueva { get; set; } = "";

    [Compare("ClaveNueva", ErrorMessage = "Las contraseñas no coinciden")]
    [DataType(DataType.Password), Display(Name = "Repetir contraseña nueva")]
    public string ConfirmarClave { get; set; } = "";
}

public class CambiarCorreoVM
{
    [Required(ErrorMessage = "Ingresá el correo nuevo")]
    [EmailAddress(ErrorMessage = "Ingresá un correo válido")]
    [Display(Name = "Correo nuevo")]
    public string NuevoCorreo { get; set; } = "";

    [Required(ErrorMessage = "Ingresá tu contraseña para confirmar")]
    [DataType(DataType.Password), Display(Name = "Contraseña actual")]
    public string ClaveActual { get; set; } = "";
}

public class PerfilVM
{
    public string Email { get; set; } = "";
    public string Rol { get; set; } = "";
    public bool EsCliente { get; set; }
    public DatosClienteVM Datos { get; set; } = new();
    public CambiarClaveVM Clave { get; set; } = new();
    public CambiarCorreoVM Correo { get; set; } = new();
}

public class FirmarVM
{
    public int IdContrato { get; set; }

    [Required(ErrorMessage = "Escribí tu nombre y apellido"), StringLength(150, ErrorMessage = "Máximo 150 caracteres")]
    [Display(Name = "Nombre y apellido")]
    public string Nombre { get; set; } = "";

    [Display(Name = "Leí el contrato y acepto sus cláusulas")]
    public bool Acepto { get; set; }

    public string? FirmaPng { get; set; }
}

public class EnviarComprobanteVM
{
    public int IdPago { get; set; }

    [DataType(DataType.Date), Display(Name = "Fecha de pago")]
    public DateTime FechaPago { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Elegí el método de pago")]
    [Display(Name = "Método de pago")]
    public string MetodoPago { get; set; } = "";

    // Obligatorio para transferencia, débito o depósito (se valida en el controlador)
    [Display(Name = "Comprobante")]
    public IFormFile? Comprobante { get; set; }

    // Para efectivo o cheque: turno para pagar en el salón
    [DataType(DataType.Date), Display(Name = "Día del turno")]
    public DateTime? TurnoFecha { get; set; }

    [Display(Name = "Hora del turno")]
    public string? TurnoHora { get; set; }

    // Si el pago es la seña, el cliente puede elegir pagar todo de una
    [Display(Name = "Pagar todo de una")]
    public bool PagarTodo { get; set; }

    // Viene de "Personalizar paquete": muestra la pantalla como confirmación del pedido
    public bool Confirmar { get; set; }
}

public class SolicitudVM
{
    public int IdContrato { get; set; }

    [Display(Name = "Es la seña (solo transferencia o tarjeta de débito)")]
    public bool EsSena { get; set; }

    [Range(0.01, 999999999, ErrorMessage = "Ingresá un monto mayor a 0")]
    public decimal Monto { get; set; }

    [Required(ErrorMessage = "Indicá el concepto (ej: Seña, cuota 2)"), StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    public string Concepto { get; set; } = "";

    [Required(ErrorMessage = "Indicá hasta cuándo puede pagarlo")]
    [DataType(DataType.Date), Display(Name = "Vencimiento")]
    public DateTime? FechaVencimiento { get; set; }
}

public class GestionVM
{
    public int IdPago { get; set; }
    public string Accion { get; set; } = "";

    [StringLength(300, ErrorMessage = "Máximo 300 caracteres")]
    [Display(Name = "Motivo")]
    public string? Nota { get; set; }

    [DataType(DataType.Date), Display(Name = "Nuevo vencimiento")]
    public DateTime? NuevoVencimiento { get; set; }
}
// Pedido que arma el cliente desde "Personalizar paquete"
public class PersonalizarVM
{
    public int IdPaquete { get; set; }

    [Required(ErrorMessage = "Elegí la fecha de tu evento")]
    [DataType(DataType.Date), Display(Name = "Fecha del evento")]
    public DateTime? FechaEvento { get; set; }

    // Pago para reservar: "Sena" (30 %) o "Total" (todo de una)
    [Required(ErrorMessage = "Elegí cuánto pagás ahora")]
    [Display(Name = "¿Cuánto pagás ahora?")]
    public string PagoInicial { get; set; } = "Sena";

    // Solo transferencia o tarjeta de débito (PoliticaPagos.MediosSena)
    [Required(ErrorMessage = "Elegí el medio de pago")]
    [Display(Name = "Medio de pago")]
    public string MedioReserva { get; set; } = "Transferencia";

    [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
    [Display(Name = "Comentarios para el salón (opcional)")]
    public string? Comentario { get; set; }

    public List<int> OpcionesIds { get; set; } = new();

    [Display(Name = "Entiendo que para reservar la fecha tengo que pagar la seña")]
    public bool AceptaSena { get; set; }

    // Solo se piden si la cuenta todavía no tiene ficha de cliente
    [StringLength(150, ErrorMessage = "Máximo 150 caracteres")]
    [Display(Name = "Nombre y apellido o razón social")]
    public string? RazonSocial { get; set; }

    [StringLength(255, ErrorMessage = "Máximo 255 caracteres")]
    [Display(Name = "Domicilio")]
    public string? Domicilio { get; set; }

    [RegularExpression(@"^\d{8,15}$", ErrorMessage = "Ingresá solo números (8 a 15 dígitos)")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }
}
