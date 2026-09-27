using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zpravodajstvi.Infrastructure.Data;
using Zpravodajstvi.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// 1. Konfigurace databáze SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=zpravodajstvi.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// 2. Konfigurace ASP.NET Core Identity s rolemi
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// Základní podpora pro MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 3. Inicializace databáze a seeding rolí
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedDataAsync(scope.ServiceProvider);
}

// 4. Konfigurace HTTP pipeline
if (!app.Environment.IsDevelopment())
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

app.Run();