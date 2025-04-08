using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // <-- Add this for IEmailSender
using Cloud9_2.Data;
using Cloud9_2.Hubs;
using Cloud9_2.Models;
using Cloud9_2.Services; // <-- Assuming EmailSenderOptions and SmtpEmailSender are here

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews(); // Often added alongside Razor Pages if using both

// --- Database Context ---
// Register ApplicationDbContext (Only need one call)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Identity Configuration ---
// Configure Identity with ApplicationUser and IdentityRole (Keep this comprehensive one)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Note: RequireConfirmedAccount is false. Ensure this aligns with your registration/login flow.
    // The ForgotPassword check for IsEmailConfirmedAsync() will always pass if emails are never confirmed.
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI() // Includes UI pages like Login, Register, ForgotPassword
.AddDefaultTokenProviders(); // Needed for password reset tokens

// --- Email Sender Configuration (NEW) ---
// 1. Configure EmailSenderOptions from the "EmailSender" section in appsettings/secrets.json
builder.Services.Configure<EmailSenderOptions>(
    builder.Configuration.GetSection(EmailSenderOptions.SectionName));

// 2. Register your IEmailSender implementation
// Use AddTransient for SmtpEmailSender as MailKit's SmtpClient is best used transiently
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
// --- End Email Sender Configuration ---

// --- Authentication Cookie Configuration ---
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(45); // Set timeout
    options.SlidingExpiration = true; // Renews the cookie if active
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    // Ensure your site always uses HTTPS in production for SecurePolicy.Always
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                                    ? CookieSecurePolicy.SameAsRequest
                                    : CookieSecurePolicy.Always;
});

// --- Other Services ---
builder.Services.AddSignalR();
builder.Services.AddAuthorization(); // Ensure authorization services are registered

// --- Clean up Redundant Calls ---
// REMOVED: builder.Services.AddDbContext<ApplicationDbContext>(...); // Already added above
// REMOVED: builder.Services.AddIdentityCore<ApplicationUser>(...); // AddIdentity<TUser, TRole> is more complete and already called

// --- Build the Application ---
var app = builder.Build();

// --- Configure the HTTP request pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    // Optional: Add Database Error Page Middleware for development
    // app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication and Authorization middleware MUST be between UseRouting and Map endpoints
app.UseAuthentication(); // <-- Ensures authentication mechanisms are active
app.UseAuthorization();  // <-- Ensures authorization policies are enforced

// --- Map Endpoints ---
app.MapRazorPages();
app.MapControllers(); // Needed if you have API or MVC controllers
app.MapHub<ReportHub>("/reportHub");
app.MapHub<UserActivityHub>("/userActivityHub");


// --- Seed Data ---
// Consider moving seeding to a separate service or using a dedicated library for complex scenarios
try
{
    // Scope is created correctly here after app is built
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILogger<Program>>(); // Get logger for seeding messages

        // Seed roles
        string[] roleNames = { "SuperAdmin", "Admin" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                 if (roleResult.Succeeded)
                 {
                     logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
                 }
                 else
                 {
                     logger.LogError("Failed to create role '{RoleName}': {Errors}", roleName, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                 }
            }
        }

        // Seed SuperAdmin user
        var superAdminEmail = "superadmin@example.com"; // Use configuration ideally
        var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
        if (superAdmin == null)
        {
            superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true, // Set to true if not using confirmation emails
                MustChangePassword = false
                // Add other ApplicationUser properties if needed
            };

            // Use a secure password from configuration or secrets
            var superAdminPassword = builder.Configuration["SeedAdminPasswords:SuperAdmin"] ?? "SuperAdmin@123"; // Example fallback, use config!
            var createResult = await userManager.CreateAsync(superAdmin, superAdminPassword);
            if (createResult.Succeeded)
            {
                logger.LogInformation("User '{Email}' created successfully.", superAdminEmail);
                var addToRoleResult = await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                if (addToRoleResult.Succeeded)
                {
                     logger.LogInformation("User '{Email}' added to role 'SuperAdmin'.", superAdminEmail);
                }
                 else
                 {
                      logger.LogError("Failed to add user '{Email}' to role 'SuperAdmin': {Errors}", superAdminEmail, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
                 }
            }
            else
            {
                logger.LogError("Failed to create user '{Email}': {Errors}", superAdminEmail, string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }

        // Seed Admin user (similar structure as SuperAdmin)
        var adminEmail = "admin@example.com"; // Use configuration ideally
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true, // Set to true if not using confirmation emails
                MustChangePassword = false
                // Add other ApplicationUser properties if needed
            };
            var adminPassword = builder.Configuration["SeedAdminPasswords:Admin"] ?? "Admin@123"; // Example fallback, use config!
            var createResult = await userManager.CreateAsync(admin, adminPassword);
             if (createResult.Succeeded)
            {
                logger.LogInformation("User '{Email}' created successfully.", adminEmail);
                var addToRoleResult = await userManager.AddToRoleAsync(admin, "Admin");
                 if (addToRoleResult.Succeeded)
                {
                     logger.LogInformation("User '{Email}' added to role 'Admin'.", adminEmail);
                }
                 else
                 {
                     logger.LogError("Failed to add user '{Email}' to role 'Admin': {Errors}", adminEmail, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
                 }
            }
            else
            {
                logger.LogError("Failed to create user '{Email}': {Errors}", adminEmail, string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
catch (Exception ex)
{
    // Ensure logger is available or create one directly if needed before app is fully built
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database.");
    // Depending on the severity, you might want to stop the application
    // throw;
}

// --- Run the Application ---
app.Run();