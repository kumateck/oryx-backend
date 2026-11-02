using APP.Services.Documents;
using MailKit.Net.Smtp;
using MailKit.Security;
using DOMAIN.Entities.Notifications;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace APP.Services.Email;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> logger;
    private readonly Func<SmtpSettings> readSettings;
    private readonly Func<SmtpClient> createClient;

    public EmailService(ILogger<EmailService> logger)
        : this(logger, SmtpSettings.FromEnvironment, () => new SmtpClient()) { }

    internal EmailService(ILogger<EmailService> logger, Func<SmtpSettings> readSettings, Func<SmtpClient> createClient)
    {
        this.logger = logger;
        this.readSettings = readSettings;
        this.createClient = createClient;
    }

    public bool IsConfigured => SmtpSettings.IsConfigured(Environment.GetEnvironmentVariable);

    public void SendMail(string name, string to, string subject, string body, List<(byte[] fileContent, string fileName, string fileType)> attachments)
    {
        var smtp = readSettings();

        var stage = "preparing";
        try
        {
            using var message = new MimeMessage();
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

            using var client = createClient();
            // Retain MailKit's OS-managed TLS defaults so secure TLS 1.3 endpoints can negotiate.
            client.Timeout = 30_000; // Bound inactive network reads/writes, including the SMTP greeting.
            stage = "connecting";
            using (var connectionDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                var security = smtp.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
                client.Connect(smtp.Host, smtp.Port, security, connectionDeadline.Token);
            }
            stage = "authenticating";
            client.Authenticate(smtp.Username, smtp.Password);
            stage = "sending";
            client.Send(message);

            // Send returning means SMTP accepted the message. A failed QUIT must not make callers retry it.
            logger.LogInformation("Email sent to {To}", to);
            stage = "disconnecting";
            try { client.Disconnect(true); }
            catch (Exception ex) when (EmailTransportFailure.Is(ex))
            {
                logger.LogWarning(ex, "SMTP disconnect failed after email was accepted on {Host}:{Port}", smtp.Host, smtp.Port);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email during SMTP {Stage} on {Host}:{Port} ({ErrorType})",
                stage, smtp.Host, smtp.Port, ex.GetType().Name);
            throw;
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
