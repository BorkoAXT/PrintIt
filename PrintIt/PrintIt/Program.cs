using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Google;
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
            // External authentication
            // =========================
            var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
            var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

            if (!string.IsNullOrWhiteSpace(googleClientId) &&
                !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                builder.Services.AddAuthentication()
                    .AddGoogle(options =>
                    {
                        options.ClientId = googleClientId;
                        options.ClientSecret = googleClientSecret;
                        options.Scope.Add("profile");
                        options.Scope.Add("email");
                    });
            }
            else
            {
                builder.Services.AddAuthentication();
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
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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

            // TEMP: enable detailed startup errors in non-Development as well
            app.UseDeveloperExceptionPage();

            // =========================
            // Middleware
            // =========================
            if (app.Environment.IsDevelopment())
            {
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
            // Seed Roles + Admin
            // =========================
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var roles = new[] { "Admin", "User" };
                    
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                        await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                }

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var adminEmail = "admin@gmail.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    adminUser = new User
                    {
                        UserName = adminEmail,
                        Email = adminEmail
                    };

                    await userManager.CreateAsync(adminUser, "Admin123");
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.MigrateAsync();
            }

            app.Run();
        }
    }
}
public partial class Program { }