namespace APP.Services.Pdf;

public interface IPdfService
{ 
    byte[] GeneratePdfFromHtml(string htmlContent);
}