using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public static class GeneradorPdf
{
    private const string Verde = "#2B3319";

    public static byte[] Contrato(string texto, string cliente, byte[]? firma = null,
        string? firmante = null, DateTime? firmadoEl = null, string? huella = null)
    {
        var parrafos = texto.Replace("\r", "")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return Document.Create(doc => doc.Page(p =>
        {
            p.Size(PageSizes.A4);
            p.Margin(50);
            p.DefaultTextStyle(x => x.FontSize(10.5f).LineHeight(1.35f));

            p.Header().Column(h =>
            {
                h.Item().AlignCenter().Text("Prisma Salón").FontSize(22).FontColor(Verde);
                h.Item().AlignCenter().PaddingBottom(14).Text("CONTRATO DE PRESTACIÓN DE SERVICIOS PARA EVENTOS")
                    .FontSize(8.5f).FontColor("#5A5A50");
            });

            p.Content().Column(col =>
            {
                col.Spacing(8);
                foreach (var par in parrafos)
                {
                    var m = Regex.Match(par, @"^([A-ZÁÉÍÓÚÑ]+: [^.]*\.)(.*)$", RegexOptions.Singleline);
                    col.Item().Text(t =>
                    {
                        if (m.Success)
                        {
                            t.Span(m.Groups[1].Value + " ").Bold().FontColor(Verde);
                            t.Span(m.Groups[2].Value.Trim());
                        }
                        else t.Span(par);
                    });
                }

                col.Item().PaddingTop(30).Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Height(60).Text(" ");
                        c.Item().PaddingHorizontal(20).LineHorizontal(1);
                        c.Item().AlignCenter().PaddingTop(3).Text("Prisma Salón");
                    });
                    r.RelativeItem().Column(c =>
                    {
                        if (firma != null) c.Item().Height(60).AlignCenter().Image(firma).FitArea();
                        else c.Item().Height(60).Text(" ");
                        c.Item().PaddingHorizontal(20).LineHorizontal(1);
                        c.Item().AlignCenter().PaddingTop(3).Text(firmante ?? cliente);
                    });
                });

                if (firmadoEl != null)
                    col.Item().PaddingTop(14).Text(
                        $"Firmado electrónicamente por {firmante} el {firmadoEl:dd/MM/yyyy} a las {firmadoEl:HH:mm} hs. " +
                        $"Huella del texto firmado (SHA-256): {huella}")
                        .FontSize(7.5f).FontColor("#5A5A50");
            });

            p.Footer().AlignCenter().Text(t =>
            {
                t.Span("Página ").FontSize(8);
                t.CurrentPageNumber().FontSize(8);
                t.Span(" de ").FontSize(8);
                t.TotalPages().FontSize(8);
            });
        })).GeneratePdf();
    }

    public static byte[] ImagenAPdf(byte[] imagen, string titulo) =>
        Document.Create(doc => doc.Page(p =>
        {
            p.Size(PageSizes.A4);
            p.Margin(40);
            p.Header().Text(titulo).FontSize(14).FontColor(Verde);
            p.Content().PaddingTop(15).Image(imagen).FitArea();
        })).GeneratePdf();
}