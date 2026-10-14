using Serilog;
using Zpravodajstvi.Application;
using Zpravodajstvi.Infrastructure;
using Zpravodajstvi.Infrastructure.Data;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// 1. Připojovací řetězec k databázi SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=zpravodajstvi.db";

// 2. Registrace vrstev v DI: aplikační služby (Application) a infrastruktura (Infrastructure) –
//    DbContext, ASP.NET Core Identity s rolemi, repozitáře a služby pro účty, role a soubory.
//    Jinde než tady Presentation na Infrastructure neodkazuje.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// 3. Přidání MVC kontrolerů a pohledů
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 4. Inicializace databáze (migrace) a seeding rolí, účtů i ukázkových článků
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedDataAsync(scope.ServiceProvider);
}

// 5. Konfigurace HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Soubory známé při sestavení (CSS, JS, knihovny) obsluhuje MapStaticAssets. Obrázky nahrané za běhu
// do wwwroot/uploads v jeho seznamu nejsou, proto je vrací UseStaticFiles (jen když nenajde žádný endpoint).
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Routa pro administrátorskou Area
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
