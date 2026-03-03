using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PrintIt.Data;
using PrintIt.Models;
using PrintIt.Services;

namespace PrintIt
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =========================
            // Database
            // =========================
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            // =========================
            // Identity
            // =========================
            builder.Services
                .AddIdentity<User, IdentityRole<Guid>>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.Password.RequiredUniqueChars = 0;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 6;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireLowercase = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddDefaultUI();

            // =========================
            // Authentication (includes Google if configured)
            // =========================
            var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
            var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

            // IMPORTANT: configure auth ONCE, then optionally add Google.
            var authBuilder = builder.Services.AddAuthentication();

            if (!string.IsNullOrWhiteSpace(googleClientId) &&
                !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                authBuilder.AddGoogle(options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                });
            }

            // =========================
            // MVC + Razor Pages
            // =========================
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            builder.Services.AddRazorPages();
            builder.Services.AddControllersWithViews();

            // =========================
            // Cookie + Session
            // =========================
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // better for production
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });

            builder.Services.AddMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // =========================
            // Services (DI)
            // =========================
            builder.Services.AddScoped<IPrintService, PrintService>();
            builder.Services.AddScoped<IWishlistService, WishlistService>();
            builder.Services.AddScoped<IFileService, FileService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<IOrderService, OrderService>();

            var app = builder.Build();

            // =========================
            // Middleware
            // =========================
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            // =========================
            // Routes
            // =========================
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            // =========================
            // DB migrate + seed (SAFE)
            // =========================
            var runDbMigrations = builder.Configuration.GetValue<bool>("RUN_DB_MIGRATIONS");

            if (app.Environment.IsDevelopment() || runDbMigrations)
            {
                try
                {
                    using var scope = app.Services.CreateScope();

                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await db.Database.MigrateAsync();

                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                    var roles = new[] { "Admin", "User" };

                    foreach (var role in roles)
                    {
                        if (!await roleManager.RoleExistsAsync(role))
                            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                    }

                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                    var adminEmail = builder.Configuration["AdminUser:Email"] ?? "admin@gmail.com";
                    var adminPassword = builder.Configuration["AdminUser:Password"] ?? "Admin123";

                    var adminUser = await userManager.FindByEmailAsync(adminEmail);
                    if (adminUser == null)
                    {
                        adminUser = new User
                        {
                            UserName = adminEmail,
                            Email = adminEmail
                        };

                        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                        if (createResult.Succeeded)
                        {
                            await userManager.AddToRoleAsync(adminUser, "Admin");
                        }
                        else
                        {
                            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                            app.Logger.LogError("Failed to create admin user: {Errors}", errors);
                        }
                    }
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ex, "Database migration/seed failed during startup.");

                    // if (!app.Environment.IsDevelopment()) throw;
                }
            }

            app.Run();
        }
    }
}

public partial class Program { }