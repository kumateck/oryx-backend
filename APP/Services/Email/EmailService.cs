using MailKit.Net.Smtp;
using DOMAIN.Entities.Notifications;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace APP.Services.Email;

public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public void SendMail(string name, string to, string subject, string body, List<(byte[] fileContent, string fileName, string fileType)> attachments)
    {
        var username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "erp@entrancepharmaceuticals.com";
        var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
        var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "entrancepharmaceuticals.com";
        var smtpPort = Convert.ToInt32(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587");

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Kumateck LTD", "noreply@kumateck.com"));
            message.To.Add(new MailboxAddress(name, to));
            message.Subject = subject;

            var bodyPart = new TextPart("html")
            {
                Text = body
            };
            var multipart = new Multipart("mixed");
            multipart.Add(bodyPart);
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    var stream = new MemoryStream(attachment.fileContent);
                    var mimePart = new MimePart(attachment.fileType)
                    {
                        Content = new MimeContent(stream),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        ContentTransferEncoding = ContentEncoding.Base64,
                        FileName = attachment.fileName
                    };

                    multipart.Add(mimePart);
                }
            }
            message.Body = multipart;

            var client = new SmtpClient();
            client.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            client.Connect(smtpHost, smtpPort, false);
            client.Authenticate(username, password);
            client.Send(message);
            client.Disconnect(true);

            logger.LogInformation("Email sent to {To}", to);
        }
        catch (Exception ex)
        {
            logger.LogError("Error sending email: {ExMessage}", ex.Message);
            throw new Exception($"Error sending email: {ex.Message}");
        }
    }

    public void ProcessNotificationData(NotificationDto data)
    {
        const string subject = "New Notification";
        foreach (var user in data.Recipients)
        {
            var encode = data.Message;
            SendMail(user.FirstName, user.Email, subject, encode, []);
        }
    }
}