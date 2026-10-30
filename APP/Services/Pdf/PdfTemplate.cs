using System.Text;
using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Items.Requisitions;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.QualityAudits;
using DOMAIN.Entities.RndAnalyticalMethods;
using DOMAIN.Entities.RndFormulations;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.RndStabilityStudies;
using DOMAIN.Entities.RndTechnologyTransfers;
using DOMAIN.Entities.RndTrialBatches;
using DOMAIN.Entities.Requisitions;
using SHARED;

namespace APP.Services.Pdf;

public static class PdfTemplate
{
    public static string QualityAuditReportTemplate(QualityAuditDto audit)
    {
        var content = new StringBuilder();

        content.AppendLine($@"
          <!DOCTYPE html>
          <html lang=""en"">
            <head>
              <meta charset=""UTF-8"" />
              <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
              <title>Quality Audit Report</title>
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
                .title {{
                  text-align: center;
                  margin: 10px 0 30px;
                }}
                .title h1 {{
                  font-size: 22px;
                  margin: 0;
                  color: #0070c0;
                }}
                .meta {{
                  font-size: 13px;
                  line-height: 1.7;
                  color: #444;
                  margin-bottom: 20px;
                }}
                .content h2 {{
                  font-size: 16px;
                  color: #333;
                  margin: 24px 0 8px;
                }}
                .table {{
                  width: 100%;
                  border-collapse: collapse;
                  margin-top: 8px;
                }}
                .table th, .table td {{
                  text-align: left;
                  padding: 8px;
                  font-size: 12px;
                  border-bottom: 1px solid #e5e7eb;
                }}
                .table th {{
                  background-color: #0070c0;
                  color: white;
                }}
                .table tr:nth-child(even) {{
                  background-color: #f3f4f6;
                }}
                .footer {{
                  text-align: center;
                  font-size: 11px;
                  color: #777;
                  margin-top: 40px;
                }}
              </style>
            </head>
            <body class=""page-body"">
              <div class=""container"">
                <div class=""title"">
                  <h1>Quality Audit Report - {audit.AuditNumber}</h1>
                </div>
                <div class=""meta"">
                  <p><strong>Title:</strong> {audit.Title}</p>
                  <p><strong>Type:</strong> {audit.Type} &nbsp; <strong>Focus:</strong> {audit.FocusArea} &nbsp; <strong>Status:</strong> {audit.Status}</p>
                  <p><strong>Scope:</strong> {audit.Scope}</p>
                  <p><strong>Scheduled:</strong> {audit.ScheduledStartDate:d} - {audit.ScheduledEndDate:d}</p>
                  <p><strong>Lead Auditor:</strong> {audit.LeadAuditor?.FirstName} {audit.LeadAuditor?.LastName}</p>
                </div>

                <div class=""content"">
                  <h2>Checklist Responses</h2>
                  <table class=""table"">
                    <thead>
                      <tr>
                        <th>Question</th>
                        <th>Response</th>
                        <th>Comments</th>
                      </tr>
                    </thead>
                    <tbody>");

        foreach (var response in audit.ChecklistResponses)
        {
            var question = response.QuestionText ?? response.AdHocQuestionText;
            content.AppendLine($@"
                      <tr>
                        <td>{question}</td>
                        <td>{response.ResponseStatus}</td>
                        <td>{response.Comments}</td>
                      </tr>");
        }

        content.AppendLine($@"
                    </tbody>
                  </table>

                  <h2>Findings</h2>
                  <table class=""table"">
                    <thead>
                      <tr>
                        <th>Title</th>
                        <th>Severity</th>
                        <th>Status</th>
                        <th>CAPA Status</th>
                      </tr>
                    </thead>
                    <tbody>");

        foreach (var finding in audit.Findings)
        {
            content.AppendLine($@"
                      <tr>
                        <td>{finding.Title}</td>
                        <td>{finding.Severity}</td>
                        <td>{finding.Status}</td>
                        <td>{(finding.CorrectiveAction != null ? finding.CorrectiveAction.Status.ToString() : "N/A")}</td>
                      </tr>");
        }

        content.AppendLine($@"
                    </tbody>
                  </table>

                  <h2>Closing</h2>
                  <p>{audit.ClosingMeetingNotes}</p>
                </div>
                <div class=""footer"">
                  <p>&copy; {DateTime.UtcNow.Year} Entrance Pharmaceuticals &amp; Research Centre</p>
                </div>
              </div>
            </body>
          </html>");

        return content.ToString();
    }

    /// <summary>
    /// Rolls up an R&D project's formulation history, trial batches, analytical
    /// methods, stability data, and technology transfer status into one document,
    /// structured along CTD Module 3.2.P.2 (Pharmaceutical Development) lines.
    /// </summary>
    public static string RndDevelopmentReportTemplate(
        RndProjectDto project,
        List<RndFormulationDto> formulations,
        List<RndTrialBatch> trialBatches,
        List<RndAnalyticalMethod> analyticalMethods,
        List<RndStabilityStudy> stabilityStudies,
        List<RndTechnologyTransfer> technologyTransfers
    )
    {
        var content = new StringBuilder();

        content.AppendLine($@"
          <!DOCTYPE html>
          <html lang=""en"">
            <head>
              <meta charset=""UTF-8"" />
              <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
              <title>R&amp;D Development Report</title>
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
                .title {{
                  text-align: center;
                  margin: 10px 0 30px;
                }}
                .title h1 {{
                  font-size: 22px;
                  margin: 0;
                  color: #0070c0;
                }}
                .meta {{
                  font-size: 13px;
                  line-height: 1.7;
                  color: #444;
                  margin-bottom: 20px;
                }}
                .content h2 {{
                  font-size: 16px;
                  color: #333;
                  margin: 24px 0 8px;
                }}
                .table {{
                  width: 100%;
                  border-collapse: collapse;
                  margin-top: 8px;
                }}
                .table th, .table td {{
                  text-align: left;
                  padding: 8px;
                  font-size: 12px;
                  border-bottom: 1px solid #e5e7eb;
                }}
                .table th {{
                  background-color: #0070c0;
                  color: white;
                }}
                .table tr:nth-child(even) {{
                  background-color: #f3f4f6;
                }}
                .footer {{
                  text-align: center;
                  font-size: 11px;
                  color: #777;
                  margin-top: 40px;
                }}
              </style>
            </head>
            <body class=""page-body"">
              <div class=""container"">
                <div class=""title"">
                  <h1>R&amp;D Development Report - {project.Code}</h1>
                </div>
                <div class=""meta"">
                  <p><strong>Title:</strong> {project.Title}</p>
                  <p><strong>Objective:</strong> {project.Objective}</p>
                  <p><strong>Product:</strong> {project.Product?.Name ?? "Not yet assigned"}</p>
                  <p><strong>Department:</strong> {project.Department?.Name} &nbsp; <strong>Status:</strong> {project.Status}</p>
                  <p><strong>Requested By:</strong> {project.RequestedBy?.FirstName} {project.RequestedBy?.LastName}</p>
                </div>

                <div class=""content"">
                  <h2>3.2.P.2.1 Formulation Development History</h2>
                  <table class=""table"">
                    <thead>
                      <tr>
                        <th>Version</th>
                        <th>Status</th>
                        <th>Item Count</th>
                      </tr>
                    </thead>
                    <tbody>");

        foreach (var formulation in formulations)
        {
            content.AppendLine($@"
                      <tr>
                        <td>{formulation.Version}</td>
                        <td>{formulation.Status}</td>
                        <td>{formulation.Items.Count}</td>
                      </tr>");
        }

        content.AppendLine($@"
                    </tbody>
                  </table>

                  <h2>3.2.P.2.2 Manufacturing Process Development (Trial Batches)</h2>
                  <table class=""table"">
                    <thead>
                      <tr>
                        <th>Batch Code</th>
                        <th>Scale</th>
                        <th>Batch Size</th>
                        <th>Status</th>
                        <th>Manufacturing Date</th>
                      </tr>
                    </thead>
                    <tbody>");

        foreach (var batch in trialBatches)
        {
            content.AppendLine($@"
                      <tr>
                        <td>{batch.BatchCode}</td>
                        <td>{batch.ScaleType}</td>
                        <td>{batch.BatchSize}</td>
                        <td>{batch.Status}</td>
                        <td>{batch.ManufacturingDate:d}</td>
                      </tr>");
        }

        content.AppendLine($@"
                    </tbody>
                  </table>

                  <h2>3.2.P.2.3 Analytical Methods</h2>
                  <table class=""table"">
                    <thead>
                      <tr>
                        <th>Method Name</th>
                        <th>Subject</th>
                        <th>Status</th>
                        <th>Validated At</th>
                      </tr>
                    </thead>
                    <tbody>");

        foreach (var method in analyticalMethods)
        {
            var subject = method.Material?.Name ?? method.Product?.Name ?? "N/A";
            content.AppendLine($@"
                      <tr>
                        <td>{method.MethodName}</td>
                        <td>{subject}</td>
                        <td>{method.Status}</td>
                        <td>{method.ValidatedAt:d}</td>
                      </tr>");
        }

        content.AppendLine($@"
                    </tbody>
                  </table>

                  <h2>Stability Data (ICH Q1A)</h2>
                  <table class=""table"">
                    <thead>
                      <tr>
                        <th>Chamber</th>
                        <th>Start Date</th>
                        <th>Status</th>
                        <th>Pull Points Reported</th>
                      </tr>
                    </thead>
                    <tbody>");

        foreach (var study in stabilityStudies)
        {
            var reported = study.PullPoints.Count(p => p.Status == RndStabilityPullPointStatus.Reported);
            content.AppendLine($@"
                      <tr>
                        <td>{study.RndStabilityChamber?.Name}</td>
                        <td>{study.StartDate:d}</td>
                        <td>{study.Status}</td>
                        <td>{reported} / {study.PullPoints.Count}</td>
                      </tr>");
        }

        content.AppendLine($@"
                    </tbody>
                  </table>

                  <h2>Technology Transfer Summary</h2>
                  <table class=""table"">
                    <thead>
                      <tr>
                        <th>Status</th>
                        <th>Completed At</th>
                        <th>Bill of Material</th>
                      </tr>
                    </thead>
                    <tbody>");

        foreach (var transfer in technologyTransfers)
        {
            content.AppendLine($@"
                      <tr>
                        <td>{transfer.Status}</td>
                        <td>{transfer.CompletedAt:d}</td>
                        <td>{(transfer.BillOfMaterialId.HasValue ? transfer.BillOfMaterialId.ToString() : "Not yet promoted")}</td>
                      </tr>");
        }

        content.AppendLine($@"
                    </tbody>
                  </table>
                </div>
                <div class=""footer"">
                  <p>&copy; {DateTime.UtcNow.Year} Entrance Pharmaceuticals &amp; Research Centre</p>
                </div>
              </div>
            </body>
          </html>");

        return content.ToString();
    }
}