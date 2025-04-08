using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, 
            string fromEmail = null, List<string> ccEmails = null, List<string> bccEmails = null)
        {
            _logger.LogInformation("Attempting to send email to {ToEmail}", toEmail);

            if (string.IsNullOrEmpty(_config["Email:Smtp:Host"]))
            {
                _logger.LogError("SMTP Host is not configured in appsettings.json");
                throw new InvalidOperationException("SMTP Host is not configured.");
            }

            var smtpClient = new SmtpClient
            {
                Host = _config["Email:Smtp:Host"],
                Port = int.Parse(_config["Email:Smtp:Port"] ?? "587"),
                EnableSsl = true,
                Credentials = new NetworkCredential(_config["Email:Smtp:Username"], _config["Email:Smtp:Password"])
            };

            var defaultFrom = _config["Email:Smtp:Username"];
            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? defaultFrom),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);
            if (ccEmails != null && ccEmails.Any())
                ccEmails.ForEach(email => mailMessage.CC.Add(email));
            if (bccEmails != null && bccEmails.Any())
                bccEmails.ForEach(email => mailMessage.Bcc.Add(email));

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                throw; // Keep this to propagate errors for now
            }
        }
    }
}