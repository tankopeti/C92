// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Cloud9_2.Models;
using Cloud9_2.Data; // Adjust this to match your ApplicationDbContext namespace
using Microsoft.AspNetCore.SignalR;
using Cloud9_2.Hubs;

namespace Cloud9._2.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<UserActivityHub> _hubContext;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger,
            ApplicationDbContext context,
            IHubContext<UserActivityHub> hubContext)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _context = context;
            _hubContext = hubContext;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Felhasználónév")]
            public string Username { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Emlékezzen rám ezen a gépen?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (returnUrl.Length > 2048)
            {
                _logger.LogWarning("returnUrl too long, truncating to default.");
                returnUrl = Url.Content("~/");
            }
            ReturnUrl = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(Input.Username, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    var user = await _userManager.FindByNameAsync(Input.Username);

                    // Log the login event
                    var activity = new UserActivity
                    {
                        UserId = user.Id,
                        UserName = user.UserName,
                        LoginTime = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.UserActivities.Add(activity);
                    await _context.SaveChangesAsync();

                    // Notify all clients (add logging to confirm)
                    var activeUsers = _context.UserActivities
                                .Where(a => a.IsActive)
                                .Select(a => new { a.UserName, LoginTime = a.LoginTime.ToString("yyyy-MM-dd HH:mm:ss") })
                                .ToList();
                    await _hubContext.Clients.All.SendAsync("ReceiveActiveUsers", activeUsers);
                    

                    if (user != null && user.MustChangePassword.GetValueOrDefault(false))
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return new JsonResult(new { success = true, mustChangePassword = true });
                        }
                        return RedirectToPage("/Account/ChangePassword", new { returnUrl });
                    }
                    if (user != null && user.Disabled.GetValueOrDefault(false))
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "This account is disabled.");
                        return Page();
                    }

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return new JsonResult(new { success = true });
                    }
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return new JsonResult(new { success = true, requiresTwoFactor = true });
                    }
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return new JsonResult(new { success = false, error = "Account locked out." });
                    }
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return new JsonResult(new { success = false, error = "Invalid login attempt." });
                    }
                }
            }

            return Page();
        }
    }
}