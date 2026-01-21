using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Google;
using PrintIt.Data;
using PrintIt.Models;

namespace PrintIt
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

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

            builder.Services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
                    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                });

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddRazorPages();
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

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
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.MapRazorPages().WithStaticAssets();

            // Seed roles + admin
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var roles = new[] { "Admin", "User" };

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                    }
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

            app.Run();
        }
    }
}
