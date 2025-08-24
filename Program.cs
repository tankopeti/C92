using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // <-- Add this for IEmailSender
using Cloud9_2.Data;
using Cloud9_2.Hubs;
using Cloud9_2.Models;
using Cloud9_2.Services; // <-- Assuming EmailSenderOptions and SmtpEmailSender are here
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using System.Text.Json.Serialization; // <-- Added to use ReferenceHandler

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; // Handle circular references
        options.JsonSerializerOptions.MaxDepth = 64; // Increase depth if needed
    });

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews(); // Often added alongside Razor Pages if using both
builder.Services.AddControllers();

builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<CustomerCommunicationService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
// builder.Services.AddScoped<ITaskService, TaskService>();
// builder.Services.AddSingleton<ILogger<TaskService>, Logger<TaskService>>();

builder.Services.AddMemoryCache(); // Faster page load
builder.Services.AddScoped<QuoteService>();
builder.Services.AddResponseCaching();
builder.Services.AddScoped<OpenSearchService>();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(OrderProfile));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();

// Configure Hungarian localization
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("hu-HU") };
    options.DefaultRequestCulture = new RequestCulture("hu-HU");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// --- Database Context ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Identity Configuration ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

// --- Authentication Cookie Configuration ---
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(45);
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                                    ? CookieSecurePolicy.SameAsRequest
                                    : CookieSecurePolicy.Always;
});

builder.Services.AddScoped<UserManager<ApplicationUser>>();

// --- Other Services ---
builder.Services.AddSignalR();
builder.Services.AddAuthorization();
builder.Services.AddScoped<LeadService>();

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// --- Build the Application ---
var app = builder.Build();

// --- Configure the HTTP request pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.MapHub<ChatHub>("/chathub");

app.UseHttpsRedirection();
app.UseStaticFiles();

// Use localization
app.UseRequestLocalization();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(endpoints => endpoints.MapControllers());
app.MapRazorPages();
app.MapControllers();
app.MapHub<ReportHub>("/reportHub");
app.MapHub<UserActivityHub>("/userActivityHub");

// --- Seed Data ---
try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILogger<Program>>();

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

        var superAdminEmail = "superadmin@example.com";
        var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
        if (superAdmin == null)
        {
            superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true,
                MustChangePassword = false
            };
            var superAdminPassword = builder.Configuration["SeedAdminPasswords:SuperAdmin"] ?? "SuperAdmin@123";
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

        var adminEmail = "admin@example.com";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                MustChangePassword = false
            };
            var adminPassword = builder.Configuration["SeedAdminPasswords:Admin"] ?? "Admin@123";
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
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database.");
}

app.Run();