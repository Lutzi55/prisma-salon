using PlanificacionEventos.Models;

// Reglas de cobro del salón, en un solo lugar para poder cambiarlas fácil
public static class PoliticaPagos
{
    // Seña obligatoria para reservar la fecha
    public const int PorcentajeSena = 30;
    public static readonly string[] MediosSena = { "Transferencia", "Tarjeta de débito" };

    // Medios con comprobante (se sube una imagen) y medios en persona (se agenda un turno)
    public static readonly string[] MediosConComprobante = { "Transferencia", "Tarjeta de débito", "Depósito" };
    public static readonly string[] MediosEnPersona = { "Efectivo", "Cheque" };

    // Turnos para pagar en el salón: lunes a viernes, de 10 a 18 (cada 30 minutos)
    public static readonly TimeSpan TurnoDesde = new(10, 0, 0);
    public static readonly TimeSpan TurnoHasta = new(18, 0, 0);
    public const string TextoHorario = "de lunes a viernes, de 10 a 18 h";

    // Plazos: la seña vence en 7 días (reserva la fecha); el saldo, recién 10 días antes del evento
    public const int DiasParaSena = 7;
    public const int DiasAntesDelEvento = 10;

    public static DateTime VencimientoSena() => Reloj.Ahora().Date.AddDays(DiasParaSena);

    // Nunca menos de una semana desde hoy, aunque el evento esté cerca
    public static DateTime VencimientoSaldo(DateTime fechaEvento)
    {
        var minimo = Reloj.Ahora().Date.AddDays(7);
        var limite = fechaEvento.Date.AddDays(-DiasAntesDelEvento);
        return limite > minimo ? limite : minimo;
    }

    public static decimal Sena(decimal valor) => Math.Round(valor * PorcentajeSena / 100m, 0);

    public static bool EsEnPersona(string? metodo) => metodo != null && MediosEnPersona.Contains(metodo);

    // esReserva: la seña, o cualquier pago mientras todavía no hay nada confirmado (la fecha no está reservada)
    public static string[] MediosPermitidos(bool esReserva) =>
        esReserva ? MediosSena : MediosConComprobante.Concat(MediosEnPersona).ToArray();

    // Necesita p.Contrato.Pagos cargado
    public static bool EsReserva(Pago p) =>
        p.EsSena || !p.Contrato.Pagos.Any(x => x.IdPago != p.IdPago && x.Estado == "Confirmado");

    // Al pedir un paquete, el cliente elige pagar la seña o el total
    public const string ConceptoSena = "Reserva: seña del 30 %";
    public const string ConceptoTotal = "Reserva: pago total del evento";

    public static IEnumerable<string> HorariosTurno()
    {
        for (var h = TurnoDesde; h < TurnoHasta; h = h.Add(TimeSpan.FromMinutes(30)))
            yield return h.ToString(@"hh\:mm");
    }

    public const string TextoAviso =
        "Para reservar la fecha se paga una seña del 30 % del valor (o el total, si preferís pagar todo de una), solo por transferencia o tarjeta de débito. " +
        "Hasta que la seña no esté confirmada, la fecha no queda reservada. El saldo vence 10 días antes del evento y se puede pagar por transferencia, débito, depósito, " +
        "o en efectivo o cheque con turno en el salón.";
}
