using DOMAIN.Entities.Notifications;

namespace APP.Services.Email;

public interface IEmailService
{ 
        void SendMail(string to, string subject, string body,
        List<(byte[] fileContent, string fileName, string fileType)> attachments);
        void ProcessNotificationData(NotificationDto data);
}