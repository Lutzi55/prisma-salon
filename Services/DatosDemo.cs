using Microsoft.EntityFrameworkCore;
using PlanificacionEventos.Data;
using PlanificacionEventos.Models;

public static class DatosDemo
{
    public static async Task CargarAsync(AppDbContext db)
    {
        if (await db.Paquetes.AnyAsync()) return; // ya hay datos: no duplicar

        var hoy = DateTime.Today;

        var coords = new[] {
            new Coordinador { Dni = "28455123", NombreApellido = "Marina Gómez",  Telefono = "2954411001", AnioInicio = 2015 },
            new Coordinador { Dni = "31220987", NombreApellido = "Diego Salinas", Telefono = "2954411002", AnioInicio = 2018 },
            new Coordinador { Dni = "35880456", NombreApellido = "Paula Ortiz",   Telefono = "2954411003", AnioInicio = 2021 },
        };
        db.Coordinadores.AddRange(coords);

        var paquetes = new[] {
            new PaqueteEvento { Nombre = "Celebración de cumpleaños",    Descripcion = "Con recepción de coctelería de autor y ambientación con diseño lumínico arquitectónico", Precio = 1_200_000m, HorarioReferencia = new TimeSpan(16, 0, 0),  Coordinador = coords[2] },
            new PaqueteEvento { Nombre = "Casamiento clásico", Descripcion = "Recepción y cena para hasta 150 invitados",           Precio = 2_600_000m, HorarioReferencia = new TimeSpan(21, 0, 0),  Coordinador = coords[0] },
            new PaqueteEvento { Nombre = "Evento empresarial", Descripcion = "Catering, sonido y pantallas para tu empresa",         Precio = 3_200_000m, HorarioReferencia = new TimeSpan(19, 0, 0),  Coordinador = coords[1] },
            new PaqueteEvento { Nombre = "Casamiento premium", Descripcion = "250 invitados con show en vivo y luces",               Precio = 3_900_000m, HorarioReferencia = new TimeSpan(21, 30, 0), Coordinador = coords[0] },
        };
        db.Paquetes.AddRange(paquetes);

        var shows = new[] {
            new ShowStaff { NombreShow = "DJ Tomás Vera",         TipoShow = "DJ",                 HorarioShow = new TimeSpan(23, 0, 0),  PrecioContratacion = 350_000m },
            new ShowStaff { NombreShow = "Banda Los Tamarindos",  TipoShow = "Banda en vivo",      HorarioShow = new TimeSpan(22, 0, 0),  PrecioContratacion = 800_000m },
            new ShowStaff { NombreShow = "Dúo Acústico Alma",     TipoShow = "Música acústica",    HorarioShow = new TimeSpan(20, 30, 0), PrecioContratacion = 280_000m },
            new ShowStaff { NombreShow = "Show de Luces Prisma",  TipoShow = "Luces y pantallas",  HorarioShow = new TimeSpan(0, 30, 0),  PrecioContratacion = 220_000m },
            new ShowStaff { NombreShow = "Animación Mini Fiesta", TipoShow = "Animación infantil", HorarioShow = new TimeSpan(17, 0, 0),  PrecioContratacion = 150_000m },
        };
        db.Shows.AddRange(shows);

        var emps = new[] {
            new Empleado { Dni = "30111222", NombreApellido = "Romina Acuña",   Puesto = "Encargada de sala", PagoPorEvento = 90_000m },
            new Empleado { Dni = "33222333", NombreApellido = "Julián Medina",  Puesto = "Bartender",         PagoPorEvento = 70_000m },
            new Empleado { Dni = "29333444", NombreApellido = "Carla Benítez",  Puesto = "Mozo",              PagoPorEvento = 55_000m },
            new Empleado { Dni = "36444555", NombreApellido = "Facundo Ibarra", Puesto = "Mozo",              PagoPorEvento = 55_000m },
            new Empleado { Dni = "27555666", NombreApellido = "Sergio Quiroga", Puesto = "Seguridad",         PagoPorEvento = 65_000m },
        };
        db.Empleados.AddRange(emps);

        // Clientes: el de prueba ya existe (lo crea el seed de usuarios) y tiene usuario asociado
        var prueba = await db.Clientes.FirstAsync(c => c.IdentityUserId != null);
        var otros = new[] {
            new Cliente { RazonSocialApellido = "Familia Rodríguez",                DomicilioLegal = "Av. San Martín 450",  Telefono = "2954123456" },
            new Cliente { RazonSocialApellido = "Estudio Contable Paz y Asociados", DomicilioLegal = "Calle Belgrano 1280", Telefono = "2954234567" },
            new Cliente { RazonSocialApellido = "Martina Sosa",                     DomicilioLegal = "Pasaje Mitre 77",     Telefono = "2954345678" },
        };
        db.Clientes.AddRange(otros);

        // Contratos (fechas distintas: la regla de negocio no admite dos eventos el mismo día)
        db.Contratos.AddRange(
            new Contrato
            {
                FechaEvento = hoy.AddDays(60),
                ValorRealAcordado = 2_900_000m,
                FormaPago = "Cuotas",
                Cliente = prueba,
                Paquete = paquetes[1],
                ContratoShows = { new ContratoShow { Show = shows[0] }, new ContratoShow { Show = shows[3] } },
                ContratoEmpleados = {
                    new ContratoEmpleado { Empleado = emps[0], HoraCitacion = new TimeSpan(18, 0, 0) },
                    new ContratoEmpleado { Empleado = emps[2], HoraCitacion = new TimeSpan(18, 0, 0) },
                },
                Pagos = { new Pago { Monto = 600_000m, FechaPago = hoy.AddDays(-15), MetodoPago = "Transferencia", Concepto = "Seña" } }
            },
            new Contrato
            {
                FechaEvento = hoy.AddDays(20),
                ValorRealAcordado = 1_300_000m,
                FormaPago = "Transferencia",
                Cliente = prueba,
                Paquete = paquetes[0],
                ContratoShows = { new ContratoShow { Show = shows[4] } },
                ContratoEmpleados = { new ContratoEmpleado { Empleado = emps[3], HoraCitacion = new TimeSpan(15, 0, 0) } },
                Pagos = { new Pago { Monto = 1_300_000m, FechaPago = hoy.AddDays(-5), MetodoPago = "Transferencia", Concepto = "Pago total" } }
            },
            new Contrato
            {
                FechaEvento = hoy.AddDays(90),
                ValorRealAcordado = 4_700_000m,
                FormaPago = "Cuotas",
                Cliente = otros[0],
                Paquete = paquetes[3],
                ContratoShows = { new ContratoShow { Show = shows[1] }, new ContratoShow { Show = shows[2] }, new ContratoShow { Show = shows[3] } },
                ContratoEmpleados = {
                    new ContratoEmpleado { Empleado = emps[0], HoraCitacion = new TimeSpan(17, 30, 0) },
                    new ContratoEmpleado { Empleado = emps[1], HoraCitacion = new TimeSpan(18, 0, 0) },
                    new ContratoEmpleado { Empleado = emps[4], HoraCitacion = new TimeSpan(19, 0, 0) },
                },
                Pagos = { new Pago { Monto = 1_000_000m, FechaPago = hoy.AddDays(-30), MetodoPago = "Efectivo", Concepto = "Seña" } }
            },
            new Contrato
            {
                FechaEvento = hoy.AddDays(35),
                ValorRealAcordado = 3_400_000m,
                FormaPago = "Transferencia",
                Cliente = otros[1],
                Paquete = paquetes[2],
                ContratoShows = { new ContratoShow { Show = shows[0] } },
                ContratoEmpleados = {
                    new ContratoEmpleado { Empleado = emps[1], HoraCitacion = new TimeSpan(17, 0, 0) },
                    new ContratoEmpleado { Empleado = emps[2], HoraCitacion = new TimeSpan(17, 0, 0) },
                },
                Pagos = { new Pago { Monto = 1_700_000m, FechaPago = hoy.AddDays(-7), MetodoPago = "Transferencia", Concepto = "Seña 50%" } }
            },
            new Contrato
            { // evento ya realizado y pagado: sirve para probar saldos
                FechaEvento = hoy.AddDays(-25),
                ValorRealAcordado = 1_250_000m,
                FormaPago = "Efectivo",
                Cliente = otros[2],
                Paquete = paquetes[0],
                ContratoShows = { new ContratoShow { Show = shows[4] } },
                ContratoEmpleados = { new ContratoEmpleado { Empleado = emps[3], HoraCitacion = new TimeSpan(15, 0, 0) } },
                Pagos = { new Pago { Monto = 1_250_000m, FechaPago = hoy.AddDays(-40), MetodoPago = "Efectivo", Concepto = "Pago total" } }
            }
        );

        await db.SaveChangesAsync();
    }

    public static async Task CompletarPaquetesAsync(AppDbContext db)
    {
        var textos = new (string Clave, int Inv, int Horas, string Larga, string Incluye)[]
        {
        ("cumple", 80, 5,
         "Un cumpleaños pensado para recibir a tus invitados con una recepción cuidada y un ambiente que invita a quedarse.\n\nLa coctelería de autor y la iluminación del salón acompañan cada momento de la noche, de la bienvenida al brindis.",
         "Recepción de bienvenida con coctelería de autor\nAmbientación con diseño lumínico\nTorta y mesa dulce\nMozos y personal de sala\nCoordinador a cargo durante todo el evento"),
        ("clásico", 150, 6,
         "El clásico de los casamientos, con una recepción para dar la bienvenida y una cena servida en un salón preparado para una noche especial.\n\nCada detalle, de las mesas a la música, se coordina con vos desde la planificación hasta el último baile.",
         "Recepción con barra de bienvenida\nCena de tres pasos\nMesa dulce y torta de bodas\nMúsica durante la fiesta\nMozos, bartender y encargada de sala\nCoordinador a cargo desde la planificación"),
        ("empresarial", 120, 5,
         "Un espacio versátil para presentaciones, lanzamientos, cenas de fin de año y encuentros con clientes.\n\nEl salón se adapta a cada formato y un coordinador se ocupa de que todo salga a tiempo.",
         "Catering tipo cóctel o almuerzo\nSonido y micrófonos\nPantalla y proyector\nRecepción y acreditación de invitados\nPersonal de sala y seguridad\nCoordinador a cargo"),
        ("premium", 250, 8,
         "La versión más completa de nuestros casamientos, pensada para grandes celebraciones con música en vivo.\n\nLa ambientación, la iluminación y la atención al invitado están a otro nivel, con un coordinador exclusivo para vos.",
         "Todo lo del casamiento clásico\nShow en vivo con banda\nIluminación y pantallas LED\nBarra de tragos premium\nSeguridad y personal ampliado\nCoordinador exclusivo desde la planificación"),
        };

        foreach (var p in await db.Paquetes.ToListAsync())
        {
            var nombre = p.Nombre.ToLowerInvariant();
            var t = textos.FirstOrDefault(x => nombre.Contains(x.Clave));
            var hay = t.Clave != null;

            if (p.CapacidadInvitados <= 0) p.CapacidadInvitados = hay ? t.Inv : 50;
            if (p.DuracionHoras <= 0) p.DuracionHoras = hay ? t.Horas : 5;
            if (hay && string.IsNullOrWhiteSpace(p.DescripcionDetallada)) p.DescripcionDetallada = t.Larga;
            if (hay && string.IsNullOrWhiteSpace(p.Incluye)) p.Incluye = t.Incluye;
        }
        await db.SaveChangesAsync();
    }



    public static async Task ActualizarTextosPaquetesAsync(AppDbContext db)
    {
        
        var textos = new (string Clave, string Corta, string Larga, string Incluye)[]
        {
        ("sico",
         "Recepción de bienvenida, cena servida y fiesta en un salón preparado para tu noche, con un coordinador que te acompaña desde la planificación hasta el último baile.",
         "El casamiento clásico reúne lo esencial para una noche memorable: una recepción para dar la bienvenida a tus invitados, una cena servida y una pista lista para festejar hasta el final.\n\nEl salón se prepara según el estilo que elijas, y el equipo de sala se ocupa de que cada momento, del primer brindis al corte de la torta, salga como lo imaginaste.\n\nTu coordinador te acompaña desde la planificación, organiza los horarios con todos los proveedores y está presente durante toda la fiesta para que vos solo disfrutes.",
         "Recepción con barra de bienvenida\nCena de tres pasos\nMesa dulce y torta de bodas\nMúsica durante la fiesta\nMozos, bartender y encargada de sala\nCoordinador a cargo desde la planificación"),

        ("empresarial",
         "Un salón versátil para lanzamientos, presentaciones, cenas de fin de año y encuentros con clientes, con catering, sonido y pantallas a tu medida.",
         "Pensado para empresas que quieren recibir bien. El salón se adapta a cada formato: una presentación con auditorio, un cóctel de lanzamiento, un almuerzo de trabajo o la cena de fin de año del equipo.\n\nIncluye el equipamiento técnico necesario para que todo se escuche y se vea con claridad, y un servicio de catering acorde al tipo de encuentro.\n\nUn coordinador se ocupa de la acreditación de invitados, los tiempos del programa y la atención en sala, para que tu equipo se concentre en lo importante.",
         "Catering tipo cóctel o almuerzo\nSonido y micrófonos\nPantalla y proyector\nRecepción y acreditación de invitados\nPersonal de sala y seguridad\nCoordinador a cargo"),

        ("premium",
         "La celebración más completa del salón: recepción, cena, show en vivo y una ambientación con iluminación pensada para una noche inolvidable, con coordinador exclusivo.",
         "La propuesta más completa del salón, pensada para grandes celebraciones. Suma a todo lo del casamiento clásico música en vivo, iluminación escénica y una atención ampliada para cada invitado.\n\nLa ambientación juega con la luz para transformar el salón en los distintos momentos de la noche: la recepción, la cena y la fiesta.\n\nContás con un coordinador exclusivo que trabaja con vos desde el primer día y supervisa cada detalle hasta el cierre del evento.",
         "Todo lo del casamiento clásico\nShow en vivo con banda\nIluminación y pantallas LED\nBarra de tragos premium\nSeguridad y personal ampliado\nCoordinador exclusivo desde la planificación"),
        };

        foreach (var p in await db.Paquetes.ToListAsync())
        {
            var n = p.Nombre.ToLowerInvariant();
            var t = textos.FirstOrDefault(x => n.Contains(x.Clave));
            if (t.Clave == null) continue;
            p.Descripcion = t.Corta;
            p.DescripcionDetallada = t.Larga;
            p.Incluye = t.Incluye;
        }
        await db.SaveChangesAsync();
    }



    public static async Task AgregarFotografiaAsync(AppDbContext db)
    {
        if (!await db.CategoriasOpcion.AnyAsync(c => c.Nombre == "Fotografía"))
            db.CategoriasOpcion.Add(new CategoriaOpcion
            {
                Nombre = "Fotografía",
                EsMultiple = false,
                Orden = 5,
                Opciones = { new Opcion {
                Nombre = "Cobertura fotográfica básica (80 fotos)",
                Descripcion = "Fotógrafo durante el evento y entrega de 80 fotografías editadas en galería digital. El video no está incluido: se acuerda y se abona directamente entre el cliente y el fotógrafo.",
                PrecioAdicional = 220_000m } }
            });

        if (!await db.Empleados.AnyAsync(e => e.Puesto == "Fotógrafo"))
            db.Empleados.Add(new Empleado { Dni = "34666777", NombreApellido = "Camila Paredes", Puesto = "Fotógrafo", PagoPorEvento = 150_000m });

        await db.SaveChangesAsync();
    }

    public static async Task PrepararFirmaYCobroAsync(AppDbContext db)
    {
        var contratos = await db.Contratos
            .Include(c => c.Cliente).Include(c => c.Pagos)
            .Include(c => c.Paquete).ThenInclude(p => p.Coordinador)
            .Include(c => c.ContratoShows).ThenInclude(x => x.Show)
            .Include(c => c.ContratoOpciones).ThenInclude(x => x.Opcion).ThenInclude(o => o.Categoria)
            .Where(c => c.Cliente.IdentityUserId != null && (c.TextoContrato == null || c.TextoContrato == ""))
            .OrderByDescending(c => c.FechaEvento).ToListAsync();

        foreach (var c in contratos) { c.TextoContrato = PlantillaContrato.Generar(c); c.ContratoEnviado = true; }

        var primero = contratos.FirstOrDefault();
        if (primero != null && primero.PorSolicitar > 0 && !primero.Pagos.Any(p => p.Estado == "Solicitado"))
            db.Pagos.Add(new Pago
            {
                IdContrato = primero.IdContrato,
                Monto = Math.Min(700_000m, primero.PorSolicitar),
                Concepto = "Segunda cuota",
                MetodoPago = "A definir",
                Estado = "Solicitado",
                FechaPago = Reloj.Ahora(),
                FechaVencimiento = Reloj.Ahora().Date.AddDays(30)
            });
        await db.SaveChangesAsync();
    }


    public static async Task AgregarPagosVariadosAsync(AppDbContext db)
    {
        if (await db.Pagos.AnyAsync(p => p.Estado == "Reprogramado")) return;
        var c = await db.Contratos.Where(x => x.Cliente.IdentityUserId != null)
            .OrderByDescending(x => x.FechaEvento).FirstOrDefaultAsync();
        if (c == null) return;

        var hoy = Reloj.Ahora().Date;
        Pago P(decimal monto, string concepto, string estado, DateTime? vence, string metodo = "A definir",
               string? nota = null, DateTime? original = null, int dias = 0) => new()
               {
                   IdContrato = c.IdContrato,
                   Monto = monto,
                   Concepto = concepto,
                   Estado = estado,
                   MetodoPago = metodo,
                   FechaVencimiento = vence,
                   Nota = nota,
                   FechaVencimientoOriginal = original,
                   FechaPago = hoy.AddDays(dias)
               };

        db.Pagos.AddRange(
            P(500_000m, "Cuota 2", "Pendiente", hoy.AddDays(5), "Transferencia", dias: -1),
            P(400_000m, "Cuota 3", "Reprogramado", hoy.AddDays(25), nota: "Reprogramado a pedido del cliente", original: hoy.AddDays(10)),
            P(300_000m, "Cuota 4", "Rechazado", hoy.AddDays(8), "Transferencia", "El comprobante no se lee. Enviá una captura completa", dias: -2),
            P(200_000m, "Adicional de mobiliario", "Cancelado", hoy.AddDays(-3), nota: "Se incluyó en el valor del contrato"),
            P(100_000m, "Seña duplicada", "Reembolsado", null, "Transferencia", "Se transfirió dos veces. Devolución realizada", dias: -12));
        await db.SaveChangesAsync();
    }
}