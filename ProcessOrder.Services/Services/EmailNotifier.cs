using System.Net.Mail;
using System.Net;
using ProcessOrder.Services.Models.Settings;

namespace ProcessOrder.Services.Services
{
    public class EmailNotifier : IEmailNotifier
    {
        private readonly SmtpSettings _smtpSettings;
        public EmailNotifier(SmtpSettings smtpSettings)
        {
            _smtpSettings = smtpSettings;
        }

        public Task<bool> SendNotificationAsync(string toEmail, string subject, string body)
        {
            using (SmtpClient smtpClient = new SmtpClient(_smtpSettings.SMTPServer))
            {
                smtpClient.Credentials = new NetworkCredential(_smtpSettings.SMTPUserName, _smtpSettings.SMTPPassword);
                smtpClient.EnableSsl = true;

                MailMessage mailMessage = new MailMessage(_smtpSettings.SMTPEmail, toEmail, subject, body);
                smtpClient.SendAsync(mailMessage, default);
            }

            return Task.FromResult(true);
        }
    }
}
