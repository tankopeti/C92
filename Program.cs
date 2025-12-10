using Microsoft.EntityFrameworkCore;
  using Microsoft.AspNetCore.Identity;
  using Microsoft.AspNetCore.Identity.UI.Services;
  using Cloud9_2.Data;
  using Cloud9_2.Hubs;
  using Cloud9_2.Models;
  using Cloud9_2.Services;
  using Microsoft.AspNetCore.Localization;
  using System.Globalization;
  using System.Text.Json.Serialization;

  var builder = WebApplication.CreateBuilder(args);
  builder.Configuration.AddEnvironmentVariables();

  builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

  // Add services to the container
  builder.Services.AddControllers()
      .AddJsonOptions(options =>
      {
          options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
          options.JsonSerializerOptions.MaxDepth = 64;
      });

  builder.Services.AddRazorPages();
  builder.Services.AddControllersWithViews();
builder.Services.AddControllers();
  
// Register Twilio settings from appsettings.json
builder.Services.Configure<TwilioSettings>(builder.Configuration.GetSection("Twilio"));

// // Initialize Twilio client (optional but recommended)
var twilio = builder.Configuration.GetSection("Twilio").Get<TwilioSettings>();
Twilio.TwilioClient.Init(twilio.AccountSid, twilio.AuthToken);

builder.Services.AddScoped<TaskPMService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<ResourceService>();
builder.Services.AddScoped<EmailService>();
  builder.Services.AddScoped<IDocumentService, DocumentService>();
  builder.Services.AddScoped<QuoteService>();
  builder.Services.AddScoped<OrderService>();
  builder.Services.AddScoped<CustomerCommunicationService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
  builder.Services.AddScoped<ProductPriceService>();
  builder.Services.AddScoped<ContactService>();
  builder.Services.AddScoped<PartnerProductPriceService>();
  builder.Services.AddMemoryCache();
  builder.Services.AddResponseCaching();
//   builder.Services.AddScoped<OpenSearchService>();
  builder.Services.AddSingleton<MetadataExtractor>();
  builder.Services.AddHttpClient();

//   builder.Services.AddAutoMapper(typeof(OrderProfile));
  builder.Services.AddAutoMapper(typeof(Program));
  builder.Services.AddAutoMapper(typeof(MappingProfile));
  builder.Services.AddHttpContextAccessor();
  builder.Services.AddScoped<IUserService, UserService>();
  
  builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

  builder.Services.Configure<RequestLocalizationOptions>(options =>
  {
      var supportedCultures = new[] { new CultureInfo("hu-HU") };
      options.DefaultRequestCulture = new RequestCulture("hu-HU");
      options.SupportedCultures = supportedCultures;
      options.SupportedUICultures = supportedCultures;
  });

  builder.Services.AddDbContext<ApplicationDbContext>(options =>
      options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

  builder.Services.ConfigureApplicationCookie(options =>
  {
      options.ExpireTimeSpan = TimeSpan.FromMinutes(450);
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
  builder.Services.AddSignalR();
  builder.Services.AddAuthorization();
  builder.Services.AddScoped<LeadService>();

  builder.Services.AddLogging(logging =>
  {
      logging.AddConsole();
      logging.AddDebug();
      logging.SetMinimumLevel(LogLevel.Debug);
  });

  var app = builder.Build();

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
  app.UseRequestLocalization();
  app.UseRouting();
  app.UseAuthentication();
  app.UseAuthorization();

  // Top-level route registrations
  app.MapControllers();
  app.MapRazorPages();
  app.MapHub<ReportHub>("/reportHub");
  app.MapHub<UserActivityHub>("/userActivityHub");

  try
  {
      using (var scope = app.Services.CreateScope())
      {
          var services = scope.ServiceProvider;
          var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
          var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
          var context = services.GetRequiredService<ApplicationDbContext>();
        //   var openSearchService = services.GetRequiredService<OpenSearchService>();
          var logger = services.GetRequiredService<ILogger<Program>>();

          // Seed DocumentTypes with proper async handling
          if (!context.DocumentTypes.Any())
          {
              var types = new[]
              {
                  new DocumentType { Name = "Invoice" },
                  new DocumentType { Name = "Contract" },
                  new DocumentType { Name = "DeliveryNote" },
                  new DocumentType { Name = "Unknown" }
              };
              context.DocumentTypes.AddRange(types);
              try
              {
                  await context.SaveChangesAsync();
                  logger.LogInformation("DocumentTypes seeded successfully: {Types}", string.Join(", ", types.Select(t => t.Name)));
              }
              catch (Exception ex)
              {
                  logger.LogError(ex, "Failed to seed DocumentTypes.");
              }
          }

          // Initialize OpenSearch
        //   await openSearchService.InitializeAsync();

          // Seed Roles and Users (unchanged)
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