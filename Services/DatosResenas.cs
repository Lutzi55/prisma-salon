using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;
using PlanificacionEventos.Models;

// Reseñas de demostración. Como solo se puede opinar sobre eventos que ya pasaron,
// crea algunos contratos con fecha pasada (ya pagados) y les agrega una reseña.
// También deja un evento pasado SIN reseña al Cliente de Prueba, para mostrar en vivo
// cómo un cliente publica su opinión. Si ya hay reseñas, no hace nada.
public static class DatosResenas
{
    public static async Task CargarAsync(AppDbContext db)
    {
        if (await db.Resenas.AnyAsync()) return;

        var paquetes = await db.Paquetes.AsNoTracking().OrderBy(p => p.Precio).ToListAsync();
        if (paquetes.Count == 0) return;
        var hoy = Reloj.Ahora().Date;

        var ejemplos = new (string Cliente, int Dias, int Puntaje, string Comentario)[]
        {
            ("Familia Rodríguez", 150, 5,
             "Desde la primera reunión nos sentimos acompañados. El coordinador estuvo en cada detalle y la noche salió tal como la imaginamos. La iluminación cálida fue lo que más comentaron los invitados."),
            ("Estudio Contable Paz y Asociados", 95, 4,
             "Hicimos la cena de fin de año del estudio y todo funcionó con puntualidad. Muy práctico poder firmar el contrato y subir los comprobantes desde la web. Solo ajustaríamos el volumen de la música durante la cena."),
            ("Martina Sosa", 60, 5,
             "Mi cumpleaños de 30 fue hermoso. La barra de tragos y el living lounge le dieron otro clima a la fiesta. Súper recomendable, volvería a elegirlos."),
        };

        var i = 0;
        foreach (var ej in ejemplos)
        {
            var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.RazonSocialApellido == ej.Cliente);
            if (cliente == null) continue;

            var paquete = paquetes[i++ % paquetes.Count];
            var contrato = await ContratoPasadoAsync(db, cliente.IdCliente, paquete, hoy.AddDays(-ej.Dias));

            db.Resenas.Add(new Resena
            {
                IdContrato = contrato.IdContrato,
                IdCliente = cliente.IdCliente,
                Puntaje = ej.Puntaje,
                Comentario = ej.Comentario,
                Fecha = contrato.FechaEvento.AddDays(3).AddHours(19),
                Visible = true
            });
            await db.SaveChangesAsync();
        }

        // Evento pasado del Cliente de Prueba, todavía sin opinar (para la demo)
        var prueba = await db.Clientes.FirstOrDefaultAsync(c => c.RazonSocialApellido == "Cliente de Prueba");
        if (prueba != null && !await db.Contratos.AnyAsync(c => c.IdCliente == prueba.IdCliente && c.FechaEvento < hoy))
            await ContratoPasadoAsync(db, prueba.IdCliente, paquetes[0], hoy.AddDays(-20));
    }

    // Contrato ya realizado y pagado en su totalidad
    private static async Task<Contrato> ContratoPasadoAsync(AppDbContext db, int idCliente, PaqueteEvento paquete, DateTime fecha)
    {
        // Regla del salón: un solo evento por fecha
        while (await db.Contratos.AnyAsync(c => c.FechaEvento.Date == fecha.Date))
            fecha = fecha.AddDays(-1);

        var contrato = new Contrato
        {
            FechaEvento = fecha,
            ValorRealAcordado = paquete.Precio,
            FormaPago = "Transferencia",
            IdCliente = idCliente,
            IdPaquete = paquete.IdPaquete
        };
        contrato.Pagos.Add(new Pago
        {
            Monto = paquete.Precio,
            FechaPago = fecha.AddDays(-20),
            MetodoPago = "Transferencia",
            Concepto = "Pago total",
            Estado = "Confirmado"
        });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();
        return contrato;
    }
}
