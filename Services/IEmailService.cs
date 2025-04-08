namespace Cloud9_2.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, 
            string fromEmail = null, List<string> ccEmails = null, List<string> bccEmails = null);
    }
}