using System.Net;
using System.Net.Mail;
using DOMAIN.Entities.Users;
using Microsoft.Extensions.Logging;

namespace APP.Services.Email;

public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public void SendMail(string to, string subject, string body, List<(byte[] fileContent, string fileName)> attachments)
    {
        var username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "admin@kumateck.com";
        var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
        try
        {
            // Configure SMTP client
            var smtpClient = new SmtpClient("smtp.zoho.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            // Create email message
            var mail = new MailMessage
            {
                From = new MailAddress(username, $"Kumateck LTD"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            // Add recipient
            mail.To.Add(to);

            // Add attachments from byte array list
            foreach (var attachment in attachments)
            {
                var stream = new MemoryStream(attachment.fileContent);
                var mailAttachment = new Attachment(stream, attachment.fileName);
                mail.Attachments.Add(mailAttachment);
            }

            // Send email
            smtpClient.Send(mail);

            // Log success
            logger.LogInformation($"Email sent to {to}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error sending email: {ex.Message}");
            throw new Exception($"Error sending email: {ex.Message}");
        }
    }
}