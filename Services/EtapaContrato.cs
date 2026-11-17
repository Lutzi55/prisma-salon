using PlanificacionEventos.Models;

// En qué etapa está un contrato y quién tiene que hacer lo siguiente.
// La usan la pantalla del administrador (Contratos) y la del cliente (Mis contratos),
// así los dos ven el mismo recorrido: Pedido → Contrato → Firma → Pago → Listo.
public static class EtapaContrato
{
    public enum Paso
    {
        RedactarContrato,   // admin: armar el contrato a partir del pedido
        EnviarContrato,     // admin: el contrato está en borrador
        EsperarFirma,       // cliente: leer y firmar
        EnviarRecibo,       // admin: pedirle el pago al cliente
        EsperarPago,        // cliente: pagar y subir el comprobante
        CobrarEnSalon,      // admin: el cliente agendó turno para pagar en efectivo o cheque
        RevisarComprobante, // admin: confirmar o rechazar el comprobante
        Completo            // todo pagado
    }

    public static readonly string[] Pasos = { "Pedido y seña", "Contrato", "Firma", "Pago", "Listo" };

    // El contrato ya está resuelto: firmado (texto) o cargado como PDF
    public static bool ContratoListo(Contrato c)
    {
        var tieneTexto = !string.IsNullOrWhiteSpace(c.TextoContrato);
        return tieneTexto ? c.Firmado : !string.IsNullOrEmpty(c.RutaContratoPdf);
    }

    public static Paso Actual(Contrato c)
    {
        // Lo que tiene que ver con pagos va primero: así la seña elegida al pedir el paquete
        // se paga y se revisa antes de preparar el contrato (sin seña la fecha no queda reservada).
        if (c.EnRevision > 0) return Paso.RevisarComprobante;
        if (c.Pagos.Any(p => EstadoPago.AdmiteComprobante(p.Estado) && p.TurnoPago != null)) return Paso.CobrarEnSalon;
        if (c.Pagado == 0 && c.Solicitado > 0) return Paso.EsperarPago;

        var tieneTexto = !string.IsNullOrWhiteSpace(c.TextoContrato);
        if (!tieneTexto && string.IsNullOrEmpty(c.RutaContratoPdf)) return Paso.RedactarContrato;
        if (tieneTexto && !c.ContratoEnviado) return Paso.EnviarContrato;
        if (tieneTexto && !c.Firmado) return Paso.EsperarFirma;
        if (c.Saldo <= 0) return Paso.Completo;
        if (c.Solicitado > 0) return Paso.EsperarPago;
        return Paso.EnviarRecibo;
    }

    // Índice (0 a 4) del círculo resaltado en la barra de pasos
    public static int Indice(Contrato c)
    {
        var p = Actual(c);
        var esPago = p is Paso.EnviarRecibo or Paso.EsperarPago or Paso.RevisarComprobante or Paso.CobrarEnSalon;
        if (esPago && !ContratoListo(c)) return 0; // pagando la seña, antes del contrato
        return p switch
        {
            Paso.RedactarContrato or Paso.EnviarContrato => 1,
            Paso.EsperarFirma => 2,
            Paso.Completo => 5,
            _ => 3
        };
    }

    public static bool TocaAlAdmin(Paso p) =>
        p is Paso.RedactarContrato or Paso.EnviarContrato or Paso.EnviarRecibo or Paso.RevisarComprobante or Paso.CobrarEnSalon;

    public static bool TocaAlCliente(Paso p) => p is Paso.EsperarFirma or Paso.EsperarPago;

    public static string TextoAdmin(Paso p) => p switch
    {
        Paso.RedactarContrato => "Revisá el pedido y redactá el contrato.",
        Paso.EnviarContrato => "El contrato está en borrador: revisalo y envialo al cliente.",
        Paso.EsperarFirma => "Esperando que el cliente lea y firme el contrato.",
        Paso.EnviarRecibo => "Contrato firmado. Enviale al cliente el recibo con el monto a pagar (la seña si todavía no la pagó, o el saldo).",
        Paso.EsperarPago => "Esperando que el cliente pague y envíe el comprobante (si es la seña, la fecha todavía no está reservada).",
        Paso.RevisarComprobante => "El cliente envió un comprobante: revisalo y confirmalo.",
        Paso.CobrarEnSalon => "El cliente agendó un turno para pagar en el salón. Cuando pague, registrá el cobro.",
        _ => "Pagado en su totalidad. ¡Listo para el evento!"
    };

    public static string TextoCliente(Paso p) => p switch
    {
        Paso.RedactarContrato or Paso.EnviarContrato => "Recibimos tu pedido. Estamos preparando tu contrato.",
        Paso.EsperarFirma => "Tu contrato está listo: leelo y firmalo.",
        Paso.EnviarRecibo => "Contrato firmado. En breve te enviamos el recibo con el monto a pagar.",
        Paso.EsperarPago => "Tenés un pago para hacer: confirmá el medio y subí el comprobante.",
        Paso.RevisarComprobante => "Recibimos tu comprobante. El salón lo está revisando.",
        Paso.CobrarEnSalon => "Tenés un turno para pagar en el salón. Te esperamos.",
        _ => "¡Todo listo! Tu evento está pagado."
    };
}
