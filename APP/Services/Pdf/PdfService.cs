using DinkToPdf;
using DinkToPdf.Contracts;

namespace APP.Services.Pdf;

public class PdfService(IConverter converter) : IPdfService
{
    public byte[] GeneratePdfFromHtml(string htmlContent)
    {
        var doc = new HtmlToPdfDocument
        {
            GlobalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                DPI = 300
            },
            Objects = {
                new ObjectSettings
                {
                    PagesCount = true,
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8", LoadImages = true }
                }
            }
        };

        return converter.Convert(doc);
    }
    
}