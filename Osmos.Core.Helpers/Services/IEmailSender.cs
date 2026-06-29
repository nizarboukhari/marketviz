using System.Threading.Tasks;

namespace Osmos.Core.Helpers.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message, Attachment[] attachments = null);
        Task SendEmailsAsync(string[] emails, string subject, string message, Attachment[] attachments = null);
    }

    public class Attachment
    {
        public string Content { get; set; }
        public string Type { get; set; }
        public string Filename { get; set; }
    }
}
