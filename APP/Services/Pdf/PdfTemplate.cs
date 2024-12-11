using System.Text;
using APP.Extensions;
using DOMAIN.Entities.Requisitions;

namespace APP.Services.Pdf;

public static class PdfTemplate
{
    public static string QuotationRequestTemplate(SupplierQuotationDto quotation)
    {
        var content = new StringBuilder();
        
        content.AppendLine($@"
      <!DOCTYPE html>
      <html lang=""en"">
        <head>
          <meta charset=""UTF-8"" />
          <meta
            name=""viewport""
            content=""width=device-width, initial-scale=1.0"" />
          <title>Sales Quotation Request</title>
          <style>
            .page-body {{
              font-family: Arial, sans-serif;
              margin: 0;
              padding: 0;
              background-color: #ffffff;
              color: #333;
            }}
            .container {{
              max-width: 900px;
              margin: 40px auto;
              background: #ffffff;
            }}
            .header {{
              display: flex;
              justify-content: space-between;
              align-items: center;
              padding-bottom: 20px;
            }}
            .header .logo {{
              max-width: 120px;
            }}
            .header .details {{
              text-align: left;
              font-size: 14px;
              line-height: 1.5;
              color: #555;
            }}
            .title {{
              text-align: center;
              margin: 30px 0;
            }}
            .title h1 {{
              font-size: 24px;
              margin: 0;
              color: #0070c0;
            }}
            .content {{
              margin: 20px 0;
            }}
            .content h2 {{
              font-size: 18px;
              color: #333;
              margin-bottom: 10px;
            }}
            .table {{
              width: 100%;
              border-collapse: collapse;
              margin-top: 20px;
              border-radius: 8px; /* Add rounding */
              overflow: hidden; /* Ensure the rounding is visible */
            }}
            .table th,
            .table td {{
              text-align: left;
              padding: 10px;
            }}
            .table th {{
              background-color: #0070c0;
              color: white;
              font-size: 14px;
              padding: 10px;
            }}
            .table tr:nth-child(even) {{
              background-color: #f3f4f6;
            }}

            .table th:first-child {{
              border-top-left-radius: 8px; /* Round the top-left corner */
            }}
            .table th:last-child {{
              border-top-right-radius: 8px; /* Round the top-right corner */
            }}
            .footer {{
              text-align: center;
              font-size: 12px;
              color: #777;
              margin-top: 40px;
            }}
          </style>
        </head>
        <body class=""page-body"">
          <div class=""container"">
            <div class=""header"">
              <img
                src=""data:image/png;base64, {StringExtensions.ConvertToBase64("wwwroot/images/entrance-logo.png")}""
                alt=""Entrance Logo""
                class=""logo"" />
              <div class=""details"">
                <p>+233559585203</p>
                <p>supplychainmanager@entrance.com</p>
                <p>www.entrancepharmaceuticals.com</p>
              </div>
            </div>
            <div class=""title"">
              <h1>{quotation.Supplier.Name}</h1>
            </div>
            <div class=""content"">
              <h2>Sales Quotation Request</h2>
              <p>
                Kindly provide us with a sales quotation for the following
                items:
              </p>
          <table border='1' cellpadding='5'>
              <thead>
                  <tr>
                      <th>Material Name</th>
                      <th>Request Quantity</th>
                      <th>Unit of Measure</th>
                  </tr>
              </thead>
              <tbody>");
              
              foreach (var item in quotation.Items)
              {
                  content.AppendLine($@"
                  <tr>
                      <td>{item.Material.Name}</td>
                      <td>{item.Quantity}</td>
                      <td>{item.UoM.Name}</td>
                  </tr>");
              }
              content.AppendLine($@"
              </tbody>
          </table>
      </body>
      </html>");
        
        return content.ToString();
    }
}