using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;

namespace Cloud9_2.Services
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUser = "tankopeti@gmail.com";
        private readonly string _smtpPass = "helh gysm ariy famn"; // Use App Password, not your real Gmail password!

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("C92", _smtpUser));
            message.To.Add(new MailboxAddress("", toEmail)); // <-- use parameter
            message.Subject = subject;

            message.Body = new TextPart("html")
            {
                Text = body
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
