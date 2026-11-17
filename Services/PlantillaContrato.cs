using System.Globalization;
using PlanificacionEventos.Models;

public static class PlantillaContrato
{
    public static string Generar(Contrato c)
    {
        var es = new CultureInfo("es-AR");
        var shows = c.ContratoShows.Select(x => x.Show.NombreShow).ToList();
        var listaShows = shows.Any() ? string.Join(", ", shows) : "ninguno";
        var coord = c.Paquete.Coordinador?.NombreApellido ?? "a designar";
        var hoy = DateTime.Today.ToString("d 'de' MMMM 'de' yyyy", es);
        var fecha = c.FechaEvento.ToString("d 'de' MMMM 'de' yyyy", es);
        var hora = c.Paquete.HorarioReferencia.ToString(@"hh\:mm");
        var valor = c.ValorRealAcordado.ToString("N2", es);
        var fotografia = c.ContratoOpciones.Any(x => x.Opcion.Categoria != null && x.Opcion.Categoria.Nombre.Contains("Fotograf"))
    ? "OCTAVA: Fotografía. El servicio incluye la cobertura de un fotógrafo durante el evento y la entrega de 80 fotografías editadas en galería digital. La grabación de video no forma parte de este contrato: se acuerda y se abona directamente entre EL CLIENTE y el fotógrafo, sin intervención de EL SALÓN."
    : null;

        return string.Join("\n\n", new[]
        {
            $"En la ciudad de Santa Rosa, provincia de La Pampa, a {hoy}, entre PRISMA SALÓN, en adelante \"EL SALÓN\", y {c.Cliente.RazonSocialApellido}, con domicilio legal en {c.Cliente.DomicilioLegal} y teléfono {c.Cliente.Telefono}, en adelante \"EL CLIENTE\", se acuerda celebrar el presente contrato de prestación de servicios para eventos, sujeto a las siguientes cláusulas.",
            $"PRIMERA: Objeto. EL SALÓN se obliga a poner a disposición de EL CLIENTE el espacio, el personal y los servicios del paquete \"{c.Paquete.Nombre}\" para el evento del {fecha}, con horario de referencia {hora} hs. El coordinador asignado es {coord}. Shows contratados: {listaShows}.",
            $"SEGUNDA: Valor y forma de pago. El valor total acordado es de $ {valor}. La forma de pago es: {c.FormaPago}. La seña se abona al firmar y el saldo se cancela como máximo 7 días antes del evento.",
            "TERCERA: Reserva de fecha. La fecha queda reservada únicamente con el pago de la seña. EL SALÓN no admite dos eventos contratados en una misma fecha.",
            "CUARTA: Cancelación. Si EL CLIENTE cancela con más de 60 días de anticipación, se reintegra el 70% de lo abonado. Entre 60 y 30 días, se reintegra el 30%. Con menos de 30 días, no hay reintegro.",
            "QUINTA: Personal y horarios. El personal será citado en el horario que EL SALÓN determine. EL CLIENTE se compromete a respetar el horario de finalización pactado.",
            "SEXTA: Responsabilidad. EL CLIENTE responde por los daños que sus invitados ocasionen en las instalaciones. EL SALÓN responde por el cumplimiento de los servicios contratados.",
            "SÉPTIMA: Jurisdicción. Para cualquier controversia, las partes se someten a los tribunales ordinarios de la ciudad de Santa Rosa, La Pampa.",
            "En prueba de conformidad, se firman dos ejemplares de un mismo tenor y a un solo efecto."
        });
    }
}
