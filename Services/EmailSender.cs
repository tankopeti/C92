using Microsoft.AspNetCore.Identity;
using Cloud9_2.Services;
using Cloud9_2.Models;

namespace Cloud9_2.Services
{
    public class EmailSender : IEmailSender<ApplicationUser>
    {
        private readonly IEmailService _emailService;

        public EmailSender(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return _emailService.SendEmailAsync(email, subject, htmlMessage); // Fixed typo here
        }

        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        {
            var subject = "Confirm Your Email";
            var htmlMessage = $"<h1>Email Confirmation</h1><p>Please confirm your email by <a href='{confirmationLink}'>clicking here</a>.</p>";
            return _emailService.SendEmailAsync(email, subject, htmlMessage);
        }

        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        {
            var subject = "Reset Your Password";
            var htmlMessage = $"<h1>Password Reset</h1><p>Please reset your password by <a href='{resetLink}'>clicking here</a>.</p>";
            return _emailService.SendEmailAsync(email, subject, htmlMessage);
        }

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        {
            var subject = "Password Reset Code";
            var htmlMessage = $"<h1>Password Reset Code</h1><p>Your password reset code is: <strong>{resetCode}</strong></p>";
            return _emailService.SendEmailAsync(email, subject, htmlMessage);
        }
    }
}