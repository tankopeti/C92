using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options; // Required for IOptions
using MimeKit;
using MimeKit.Text;
using System.Threading.Tasks;

namespace Cloud9_2.Services
{

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSenderOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger; // Optional: For logging

    // Inject IOptions<EmailSenderOptions> and optionally ILogger
    public SmtpEmailSender(IOptions<EmailSenderOptions> optionsAccessor, ILogger<SmtpEmailSender> logger)
    {
        _options = optionsAccessor.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpServer) ||
            string.IsNullOrWhiteSpace(_options.SenderEmail))
        {
            _logger.LogError("Email Sender is not configured. Check {SectionName} in configuration.", EmailSenderOptions.SectionName);
            // Decide if you want to throw an exception or just log and return
            // throw new InvalidOperationException("Email Sender is not configured.");
             return; // Or silently fail in dev? Best to ensure config is present.
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.SenderName ?? _options.SenderEmail, _options.SenderEmail));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;

            message.Body = new TextPart(TextFormat.Html) // Specify HTML format
            {
                Text = htmlMessage
            };

            using (var client = new SmtpClient())
            {
                // Determine SecureSocketOptions based on config or port common practices
                // Port 465 typically uses SslOnConnect.
                // Port 587 or 25 typically use StartTls.
                // Consult your provider's documentation.
                SecureSocketOptions socketOptions = SecureSocketOptions.Auto; // MailKit tries to figure it out
                if (_options.UseSsl) // Explicitly configure SSL if needed (e.g., port 465)
                {
                    socketOptions = SecureSocketOptions.SslOnConnect;
                }
                 // For STARTTLS on ports 587 or 25, often you don't need to set UseSsl=true,
                 // MailKit's Auto or StartTls setting handles it. Test with your provider.
                 // else if (_options.SmtpPort == 587 || _options.SmtpPort == 25)
                 // {
                 //    socketOptions = SecureSocketOptions.StartTls;
                 // }


                _logger.LogInformation("Connecting to SMTP server {Server} on port {Port} using SSL/TLS: {SslTls}",
                                        _options.SmtpServer, _options.SmtpPort, socketOptions);

                await client.ConnectAsync(_options.SmtpServer, _options.SmtpPort, socketOptions);

                // Authenticate if username/password are provided
                if (!string.IsNullOrWhiteSpace(_options.SmtpUser) && !string.IsNullOrWhiteSpace(_options.SmtpPass))
                {
                    await client.AuthenticateAsync(_options.SmtpUser, _options.SmtpPass);
                }

                _logger.LogInformation("Sending email to {Recipient} with subject {Subject}", email, subject);
                await client.SendAsync(message);
                _logger.LogInformation("Email sent successfully to {Recipient}", email);

                await client.DisconnectAsync(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Recipient}. Server: {Server}, Port: {Port}",
                             email, _options.SmtpServer, _options.SmtpPort);
            // Depending on requirements, you might want to re-throw the exception
            // or handle it gracefully (e.g., queue for retry).
            // throw;
        }
    }
}}