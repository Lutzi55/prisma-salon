using PlanificacionEventos.Models;


public static class EstadoPago
{
    public static string Etiqueta(string e) => e switch
    {
        "Solicitado" => "Para pagar",
        "Reprogramado" => "Reprogramado",
        "Pendiente" => "En revisión",
        "Rechazado" => "Comprobante rechazado",
        "Confirmado" => "Pagado",
        "Reembolsado" => "Reembolsado",
        "Cancelado" => "Cancelado",
        _ => e
    };

    // Estados en los que el cliente puede enviar (o reenviar) su comprobante
    public static bool AdmiteComprobante(string e) => e is "Solicitado" or "Reprogramado" or "Rechazado";
}