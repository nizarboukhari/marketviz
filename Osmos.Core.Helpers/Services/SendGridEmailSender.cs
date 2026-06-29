using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Osmos.Core.Helpers.Services
{
    public class SendGridEmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string message, Attachment[] attachments = null)
        {
            await _ExecuteAsync(_sendGridOptions.Key, subject, message, email, attachments);
        }

        public async Task SendEmailsAsync(string[] emails, string subject, string message, Attachment[] attachments = null)
        {
            await _ExecuteAsync(_sendGridOptions.Key, subject, message, emails, attachments);
        }

        #region internals

        private SendGridOptions _sendGridOptions = null;

        public SendGridEmailSender(IOptions<SendGridOptions> optionsAccessor)
        {
            _sendGridOptions = optionsAccessor.Value;
        }

        private async Task _ExecuteAsync(string apiKey, string subject, string message, string email, Attachment[] attachments = null)
        {
            if (email == null) throw new ArgumentNullException();

            var emails = new string[] { email };
            await _ExecuteAsync(apiKey, subject, message, emails, attachments);
        }

        private async Task _ExecuteAsync(string apiKey, string subject, string message, string[] emails, Attachment[] attachments = null)
        {
            if (apiKey == null || subject == null || emails == null) throw new ArgumentNullException();

            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(_sendGridOptions.From, _sendGridOptions.Name),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };

            msg.AddTos(emails.Select(e => new EmailAddress(e)).ToList());

            if (attachments != null && attachments.Any())
            {
                msg.AddAttachments(attachments.Select(a => new SendGrid.Helpers.Mail.Attachment
                {
                    Content = a.Content,
                    Type = a.Type,
                    Filename = a.Filename
                }).ToList());
            }

            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode != HttpStatusCode.Accepted) throw new Exception();
        }

        #endregion
    }

    public class SendGridOptions
    {
        public string Key { get; set; }
        public string From { get; set; }
        public string Name { get; set; }
    }
}
