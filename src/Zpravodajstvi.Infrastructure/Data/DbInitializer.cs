using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Infrastructure.Identity;

namespace Zpravodajstvi.Infrastructure.Data;

public static class DbInitializer
{
    public const string RoleAdmin = "Admin";
    public const string RoleRedaktor = "Redaktor";
    public const string RoleCtenar = "Ctenar";

    public static async Task SeedDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Zajištění existence databáze
        await context.Database.EnsureCreatedAsync();

        // 1. Vytvoření 3 rolí: Admin, Redaktor, Ctenar
        string[] roles = [RoleAdmin, RoleRedaktor, RoleCtenar];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Vytvoření výchozích uživatelů pro jednotlivé role
        await EnsureUserAsync(userManager, "admin@zpravodajstvi.cz", "Heslo123!", "Hlavní Administrátor", RoleAdmin);
        await EnsureUserAsync(userManager, "redaktor@zpravodajstvi.cz", "Heslo123!", "Jan Redaktor", RoleRedaktor);
        await EnsureUserAsync(userManager, "ctenar@zpravodajstvi.cz", "Heslo123!", "Petr Čtenář", RoleCtenar);

        // 3. Naplnění ukázkových dat pro veřejný web (články, kategorie, štítky, komentáře)
        if (!await context.Categories.AnyAsync())
        {
            var catTech = new Category { Name = "Technologie" };
            var catDomaci = new Category { Name = "Domácí" };
            var catSport = new Category { Name = "Sport" };
            var catZahranici = new Category { Name = "Zahraničí" };

            await context.Categories.AddRangeAsync(catTech, catDomaci, catSport, catZahranici);

            var tagAi = new Tag { Name = "AI" };
            var tagCesko = new Tag { Name = "Česko" };
            var tagHokej = new Tag { Name = "Hokej" };
            var tagAktualne = new Tag { Name = "Aktuálně" };
            var tagEkonomika = new Tag { Name = "Ekonomika" };

            await context.Tags.AddRangeAsync(tagAi, tagCesko, tagHokej, tagAktualne, tagEkonomika);

            var art1 = new Article
            {
                Title = "Revoluce v umělé inteligenci: Nové modely mění svět vývoje",
                Perex = "Nejnovější generativní modely AI posouvají možnosti programování a automatizace na novou úroveň.",
                Content = "V posledních měsících došlo k bezprecedentnímu pokroku v oblasti umělé inteligence. Nové modely nejen generují kód s vysokou přesností, ale dokáží také asistovat při architektonických rozhodnutích a testování aplikací. Vývojáři po celém světě tak získávají mocného parťáka pro každodenní práci, což výrazně zvyšuje produktivitu i kvalitu dodávaných softwarových řešení.",
                CreatedAt = DateTime.UtcNow.AddHours(-6),
                Category = catTech,
                Tags = new List<Tag> { tagAi, tagAktualne },
                Images = new List<Image>
                {
                    new() { Url = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=1000&auto=format&fit=crop&q=80", Caption = "Abstraktní vizualizace moderní umělé inteligence" }
                },
                Comments = new List<Comment>
                {
                    new()
                    {
                        AuthorName = "Petr Čtenář",
                        Content = "Výborný a přehledný článek. Používám AI asistenty každý den a je to neuvěřitelný posun.",
                        CreatedAt = DateTime.UtcNow.AddHours(-4)
                    },
                    new()
                    {
                        AuthorName = "Jan Redaktor",
                        Content = "Děkujeme za reakci! V dalším článku se podíváme na integraci AI do ASP.NET Core.",
                        CreatedAt = DateTime.UtcNow.AddHours(-2)
                    }
                }
            };

            var art2 = new Article
            {
                Title = "Výstavba nové vysokorychlostní trati v Česku získala klíčové povolení",
                Perex = "Dopravní infrastruktura v České republice se dočká zásadní modernizace s napojením na evropskou síť.",
                Content = "Ministerstvo dopravy a Správa železnic oznámily úspěšné schválení posouzení vlivu na životní prostředí (EIA) pro klíčový úsek budoucí vysokorychlostní železnice. Projekt v budoucnu zkrátí cestovní čas mezi Prahou a Brnem na necelou hodinu. Zahájení prvních stavebních prací je plánováno v horizontu několika let.",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Category = catDomaci,
                Tags = new List<Tag> { tagCesko, tagEkonomika, tagAktualne },
                Images = new List<Image>
                {
                    new() { Url = "https://images.unsplash.com/photo-1474487548417-781cb71495f3?w=1000&auto=format&fit=crop&q=80", Caption = "Moderní rychlovlak na trati" }
                },
                Comments = new List<Comment>
                {
                    new()
                    {
                        AuthorName = "Petr Čtenář",
                        Content = "Už aby to bylo hotové, cestování po D1 je často za trest.",
                        CreatedAt = DateTime.UtcNow.AddHours(-12)
                    }
                }
            };

            var art3 = new Article
            {
                Title = "Český hokejový tým vstoupil do šampionátu přesvědčivým vítězstvím",
                Perex = "Skvělý týmový výkon a čisté konto brankáře zajistily národnímu týmu první tři body do tabulky.",
                Content = "Reprezentanti předvedli od první minuty disciplinovaný a soustředěný výkon. Soupeře přehráli ve všech herních činnostech, klíčové bylo ubráněné oslabení v první třetině a následné dvě rychlé branky při hře pět na pět. Trenér po zápase vyzdvihl obětavost celého kádru.",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                Category = catSport,
                Tags = new List<Tag> { tagHokej, tagAktualne },
                Images = new List<Image>
                {
                    new() { Url = "https://images.unsplash.com/photo-1580748141549-71748dbe0bdc?w=1000&auto=format&fit=crop&q=80", Caption = "Hokejová aréna během zápasu" }
                },
                Comments = new List<Comment>()
            };

            await context.Articles.AddRangeAsync(art1, art2, art3);
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string fullName, string role)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
