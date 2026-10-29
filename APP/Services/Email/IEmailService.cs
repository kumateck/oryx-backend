using DOMAIN.Entities.Notifications;

namespace APP.Services.Email;

public interface IEmailService
{
    /// <summary>False when the API container lacks the SMTP settings, so SendMail would throw.</summary>
    bool IsConfigured { get; }
    void SendMail(string name, string to, string subject, string body,
    List<(byte[] fileContent, string fileName, string fileType)> attachments);
    void ProcessNotificationData(NotificationDto data);
}