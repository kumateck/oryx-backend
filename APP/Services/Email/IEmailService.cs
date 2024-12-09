using DOMAIN.Entities.Users;

namespace APP.Services.Email;

public interface IEmailService
{ 
        void SendMail(string to, string subject, string body,
        List<(byte[] fileContent, string fileName)> attachments);
}