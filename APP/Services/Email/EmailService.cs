using APP.Services.Documents;
using MailKit.Net.Smtp;
using DOMAIN.Entities.Notifications;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace APP.Services.Email;

public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public bool IsConfigured => SmtpSettings.IsConfigured(Environment.GetEnvironmentVariable);

    public void SendMail(string name, string to, string subject, string body, List<(byte[] fileContent, string fileName, string fileType)> attachments)
    {
        var smtp = SmtpSettings.FromEnvironment();

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Kumateck LTD", smtp.Sender));
            message.To.Add(new MailboxAddress(name, to));
            message.Subject = subject;

            var multipart = new Multipart("mixed");
            multipart.Add(HtmlBody(body));
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
            client.Connect(smtp.Host, smtp.Port, false);
            client.Authenticate(smtp.Username, smtp.Password);
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

    /// <summary>
    /// The HTML body, carrying the company logo as an inline image when the body refers to it.
    /// Mail clients block images embedded as data URIs, so documents point at the logo by content id.
    /// </summary>
    private static MimeEntity HtmlBody(string body)
    {
        var html = new TextPart("html") { Text = body };
        if (body?.Contains(CompanyProfile.LogoForEmail, StringComparison.Ordinal) != true || !File.Exists(CompanyProfile.LogoPath))
            return html;

        var logo = new MimePart("image", "png")
        {
            Content = new MimeContent(new MemoryStream(File.ReadAllBytes(CompanyProfile.LogoPath))),
            ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
            ContentTransferEncoding = ContentEncoding.Base64,
            ContentId = CompanyProfile.LogoContentId,
            FileName = "company-logo.png"
        };
        var related = new MultipartRelated { Root = html };
        related.Add(logo);
        return related;
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
