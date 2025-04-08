// Location: ./Areas/Identity/Pages/Account/ForgotPassword.cshtml.cs
// (Adjust namespace Cloud9_2.Models and Cloud9._2.Areas... as needed)

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Cloud9_2.Models; // Assuming ApplicationUser is here
using Microsoft.AspNetCore.Antiforgery; // Needed if validating token manually, but [FromBody] often handles it

namespace Cloud9._2.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ForgotPasswordModel> _logger;
        // Antiforgery might be needed if validating token manually in AJAX handler
        // private readonly IAntiforgery _antiforgery;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<ForgotPasswordModel> logger
            /* IAntiforgery antiforgery */) // Inject if needed
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
            // _antiforgery = antiforgery;
        }

        // Bound property for the dedicated Forgot Password form
        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        // Handler for GET request to the ForgotPassword page
        public IActionResult OnGet()
        {
            // Initialize model for the form
            Input = new InputModel();
            return Page();
        }

        // Handler for standard POST from the ForgotPassword.cshtml form
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid) // Validates the Input.Email property
            {
                _logger.LogInformation("Password reset initiated via POST for email: {Email}", Input.Email);

                var user = await _userManager.FindByEmailAsync(Input.Email);
                // Security Check: Don't reveal if user doesn't exist or isn't confirmed
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    _logger.LogWarning("Password reset via POST: Email '{Email}' not found or not confirmed. Redirecting to confirmation.", Input.Email);
                    // ALWAYS redirect to confirmation page, even if user invalid/unconfirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                 _logger.LogInformation("Password reset via POST: Found user {UserId} for email {Email}", user.Id, Input.Email);

                try
                {
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword", // Target page
                        pageHandler: null,
                        values: new { area = "Identity", code }, // Parameters
                        protocol: Request.Scheme); // Use current scheme (http/https)

                    var emailSubject = "Reset Your Password"; // Customize
                    var emailBody = $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>."; // Customize

                    await _emailSender.SendEmailAsync(
                        Input.Email,
                        emailSubject,
                        emailBody);

                     _logger.LogInformation("Password reset email sent successfully via POST for email {Email}", Input.Email);
                }
                catch(Exception ex)
                {
                     _logger.LogError(ex, "Error sending password reset email via POST for email {Email}", Input.Email);
                     // Still redirect to confirmation for security
                }

                // Redirect after standard POST submission
                return RedirectToPage("./ForgotPasswordConfirmation");
            }
            // Model state invalid (e.g., email format wrong), redisplay the page with errors
            return Page();
        }


        // --- AJAX Handler for initiating reset from *another page* (e.g., Login page) ---
        // This handler expects a JSON payload containing the username
        public async Task<IActionResult> OnPostInitiateResetFromLoginAsync([FromBody] ResetRequestPayload payload)
        {
             // Optional: Manually validate antiforgery token if needed
             // try { await _antiforgery.ValidateRequestAsync(HttpContext); }
             // catch (AntiforgeryValidationException e) { return BadRequest("Antiforgery token validation failed."); }

            string username = payload?.Username;

            if (string.IsNullOrWhiteSpace(username))
            {
                 _logger.LogWarning("AJAX password reset request received without username.");
                 // Return generic success even if input is bad, to prevent info leakage
                 return new JsonResult(new { success = true, message = "If an account with that username exists and has a confirmed email, instructions have been sent." });
            }

             _logger.LogInformation("AJAX Password reset initiated for username: {Username}", username);

            var user = await _userManager.FindByNameAsync(username);

            string email = null;
            bool canProceed = false;

            // Check if user exists AND their email is confirmed
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                email = await _userManager.GetEmailAsync(user);
                if (!string.IsNullOrWhiteSpace(email))
                {
                    canProceed = true;
                     _logger.LogInformation("AJAX Password reset: Found user {UserId} with email {Email} for username {Username}", user.Id, email, username);
                }
                else
                {
                     _logger.LogWarning("AJAX Password reset: User '{Username}' found, but has no email address associated.", username);
                }
            }
            else if (user == null)
            {
                 _logger.LogWarning("AJAX Password reset: User '{Username}' not found.", username);
            }
            else // User found but email not confirmed
            {
                 _logger.LogWarning("AJAX Password reset: User '{Username}' found, but email not confirmed.", username);
            }


            if (!canProceed)
            {
                // Return generic success even if user not found/confirmed/no email, to prevent enumeration
                _logger.LogInformation("AJAX Password reset: Cannot proceed for username {Username}. Returning generic success message.", username);
                return new JsonResult(new { success = true, message = "If an account with that username exists and has a confirmed email, instructions to reset your password have been sent." });
            }

            // Proceed with sending the email
            try
            {
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                 var callbackUrl = Url.Page(
                     "/Account/ResetPassword",
                     pageHandler: null,
                     values: new { area = "Identity", code },
                     protocol: Request.Scheme); // Use current scheme

                var emailSubject = "Password Reset Request"; // Customize
                var emailBody = $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>."; // Customize

                // *** This await causes the delay in the response ***
                await _emailSender.SendEmailAsync(email, emailSubject, emailBody);

                 _logger.LogInformation("Password reset email sent successfully via AJAX for username {Username} to {Email}", username, email);

                // Return specific success message after successful sending
                return new JsonResult(new { success = true, message = "Instructions to reset your password have been sent to the email address associated with your account." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email via AJAX for username {Username} to {Email}", username, email);
                // Return generic success even on internal error for security
                return new JsonResult(new { success = true, message = "If an account with that username exists and has a confirmed email, instructions to reset your password have been sent." }); // Safer option
            }
        }

        // Helper class for receiving JSON payload from AJAX request
        public class ResetRequestPayload
        {
            public string Username { get; set; } // Ensure casing matches JSON sent from JS
        }
    }
}