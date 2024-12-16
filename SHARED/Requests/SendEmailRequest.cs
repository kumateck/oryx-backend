namespace SHARED.Requests;

public class SendEmailRequest
{
    public string Subject { get; set; }
    public string Body { get; set; }
    public List<string> Attachments { get; set; } = [];
}