# Jak funguje Zpravodajství

Tento návod vysvětluje, jak funguje hotový kód aplikace Zpravodajství: z jakých částí se skládá, kudy prochází požadavek a kde v kódu najdete jednotlivé funkce. U každé části je uvedeno, ve kterých souborech leží, a krátká ukázka kódu. Ukázky jsou zkrácené, vynechané části označuje `// ...`. Instalace a spuštění jsou popsané v [README.md](README.md).

## Obsah

1. [Architektura: vrstvy a projekty](#1-architektura-vrstvy-a-projekty)
2. [Start aplikace (Program.cs)](#2-start-aplikace-programcs)
3. [Cesta požadavku: controller, služba, repozitář, view](#3-cesta-požadavku-controller-služba-repozitář-view)
4. [Datový model a DbContext](#4-datový-model-a-dbcontext)
5. [Migrace a výchozí data](#5-migrace-a-výchozí-data)
6. [Články, kategorie, štítky a obrázky](#6-články-kategorie-štítky-a-obrázky)
7. [Komentáře](#7-komentáře)
8. [Účty, role a oprávnění](#8-účty-role-a-oprávnění)
9. [Validace a vlastní validační atributy](#9-validace-a-vlastní-validační-atributy)
10. [Ochrana formulářů antiforgery tokenem](#10-ochrana-formulářů-antiforgery-tokenem)
11. [Vyhledávání s normalizací českých znaků](#11-vyhledávání-s-normalizací-českých-znaků)
12. [Mazání kategorie s články](#12-mazání-kategorie-s-články)
13. [Testy](#13-testy)
14. [Opravené chyby](#14-opravené-chyby)
15. [Úpravy podle kontrolního seznamu](#15-úpravy-podle-kontrolního-seznamu)

---

## 1. Architektura: vrstvy a projekty

Řešení `Zpravodajstvi.sln` (a stejně tak `Zpravodajstvi.slnx`) obsahuje čtyři projekty aplikace a jeden testovací projekt. Všechny cílí na `net10.0`. Každý projekt je jedna vrstva s vlastní odpovědností:

| Projekt | Odpovědnost | Závisí na |
| :--- | :--- | :--- |
| `src/Zpravodajstvi.Domain` | entity (`Article`, `Category`, `Comment`, `Tag`, `Image`, `ApplicationUser`), rozhraní repozitářů (`IRepository<T>` a specifická), názvy rolí `AppRoles`, `SearchNormalizer` | žádném projektu (jen balíček `Microsoft.Extensions.Identity.Stores` kvůli `IdentityUser`) |
| `src/Zpravodajstvi.Application` | služby (`ArticleService`, `CategoryService`, `TagService`, `CommentService`), rozhraní všech služeb včetně účtů, rolí a souborů, DTO, validační atributy `[NoProfanity]` a `[ImageFile]` | Domain |
| `src/Zpravodajstvi.Infrastructure` | `ApplicationDbContext`, migrace, repozitáře, služby nad Identity (`AccountService`, `UserService`, `RoleService`), ukládání souborů, `DbInitializer` | Application, Domain |
| `src/Zpravodajstvi.Presentation` | webová aplikace MVC: controllery, views, administrace (Area `Admin`), JavaScript | Application; Infrastructure jen v `Program.cs` |
| `tests/Zpravodajstvi.Tests` | unit testy a testy nad SQLite v paměti | všech čtyřech |

```text
Presentation ──► Application ──► Domain
     │ (jen Program.cs)              ▲
     └──► Infrastructure ────────────┘   (Infrastructure používá i Application)
```

Doména definuje **rozhraní** repozitářů a Infrastructure je **implementuje**. Základem je obecný (generic) repozitář s operacemi CRUD, ze kterého dědí specifické repozitáře:

```csharp
// src/Zpravodajstvi.Domain/Interfaces/IRepository.cs
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}

// src/Zpravodajstvi.Domain/Interfaces/ICommentRepository.cs
public interface ICommentRepository : IRepository<Comment>
{
    Task<IEnumerable<Comment>> GetByArticleIdAsync(int articleId);
    Task<IEnumerable<Comment>> GetByUserIdAsync(string userId);
    // ...
}
```

Implementace `Repository<T>` (`src/Zpravodajstvi.Infrastructure/Repositories/Repository.cs`) pracuje s `DbSet<T>` z EF Core. Specifické repozitáře z ní dědí (`public class CommentRepository : Repository<Comment>, ICommentRepository`) a přidávají dotazy, které načítají navigační vlastnosti přes `Include`. Pro úpravu stačí entitu načtenou kontextem uložit, nesledovanou je potřeba nejdřív připojit:

```csharp
public virtual async Task UpdateAsync(T entity)
{
    // Entitu načtenou tímto kontextem EF Core sleduje a změny najde sám,
    // nesledovanou (např. sestavenou z formuláře) je potřeba nejdřív připojit
    if (_context.Entry(entity).State == EntityState.Detached)
    {
        _dbSet.Update(entity);
    }

    await _context.SaveChangesAsync();
}
```

Služby v Application pracují jen s rozhraními, takže nevědí nic o EF Core, SQLite ani o ASP.NET Core Identity. Konkrétní třídy jim dodá kontejner závislostí (Dependency Injection). Díky tomu lze služby i controllery testovat s falešnými implementacemi (Moq).

**Proč je `ApplicationUser` v doméně:** články a komentáře mají navigační vlastnost na svého autora (`Article.Author`, `Comment.User`). Entity leží v Domain, a ta nesmí odkazovat na Infrastructure, proto je v Domain i třída uživatele. Dědí z `IdentityUser`, který pochází z balíčku `Microsoft.Extensions.Identity.Stores`. Je to jediná závislost domény a není to reference na jinou vrstvu.

**Identity jen v Infrastructure:** `UserManager`, `SignInManager` a `RoleManager` používají jen třídy `AccountService`, `UserService` a `RoleService` ve složce `src/Zpravodajstvi.Infrastructure/Identity`. Jejich rozhraní (`IAccountService`, `IUserService`, `IRoleService`) jsou v Application a controllery i views pracují jen s nimi (podrobně v [kapitole 8](#8-účty-role-a-oprávnění)).

## 2. Start aplikace (Program.cs)

**Soubory:** `src/Zpravodajstvi.Presentation/Program.cs`, `src/Zpravodajstvi.Application/DependencyInjection.cs`, `src/Zpravodajstvi.Infrastructure/DependencyInjection.cs`

Při startu se nastaví logování Serilog, zaregistrují se obě vrstvy, nastaví se cookie pro přihlášení a směrování. Nejdůležitější části `Program.cs`:

```csharp
// 1. Připojovací řetězec k databázi SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=zpravodajstvi.db";

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    // ...
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 4. Inicializace databáze (migrace) a seeding rolí, účtů i ukázkových článků
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedDataAsync(scope.ServiceProvider);
}
```

`AddApplication()` zaregistruje služby z vrstvy Application (`ArticleService`, `CategoryService`, `CommentService`, `TagService`). `AddInfrastructure()` zaregistruje všechno, co patří do Infrastructure:

```csharp
// src/Zpravodajstvi.Infrastructure/DependencyInjection.cs
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    // ... pravidla hesla
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
services.AddScoped<IArticleRepository, ArticleRepository>();
// ... další repozitáře

services.AddScoped<IAccountService, AccountService>();
services.AddScoped<IUserService, UserService>();
services.AddScoped<IRoleService, RoleService>();
services.AddScoped<IFileStorageService, LocalFileStorageService>();
```

- `AddScoped` znamená jednu instanci na jeden HTTP požadavek. Všechny repozitáře v rámci požadavku sdílejí stejný `ApplicationDbContext`.
- `AddScoped(typeof(IRepository<>), typeof(Repository<>))` registruje obecný repozitář pro libovolnou entitu.
- `AddIdentity` přidá správu uživatelů, hesel, rolí a přihlašovací cookie. Uživatele ukládá do tabulek `AspNetUsers`, `AspNetRoles` a dalších ve stejné databázi.
- `DbInitializer.SeedDataAsync` aplikuje migrace a naplní výchozí data (viz [kapitola 5](#5-migrace-a-výchozí-data)). Spolu s `AddInfrastructure` je to jediné místo, kde Presentation používá Infrastructure.
- Statické soubory známé při sestavení (CSS, JS, knihovny) obsluhuje `MapStaticAssets()`. Obrázky nahrané za běhu do `wwwroot/uploads` v jeho seznamu nejsou, proto je vrací `UseStaticFiles()`. Ten soubor vrátí jen tehdy, když pro adresu neexistuje žádný endpoint.
- Na konci jsou dvě routy: `{area:exists}/{controller=Home}/{action=Index}/{id?}` pro administraci (`/Admin/Article/...`) a výchozí `{controller=Home}/{action=Index}/{id?}`.

## 3. Cesta požadavku: controller, služba, repozitář, view

Jako příklad poslouží vyhledávání `GET /?search=český`:

1. **Routa** pošle požadavek na `HomeController.Index` (`src/Zpravodajstvi.Presentation/Controllers/HomeController.cs`). Model binding vezme parametr `search` z adresy.
2. **Controller** zavolá služby a výsledek předá do view. Sám nic nepočítá ani nečte z databáze:

   ```csharp
   public async Task<IActionResult> Index(int? categoryId = null, string? tag = null, string? search = null)
   {
       var articles = await _articleService.GetArticlesAsync(categoryId, tag, search);
       var categories = await _categoryService.GetAllCategoriesAsync();

       ViewBag.Categories = categories;
       ViewBag.CurrentCategory = categoryId;
       ViewBag.CurrentTag = tag;
       ViewBag.CurrentSearch = search;

       return View(articles);
   }
   ```

3. **Služba** `ArticleService.GetArticlesAsync` (`src/Zpravodajstvi.Application/Services/ArticleService.cs`) si vyžádá entity z repozitáře a převede je na DTO. Převod dělá `DtoMapper`, který jméno autora bere z navigační vlastnosti `Article.Author`:

   ```csharp
   var articles = await _articleRepository.GetFilteredAsync(categoryId, tag, search);
   return articles.Select(DtoMapper.ToListDto);
   ```

   ```csharp
   // src/Zpravodajstvi.Application/Services/DtoMapper.cs
   return new ArticleListDto
   {
       Id = article.Id,
       Title = article.Title,
       CategoryName = article.Category?.Name ?? "Bez kategorie",
       AuthorName = article.Author?.DisplayName ?? UnknownAuthor,
       CommentsCount = article.Comments?.Count ?? 0,
       // ...
   };
   ```

4. **Repozitář** `ArticleRepository.GetFilteredAsync` (`src/Zpravodajstvi.Infrastructure/Repositories/ArticleRepository.cs`) sestaví LINQ dotaz s `Include` (kategorie, autor, štítky, obrázky, komentáře), EF Core ho přeloží do SQL a SQLite vrátí řádky (podrobně v [kapitole 11](#11-vyhledávání-s-normalizací-českých-znaků)). Bez `Include(a => a.Author)` by `Author` zůstal `null` a místo jména by se zobrazil „Neznámý autor“.
5. **View** `src/Zpravodajstvi.Presentation/Views/Home/Index.cshtml` vykreslí seznam DTO do HTML pomocí Razoru a tag helperů.

### Modely, DTO a ViewModely

Views nedostávají entity EF Core, ale **DTO** (Data Transfer Object) nebo **ViewModely** s daty, která potřebují. Formulářové třídy navíc nesou validační atributy.

| Třída | Soubor | K čemu slouží |
| :--- | :--- | :--- |
| `ArticleListDto`, `ArticleDetailDto`, `ImageDto`, `CategoryDto`, `TagDto`, `CreateArticleDto`, `UpdateArticleDto` | `src/Zpravodajstvi.Application/DTOs/ArticleDtos.cs` | výpis a detail článku, kategorie, štítky, vstup pro vytvoření a úpravu článku |
| `CreateCommentDto`, `CommentDto` | `src/Zpravodajstvi.Application/DTOs/CommentDtos.cs` | formulář nového komentáře, zobrazení komentáře |
| `UpdateComment` | `src/Zpravodajstvi.Application/DTOs/UpdateComment.cs` | formulář úpravy komentáře |
| `UserDto`, `RegisterUserDto`, `CreateUserDto`, `UpdateUserDto`, `RoleDto` | `src/Zpravodajstvi.Application/DTOs/UserDtos.cs` | data účtů a rolí mezi controllery a službami (bez hesla a `PasswordHash`) |
| `OperationResult` | `src/Zpravodajstvi.Application/DTOs/OperationResult.cs` | výsledek operace služby: úspěch, nebo seznam chyb pro formulář |
| `ArticleCreateViewModel`, `ArticleEditViewModel` (společný základ `ArticleFormViewModel`) | `src/Zpravodajstvi.Presentation/Areas/Admin/Models/` | formulář článku včetně štítků a nahrávaného souboru `IFormFile` |
| `CategoryViewModel`, `TagViewModel`, `CommentEditViewModel` | `src/Zpravodajstvi.Presentation/Areas/Admin/Models/ContentViewModels.cs` | formuláře kategorie, štítku a moderování komentáře |
| `UserViewModel`, `CreateUserViewModel`, `EditUserViewModel`, `RoleViewModel` | `src/Zpravodajstvi.Presentation/Areas/Admin/Models/UserViewModels.cs` | správa účtů a rolí |
| `RegisterViewModel`, `LoginViewModel`, `ChangePasswordViewModel`, `ProfileViewModel` | `src/Zpravodajstvi.Presentation/Models/` | registrace, přihlášení, změna hesla, profil |

`ArticleDetailDto` slouží jako model stránky detailu. Kromě článku, autora a komentářů obsahuje i data pro formuláře komentářů:

```csharp
public CreateCommentDto NewComment { get; set; } = new();
// Úprava komentáře, která neprošla validací – view u ní znovu otevře formulář s chybou
public UpdateComment? EditComment { get; set; }
// Jméno přihlášeného uživatele (jen pro zobrazení ve formuláři komentáře, autora určuje server)
public string? CurrentUserName { get; set; }
```

### Views

| View | Obsah |
| :--- | :--- |
| `Views/Shared/_Layout.cshtml` | společná kostra stránky (Bootstrap), navigace, přepínač motivu |
| `Views/Shared/_LoginPartial.cshtml` | jméno (odkaz na profil) a role přihlášeného uživatele, odkaz do administrace, odhlášení |
| `Views/Home/Index.cshtml` | výpis článků s autorem, vyhledávací formulář, tlačítka kategorií, odkazy štítků |
| `Views/Home/Details.cshtml` | detail článku s autorem, formulář nového komentáře, seznam komentářů s úpravou a mazáním |
| `Views/Account/*.cshtml` | přihlášení, registrace, profil, změna hesla, stránka „Přístup odepřen“ |
| `Areas/Admin/Views/Shared/_AdminNav.cshtml` | navigace administrace (správu účtů a rolí vidí jen Admin) |
| `Areas/Admin/Views/**` | administrace článků, kategorií, štítků, komentářů, uživatelů a rolí |

Odkazy a formuláře se generují tag helpery, takže adresy odpovídají routám. Například odkaz na štítek ve výpisu:

```cshtml
<a asp-action="Index" asp-route-tag="@tag" class="badge bg-light text-secondary text-decoration-none border me-1">
    #@tag
</a>
```

Tabulky v administraci jsou v `<div class="table-responsive">`, takže se na mobilu posouvají vodorovně a nerozbijí rozložení stránky.

## 4. Datový model a DbContext

**Soubory:** `src/Zpravodajstvi.Domain/Entities/*.cs`, `src/Zpravodajstvi.Infrastructure/Data/ApplicationDbContext.cs`

| Entita | Hlavní vlastnosti |
| :--- | :--- |
| `Article` | `Title`, `Perex`, `Content`, `CreatedAt`, `UpdatedAt`, `SearchTitle`, `SearchPerex`, `SearchContent`, `CategoryId` + `Category`, `AuthorId` + `Author`, kolekce `Comments`, `Tags`, `Images` |
| `Category` | `Name`, kolekce `Articles` |
| `Comment` | `AuthorName`, `Content`, `CreatedAt`, `UserId` + `User` (autor), `ArticleId` + `Article` |
| `Tag` | `Name`, `SearchName`, kolekce `Articles` |
| `Image` | `Url`, `Caption`, `ArticleId` + `Article` |
| `ApplicationUser` | dědí z `IdentityUser` (`Id`, `UserName`, `Email`, hash hesla…), navíc `FullName`, `CreatedAt`, kolekce `Articles` a `Comments` a vlastnost `DisplayName` |

Autor článku a komentáře je cizí klíč s veřejnou navigační vlastností na uživatele:

```csharp
// src/Zpravodajstvi.Domain/Entities/Article.cs
public string? AuthorId { get; set; }
public ApplicationUser? Author { get; set; }

// src/Zpravodajstvi.Domain/Entities/Comment.cs
public string? UserId { get; set; }
public ApplicationUser? User { get; set; }
```

`Comment.AuthorName` je jméno autora v okamžiku napsání komentáře. Zobrazuje se jen tehdy, když komentář uživatele nemá (smazaný účet nebo starý komentář), jinak se zobrazí aktuální jméno z `Comment.User`.

`ApplicationDbContext` dědí z `IdentityDbContext<ApplicationUser>`, takže kromě tabulek aplikace obsahuje i tabulky Identity. Vazby a chování při mazání nastavuje `OnModelCreating` (Fluent API):

| Vazba | Typ | Při smazání nadřazeného záznamu |
| :--- | :--- | :--- |
| `Category` → `Article` | 1:N | `Restrict` – kategorii s články databáze smazat nedovolí |
| `ApplicationUser` → `Article` | 1:N přes `Article.AuthorId` | `SetNull` – článek zůstane, jen bez autora |
| `ApplicationUser` → `Comment` | 1:N přes `Comment.UserId` | `SetNull` – komentář zůstane, jen bez vlastníka |
| `Article` → `Comment` | 1:N | `Cascade` – smažou se i komentáře |
| `Article` → `Image` | 1:N | `Cascade` – smažou se i záznamy obrázků |
| `Article` ↔ `Tag` | M:N přes tabulku `ArticleTag` | smaže se jen vazba |

```csharp
// Konfigurace relace 1:N ApplicationUser -> Articles (autor článku).
// Po smazání účtu články zůstanou s AuthorId = null a zobrazí se jako od neznámého autora.
modelBuilder.Entity<Article>()
    .HasOne(a => a.Author)
    .WithMany(u => u.Articles)
    .HasForeignKey(a => a.AuthorId)
    .OnDelete(DeleteBehavior.SetNull);

// Konfigurace relace 1:N ApplicationUser -> Comments (autor komentáře).
// Po smazání účtu komentáře zůstanou s UserId = null a spravovat je může jen administrátor nebo redaktor.
modelBuilder.Entity<Comment>()
    .HasOne(c => c.User)
    .WithMany(u => u.Comments)
    .HasForeignKey(c => c.UserId)
    .OnDelete(DeleteBehavior.SetNull);
```

DbContext navíc přepisuje `SaveChanges` a `SaveChangesAsync`, aby před každým uložením doplnil normalizovaná pole pro vyhledávání (viz [kapitola 11](#11-vyhledávání-s-normalizací-českých-znaků)).

## 5. Migrace a výchozí data

**Soubory:** `src/Zpravodajstvi.Infrastructure/Migrations/`, `src/Zpravodajstvi.Infrastructure/Data/DbInitializer.cs`

Projekt je **Code-First**: model popisují třídy entit a DbContext, schéma databáze z nich vytvářejí migrace EF Core. Každá migrace má metodu `Up` (co se má v databázi změnit) a `Down` (jak změnu vrátit), soubor `ApplicationDbContextModelSnapshot.cs` drží aktuální podobu modelu.

| Migrace | Co dělá |
| :--- | :--- |
| `20261012203328_InitialCreate` | výchozí schéma: články, kategorie, štítky, obrázky, komentáře a tabulky Identity |
| `20261012203511_AddCommentUserId` | přidá `Comments.UserId` s indexem a cizím klíčem na `AspNetUsers` (`SET NULL`) |
| `20261012203628_AddSearchFields` | přidá `Articles.SearchTitle`, `SearchPerex`, `SearchContent` a `Tags.SearchName` |
| `20261014075659_AddArticleAuthor` | přidá `Articles.AuthorId` s indexem a cizím klíčem na `AspNetUsers` (`SET NULL`) |

Migrace obsahují jen operace EF Core (`AddColumn`, `CreateIndex`, `AddForeignKey` …), žádný ručně psaný SQL. Aplikují se automaticky při startu v `DbInitializer.SeedDataAsync`:

```csharp
// Zajištění existence databáze a aplikace migrací (existující data zůstanou zachována)
await context.Database.MigrateAsync();
```

Databáze v repozitáři (`zpravodajstvi.db`) je vytvořená čistě z migrací: po smazání staré databáze ji vytvořil `dotnet ef database update` a seeding ji naplnil při prvním spuštění. Starší databáze (původní ze `EnsureCreated()` i převedená verze s daty) jsou jen lokálně ve složce `backup/`, která je v `.gitignore`. Úplně starou kopii databáze bez historie migrací aplikace nepřevádí, stačí ji smazat a aplikace vytvoří novou (README, oddíl Databáze a migrace).

Že migrace odpovídají modelu, ověří příkaz `dotnet ef migrations has-pending-model-changes` („No changes have been made to the model since the last migration.“). Stejně tak zkušební `dotnet ef migrations add Check` vytvoří migraci s prázdnými metodami `Up` a `Down` a `ModelSnapshot` nezmění. Takovou migraci je potom potřeba smazat.

Po migracích `DbInitializer` vytvoří tři role (`AppRoles.SystemRoles`), tři výchozí účty (metoda `EnsureUserAsync`) a do prázdné databáze ukázkové kategorie, štítky, články a komentáře. Ukázkové články i komentáře se rovnou svážou s autory:

```csharp
var ctenar = await userManager.FindByEmailAsync("ctenar@zpravodajstvi.cz");
var redaktor = await userManager.FindByEmailAsync("redaktor@zpravodajstvi.cz");
// ...
Category = catTech,
AuthorId = redaktor?.Id,
// ...
new()
{
    AuthorName = "Petr Čtenář",
    UserId = ctenar?.Id,
    // ...
},
```

## 6. Články, kategorie, štítky a obrázky

**Soubory:**
- `src/Zpravodajstvi.Presentation/Areas/Admin/Controllers/ArticleController.cs`, `CategoryController.cs`, `TagController.cs`
- `src/Zpravodajstvi.Application/Services/ArticleService.cs`, `CategoryService.cs`, `TagService.cs`
- `src/Zpravodajstvi.Infrastructure/Repositories/ArticleRepository.cs`, `CategoryRepository.cs`, `TagRepository.cs`
- `src/Zpravodajstvi.Infrastructure/Files/LocalFileStorageService.cs`
- `src/Zpravodajstvi.Presentation/Areas/Admin/Views/Article/`, `Category/`, `Tag/`

Administrace leží v Area `Admin`. Obsah (články, kategorie, štítky, komentáře) spravují role Admin a Redaktor:

```csharp
[Area("Admin")]
[Authorize(Roles = AppRoles.ContentManagers)]
public class ArticleController : Controller
```

### Vytvoření článku

Formulář posílá `ArticleCreateViewModel` (titulek, text, kategorie, zaškrtnuté štítky, volitelný obrázek). Autor ve formuláři není. Controller vezme `Id` přihlášeného uživatele z claimu a předá ho službě spolu s DTO:

```csharp
// Autorem článku je přihlášený uživatel (Id z claimu NameIdentifier, ne ze skrytého pole formuláře)
var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
// ...
var dto = new CreateArticleDto
{
    Title = model.Title,
    Content = model.Content,
    CategoryId = model.CategoryId.GetValueOrDefault(),
    TagIds = model.SelectedTagIds,
    Image = model.ImageFile
};

var created = await _articleService.CreateArticleAsync(dto, authorId);
```

`ArticleService.CreateArticleAsync` nastaví autora, doplní perex (prvních 150 znaků textu), načte vybrané štítky a obrázek uloží přes `IFileStorageService`:

```csharp
var article = new Article
{
    Title = dto.Title,
    // ...
    // Autor = přihlášený uživatel, kterého předal controller (ne hodnota z formuláře)
    AuthorId = authorId,
    CreatedAt = DateTime.UtcNow,
    Tags = await LoadTagsAsync(dto.TagIds)
};

// Uložení souboru obstará IFileStorageService, článek si pamatuje jen jeho URL
if (dto.Image != null && dto.Image.Length > 0)
{
    var imageUrl = await _fileStorage.SaveImageAsync(dto.Image);
    // ...
}
```

### Úprava a smazání článku

`ArticleController.Edit` sestaví `UpdateArticleDto` a `ArticleService.UpdateArticleAsync` změní titulek, text, kategorii a štítky, znovu vytvoří perex a nastaví `UpdatedAt`. Nový obrázek **nahradí** dosavadní: starý záznam `Image` se odebere a jeho soubor se smaže až po úspěšném uložení článku. Normalizovaná pole pro vyhledávání se přepočítají automaticky při uložení. Smazání článku (`DeleteArticleAsync`) smaže i nahrané soubory jeho obrázků, komentáře a záznamy obrázků smaže databáze (`Cascade`).

Redaktor i administrátor mohou upravit a smazat jakýkoli článek, protože oba spravují obsah redakce.

### Nahrávání obrázků

Ukládání souboru je služba, ne kód v controlleru. Rozhraní `IFileStorageService` je v Application, implementace `LocalFileStorageService` v Infrastructure zapisuje do `wwwroot/uploads` pod jedinečným jménem:

```csharp
// Guid v názvu zabrání přepsání souboru se stejným jménem
var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
```

Nahrát lze jen obrázek `.jpg`, `.jpeg`, `.png`, `.gif` nebo `.webp` do 5 MB. Hlídá to vlastní validační atribut `[ImageFile]` u vlastnosti `ImageFile` ve ViewModelu a pro jistotu ještě jednou služba (viz [kapitola 9](#9-validace-a-vlastní-validační-atributy)). Bez této kontroly by šlo nahrát například `.html` soubor, který by server vracel jako stránku.

### Kategorie

`CategoryService` vrací kategorie jako `CategoryDto` včetně počtu článků (`ArticlesCount`), vytváří je, přejmenovává a maže prázdné. Formulář vytvoření i úpravy používá `CategoryViewModel` s validací názvu. Ve veřejné části jsou kategorie tlačítka nad výpisem článků, která přidají do adresy `?categoryId=...`. Repozitář pak filtruje:

```csharp
if (categoryId.HasValue)
{
    query = query.Where(a => a.CategoryId == categoryId.Value);
}
```

Mazání kategorie, která ještě obsahuje články, popisuje [kapitola 12](#12-mazání-kategorie-s-články).

### Štítky

Štítky (`Tag`) spravuje `TagController` (vytvoření, přejmenování, smazání) přes `TagService`. Název se ukládá bez úvodního `#`, ten se doplní až při zobrazení. K článkům se štítky přiřazují zaškrtávacími poli ve formuláři článku (`SelectedTagIds`) a s články je spojuje tabulka `ArticleTag`. Smazáním štítku zmizí jen jeho vazby, články zůstanou. Ve výpisu i v detailu se štítky zobrazují jako odkazy `#štítek`, které vedou na `/?tag=štítek`. Hledat podle štítku lze i z vyhledávacího pole zápisem `#Česko`.

## 7. Komentáře

**Soubory:**
- `src/Zpravodajstvi.Presentation/Controllers/CommentsController.cs` (veřejná část)
- `src/Zpravodajstvi.Presentation/Areas/Admin/Controllers/CommentController.cs` (moderování)
- `src/Zpravodajstvi.Application/Services/CommentService.cs`
- `src/Zpravodajstvi.Application/DTOs/CommentDtos.cs`, `UpdateComment.cs`
- `src/Zpravodajstvi.Infrastructure/Repositories/CommentRepository.cs`
- `src/Zpravodajstvi.Presentation/Views/Home/Details.cshtml`

### Přidání komentáře

Formulář posílá jen `ArticleId` a `Content`. DTO `CreateCommentDto` autora vůbec neobsahuje, takže ho z formuláře nelze podvrhnout. Pole se jménem ve formuláři slouží jen pro informaci a nemá atribut `name`, proto se neodesílá:

```cshtml
<input id="commentAuthorName" class="form-control" value="@Model.CurrentUserName" readonly="readonly" />
```

Controller vezme `Id` přihlášeného uživatele z claimu a předá ho službě:

```csharp
public async Task<IActionResult> Add([Bind(Prefix = "NewComment")] CreateCommentDto model)
{
    // Autora vždy určuje server podle přihlášeného účtu (Id z claimu, jméno dohledá služba), z formuláře nic nepřichází
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null)
    {
        return Challenge();
    }

    if (!ModelState.IsValid)
    {
        // ... načte článek a vrátí stránku detailu s chybami validace
        return View("~/Views/Home/Details.cshtml", article);
    }

    var created = await _commentService.AddCommentAsync(model, userId);
    // ...
}
```

`CommentService.AddCommentAsync` ověří, že článek existuje, dohledá jméno autora přes `IUserService` a uloží komentář s jeho `UserId`:

```csharp
var authorName = await _userService.GetDisplayNameAsync(userId);
if (authorName == null)
{
    throw new UnauthorizedAccessException("Přihlášený uživatel nebyl nalezen.");
}

var comment = new Comment
{
    AuthorName = authorName.Trim(),
    UserId = userId,
    // ...
};
```

Atribut `[Bind(Prefix = "NewComment")]` říká, že pole formuláře mají prefix `NewComment.` (např. `NewComment.Content`), protože view generuje formulář z vlastnosti `ArticleDetailDto.NewComment`.

### Zobrazení autora

Repozitář načítá komentáře s uživatelem (`Include(c => c.User)`, v detailu článku `ThenInclude(c => c.User)`). `DtoMapper` pak zobrazí aktuální jméno uživatele, a jen když komentář uživatele nemá, uložené jméno:

```csharp
// Aktuální jméno uživatele; komentář bez uživatele (smazaný účet, starý záznam) má jen uložené jméno
AuthorName = comment.User?.DisplayName ?? comment.AuthorName,
```

### Úprava komentáře

Formulář úpravy je pod každým komentářem, který smí přihlášený uživatel spravovat. Jeho pole mají prefix `EditComment.` a controller je naváže do `UpdateComment`. DTO se validuje stejně jako při vytvoření:

```csharp
public class UpdateComment
{
    public int Id { get; set; }
    public int ArticleId { get; set; }

    [Required(ErrorMessage = "Obsah komentáře je povinný.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "Komentář musí mít mezi 3 a 1000 znaky.")]
    [NoProfanity(ErrorMessage = "Komentář obsahuje nepovolená nebo vulgární slova.")]
    public string Content { get; set; } = string.Empty;
}
```

Když validace neprojde, controller vrátí stránku detailu stejně jako při přidání a do modelu uloží neúspěšnou úpravu:

```csharp
public async Task<IActionResult> Edit([Bind(Prefix = "EditComment")] UpdateComment model)
{
    // ...
    if (!ModelState.IsValid)
    {
        var article = await _articleService.GetArticleDetailsAsync(model.ArticleId);
        // ...
        article.EditComment = model;
        // ...
        return View("~/Views/Home/Details.cshtml", article);
    }
    // ... kontrola vlastníka a uložení
}
```

View pak u tohoto komentáře nechá formulář otevřený (třída `show`), vrátí do něj odeslaný text a zobrazí chybu:

```cshtml
var failedEdit = Model.EditComment?.Id == comment.Id ? Model.EditComment : null;
<!-- ... -->
editTextarea.InnerHtml.Append(failedEdit?.Content ?? comment.Content);
<!-- ... -->
@if (failedEdit != null)
{
    <span asp-validation-for="EditComment.Content" class="text-danger small"></span>
}
```

Formulář úpravy se kontroluje i v prohlížeči. Atributy `data-val-*` pro pole vygeneruje MVC jednou pro celou stránku z pravidel `UpdateComment.Content` (`[Required]`, `[StringLength]`, `[NoProfanity]`):

```cshtml
var editContentExplorer = MetadataProvider.GetModelExplorerForType(typeof(UpdateComment), null)
    .GetExplorerForProperty(nameof(UpdateComment.Content));
var editValidationAttributes = new Dictionary<string, string>();
ValidationAttributeProvider.AddValidationAttributes(ViewContext, editContentExplorer, editValidationAttributes);
```

Každý komentář pak dostane vlastní `<textarea name="EditComment.Content">` s těmito atributy a s jedinečným `id` (sestaví ji `TagBuilder`). Tag helper `asp-for` se tu nepoužívá: po neúspěšné úpravě by vzal odeslaný text z `ModelState` a vložil ho do formulářů všech komentářů. Formulář je schovaný v rozbalovacím panelu, proto ho skript po rozbalení nechá jQuery Validation znovu načíst:

```js
document.querySelectorAll('.comment-edit-collapse').forEach(function (panel) {
    panel.addEventListener('shown.bs.collapse', function () {
        var form = panel.querySelector('form');
        $(form).removeData('validator').removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse(form);
    });
});
```

Po úspěšné úpravě controller přesměruje zpět na článek přímo na upravený komentář (`/Home/Details/2#comment-11`). Každý komentář má proto v HTML `id="comment-@comment.Id"`.

### Smazání komentáře a moderování

Tlačítko „Smazat“ odešle formulář s `id` komentáře a `articleId` na `CommentsController.Delete`. Kdo smí komentář upravit nebo smazat, popisuje následující kapitola.

Administrátor a redaktor mají navíc v administraci stránku **Komentáře** (`/Admin/Comment`) se všemi komentáři, článkem a autorem. Úprava tam používá `CommentEditViewModel` se stejnými pravidly včetně `[NoProfanity]`.

## 8. Účty, role a oprávnění

**Soubory:**
- `src/Zpravodajstvi.Application/Interfaces/IAccountService.cs`, `IUserService.cs`, `IRoleService.cs` (rozhraní)
- `src/Zpravodajstvi.Infrastructure/Identity/AccountService.cs`, `UserService.cs`, `RoleService.cs` (implementace nad Identity)
- `src/Zpravodajstvi.Presentation/Controllers/AccountController.cs` (registrace, přihlášení, odhlášení, profil, změna hesla)
- `src/Zpravodajstvi.Presentation/Areas/Admin/Controllers/UserController.cs`, `RoleController.cs` (správa účtů a rolí)
- `src/Zpravodajstvi.Domain/Constants/AppRoles.cs` (názvy rolí), `src/Zpravodajstvi.Infrastructure/Data/DbInitializer.cs` (role a výchozí účty)

### Identity přes služby

Controllery ani views nepoužívají `UserManager`, `SignInManager`, `RoleManager` ani `DbContext`. Například registrace v controlleru jen předá data službě a výsledek přepíše do `ModelState`:

```csharp
// Služba vytvoří účet s rolí Čtenář a uživatele rovnou přihlásí
var result = await _accountService.RegisterAsync(new RegisterUserDto
{
    FullName = model.FullName,
    Email = model.Email,
    Password = model.Password
});

if (result.Succeeded)
{
    // ...
    return RedirectToLocal(returnUrl);
}

foreach (var error in result.Errors)
{
    ModelState.AddModelError(string.Empty, error);
}
```

S Identity pracuje až `AccountService` v Infrastructure:

```csharp
var result = await _userManager.CreateAsync(user, dto.Password);
// ...
// Každý nově registrovaný uživatel automaticky získá roli Čtenář
await _userManager.AddToRoleAsync(user, AppRoles.Ctenar);
await _signInManager.SignInAsync(user, isPersistent: false);
```

Ani `_LoginPartial.cshtml` neinjektuje Identity. Přihlášení pozná z `User.Identity`, role z claimů (`User.IsInRole`) a jméno mu dodá `IUserService`:

```cshtml
@inject IUserService UserService
<!-- ... -->
var displayName = (userId != null ? await UserService.GetDisplayNameAsync(userId) : null) ?? User.Identity?.Name;
```

### Role a přístup

Názvy rolí jsou konstanty ve třídě `AppRoles` v doméně, takže je mohou používat atributy `[Authorize]`, views i `DbInitializer`:

| Role | Odpovídá | Smí |
| :--- | :--- | :--- |
| `Admin` | Admin | vše: obsah, moderování komentářů, správa účtů a rolí |
| `Redaktor` | Manager | správa obsahu: články, kategorie, štítky, moderování komentářů; ne účty, role ani nastavení |
| `Ctenar` | User | čtení, komentování, úprava a mazání vlastních komentářů, vlastní profil |

```csharp
// Správa obsahu (články, kategorie, štítky, komentáře) – Admin a Redaktor
public const string ContentManagers = Admin + "," + Redaktor;
```

Přístup hlídají atributy `[Authorize]`:

| Kde | Atribut | Kdo má přístup |
| :--- | :--- | :--- |
| `CommentsController.Add` | `[Authorize(Roles = AppRoles.All)]` | přihlášení s rolí |
| `CommentsController.Edit`, `Delete` | `[Authorize]` + kontrola vlastníka v `CommentService` | autor komentáře, Admin nebo Redaktor |
| `AccountController.Profile`, `ChangePassword`, `Logout` | `[Authorize]` | každý přihlášený |
| `Admin/ArticleController`, `CategoryController`, `TagController`, `CommentController` | `[Authorize(Roles = AppRoles.ContentManagers)]` | Admin, Redaktor |
| `Admin/UserController`, `RoleController` | `[Authorize(Roles = AppRoles.Admin)]` | jen Admin |

Nepřihlášeného uživatele přesměruje cookie autentizace na `/Account/Login`, přihlášeného bez oprávnění na `/Account/AccessDenied`. Navigace administrace (`_AdminNav.cshtml`) ukazuje odkazy na účty a role jen administrátorovi, skutečnou ochranu ale zajišťují atributy. Atributy jsou jen na úrovni controllerů, žádná akce v administraci je nemění ani neobchází přes `[AllowAnonymous]`. Hlídá to test `AuthorizationTests`, který zároveň selže, když v administraci přibude controller, jenž v testu chybí.

### Co obsahuje přihlášený uživatel

Po přihlášení obsahuje cookie identitu s claimy. Pro vlastnictví článků a komentářů jsou důležité dva:

| Claim | Hodnota | Jak ho získat |
| :--- | :--- | :--- |
| `ClaimTypes.NameIdentifier` | `Id` uživatele (GUID z `AspNetUsers.Id`) | `User.FindFirstValue(ClaimTypes.NameIdentifier)` |
| `ClaimTypes.Name` | `UserName`, v této aplikaci **e-mail** | `User.Identity.Name` |

Jméno uživatele (`FullName`) ani e-mail nejsou vhodné k určení vlastníka: e-mail se nerovná jménu u komentáře a stejné celé jméno může mít více lidí. Jediný jednoznačný a neměnný údaj je `Id`. Proto se z claimu `NameIdentifier` bere i autor nového článku a komentáře.

### Kontrola vlastníka podle UserId

Controller vezme `Id` přihlášeného uživatele z claimu a předá ho službě spolu s informací, zda jde o moderátora:

```csharp
// Vlastník komentáře se pozná podle Id přihlášeného uživatele (claim NameIdentifier)
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
// ...
await _commentService.DeleteCommentAsync(id, userId, IsModerator());

// Komentáře moderuje (upravuje a maže cizí) Admin a Redaktor
private bool IsModerator()
{
    return User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Redaktor);
}
```

Služba porovná `Id` s `Comment.UserId`. Moderátor smí všechno:

```csharp
// Kontrola oprávnění: Admin a Redaktor (moderátoři) mohou vše, ostatní jen své vlastní komentáře (podle Id uživatele)
if (!isModerator && !IsOwner(comment, currentUserId))
{
    throw new UnauthorizedAccessException("Nemáte oprávnění smazat tento komentář.");
}

// Komentář bez UserId (autora nešlo určit) nepatří nikomu – spravuje ho jen moderátor
private static bool IsOwner(Comment comment, string? currentUserId)
{
    return !string.IsNullOrEmpty(comment.UserId)
        && string.Equals(comment.UserId, currentUserId, StringComparison.Ordinal);
}
```

Výjimku `UnauthorizedAccessException` controller zachytí a vrátí `Forbid()`, což uživatele přesměruje na stránku „Přístup odepřen“. Stejně funguje úprava (`UpdateCommentAsync`).

Tlačítka „Upravit“ a „Smazat“ zobrazí view jen tomu, kdo komentář spravovat smí. Rozhoduje stejné pravidlo:

```cshtml
var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
// Moderátoři (Admin a Redaktor) mohou upravit a smazat jakýkoli komentář
var isModerator = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Redaktor);
<!-- ... v cyklu přes komentáře: -->
var canManage = isModerator || (comment.UserId != null && comment.UserId == currentUserId);
```

Skrytá tlačítka jsou jen pohodlí pro uživatele, skutečnou ochranu zajišťuje kontrola v `CommentService`.

**Komentáře s `UserId = null`** jsou komentáře, u kterých se autora nepodařilo jednoznačně určit (viz [kapitola 14, chyba 5](#chyba-5-uživatel-se-stejným-jménem-mohl-upravovat-cizí-komentáře)), nebo komentáře smazaných účtů (vazba `SetNull`). Upravit a smazat je může jen moderátor.

### Změna vlastního hesla

Každý přihlášený uživatel si na stránce `/Account/ChangePassword` (odkaz je v profilu) může změnit své heslo. Formulář používá `ChangePasswordViewModel`: současné heslo, nové heslo a jeho potvrzení s `[Compare]`. Controller jen zkontroluje `ModelState` a zavolá službu s `Id` z claimu, takže tímto formulářem nejde změnit heslo jinému účtu:

```csharp
var result = await _accountService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);
```

`AccountService` v Infrastructure nechá `UserManager` ověřit současné heslo a uložit hash nového. Změna hesla mění bezpečnostní razítko, proto služba obnoví přihlašovací cookie, aby uživatel zůstal přihlášený:

```csharp
// UserManager ověří současné heslo, zkontroluje pravidla pro nové heslo a uloží jeho hash
var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
if (!result.Succeeded)
{
    return OperationResult.Failure(result.Errors.Select(TranslateError));
}

// Změna hesla mění bezpečnostní razítko (SecurityStamp) – obnovíme cookie, aby uživatel zůstal přihlášený
await _signInManager.RefreshSignInAsync(user);
```

Špatně zadané současné heslo vrátí hlášku „Současné heslo není správné.“

### Správa uživatelů

Administrátor v `/Admin/User` vytváří účty (s počátečním heslem a rolí), mění jméno, e-mail a roli a maže účty. **Heslo uživatele měnit nemůže**: `EditUserViewModel` ani `UpdateUserDto` heslo neobsahují a `UserService.UpdateUserAsync` mění jen jméno, e-mail a roli, `PasswordHash` nikde nenastavuje. Heslo si mění jen sám uživatel (viz výše), protože k tomu musí znát to současné. Pravidla navíc chrání administrátora před zablokováním sebe sama:

```csharp
// Admin si nesmí sám odebrat roli Admin, jinak by ztratil přístup do správy účtů
if (user.Id == currentUserId && dto.Role != AppRoles.Admin)
{
    return OperationResult.Failure("Svému vlastnímu účtu nemůžete odebrat roli Admin.");
}
```

Vlastní účet smazat nelze. Po smazání jiného účtu jeho články a komentáře zůstanou, jen bez autora (`SetNull`).

### Správa rolí

V `/Admin/Role` vidí administrátor role s počtem uživatelů. Vlastní role může vytvořit, přejmenovat a smazat (jen pokud ji nikdo nemá). Systémové role `Admin`, `Redaktor` a `Ctenar` používá kód v `[Authorize]`, proto je `RoleService` přejmenovat ani smazat nedovolí.

### Profil

`/Account/Profile` zobrazí přihlášenému uživateli jeho údaje, jeho články (u redakce) a jeho komentáře s odkazy na články. Každý vidí jen svůj profil, `Id` se bere z claimu.

## 9. Validace a vlastní validační atributy

**Soubory:**
- `src/Zpravodajstvi.Application/Validation/NoProfanityAttribute.cs` (seznam slov, kontrola na serveru)
- `src/Zpravodajstvi.Application/Validation/ImageFileAttribute.cs` (kontrola nahrávaného obrázku)
- `src/Zpravodajstvi.Presentation/wwwroot/js/custom-validation.js` (kontrola v prohlížeči)
- `src/Zpravodajstvi.Presentation/Views/Shared/_ValidationScriptsPartial.cshtml` (načtení skriptů)
- použití `[NoProfanity]`: `CreateCommentDto.Content`, `UpdateComment.Content`, `CommentEditViewModel.Content`; použití `[ImageFile]`: `ArticleFormViewModel.ImageFile`
- `src/Zpravodajstvi.Presentation/Views/Home/Details.cshtml` (validační atributy a `unobtrusive.parse` pro formulář úpravy komentáře)

### Server a prohlížeč

Validační atributy (`[Required]`, `[StringLength]`, `[EmailAddress]`, `[Compare]` i vlastní) ověřuje MVC na serveru při model bindingu a chyby zapisuje do `ModelState`. Každá akce `[HttpPost]`, která přijímá data formuláře, kontroluje `ModelState.IsValid` a při chybě vrátí formulář s hláškami. Formuláře mají `asp-validation-for` u polí a v sekci `Scripts` načítají `_ValidationScriptsPartial`, takže tytéž atributy zkontroluje i jQuery Validation v prohlížeči ještě před odesláním.

### Filtr vulgárních slov

`NoProfanityAttribute` je vlastní validační atribut (dědí z `ValidationAttribute`). Zároveň implementuje `IClientModelValidator`, takže do HTML vstupního pole přidá atributy pro klientskou validaci:

```csharp
MergeAttribute(context.Attributes, "data-val", "true");
MergeAttribute(context.Attributes, "data-val-noprofanity", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
MergeAttribute(context.Attributes, "data-val-noprofanity-words", string.Join(",", BannedWords));
```

### Kontrola po celých slovech

Text i zakázaná slova se nejdřív převedou na malá písmena bez diakritiky (`RemoveDiacritics`). Pak se každé slovo hledá s hranicemi `\b`, takže se najde jen jako **celé slovo**, ne jako část jiného slova:

```csharp
var normalizedInput = RemoveDiacritics(input).ToLowerInvariant();

foreach (var word in BannedWords)
{
    var normalizedWord = RemoveDiacritics(word).ToLowerInvariant();
    var pattern = $@"\b{Regex.Escape(normalizedWord)}\b";
    if (Regex.IsMatch(normalizedInput, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
    {
        return true;
    }
}
```

`\b` v .NET bere jako písmena i znaky s diakritikou, takže hranice slov fungují i pro češtinu.

| Text | Výsledek | Proč |
| :--- | :--- | :--- |
| `Počkejte prosím sekundu.` | povoleno | „kundu“ je jen uvnitř slova „sekundu“ |
| `Operace proběhla úspěšně.` | povoleno | „pera“ je jen uvnitř slova „operace“ |
| `Autor je kretén.` | zamítnuto | „kretén“ je celé slovo |
| `To je ale IDIOT!` | zamítnuto | velikost písmen nehraje roli |

Prohlížeč dělá totéž ve funkci `containsWholeWord`. JavaScriptové `\b` zná jen znaky ASCII, proto se hranice slova skládá z unicodových tříd:

```js
function containsWholeWord(text, word) {
    var escaped = word.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    var pattern = new RegExp("(^|[^\\p{L}\\p{Mn}\\p{Nd}\\p{Pc}])" + escaped + "(?=$|[^\\p{L}\\p{Mn}\\p{Nd}\\p{Pc}])", "iu");
    return pattern.test(text);
}
```

Metodu zaregistruje `$.validator.addMethod("noprofanity", ...)` a adaptér `$.validator.unobtrusive.adapters.add("noprofanity", ["words"], ...)` ji spojí s atributy `data-val-noprofanity-*`. Formulář nového komentáře, formulář úpravy komentáře pod článkem (viz [kapitola 7](#7-komentáře)) i úprava komentáře v administraci se tak zkontrolují už v prohlížeči. Server je kontroluje znovu, protože kontrolu v prohlížeči lze obejít.

### Kontrola nahrávaného obrázku

Druhý vlastní atribut `ImageFileAttribute` hlídá nahrávaný soubor. Prázdné pole je v pořádku (obrázek je nepovinný), jinak musí mít soubor povolenou příponu a nejvýše 5 MB:

```csharp
public static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
public const long MaxFileSizeBytes = 5 * 1024 * 1024;
```

Statickou metodu `IsAllowedImage` volá i `LocalFileStorageService` před uložením souboru, takže kontrola platí, i kdyby službu někdo zavolal bez validace modelu.

## 10. Ochrana formulářů antiforgery tokenem

**Soubory:** všechny controllery s akcemi `[HttpPost]` a views s formuláři `method="post"`

Útok CSRF zneužívá to, že prohlížeč k požadavku na náš web automaticky přiloží přihlašovací cookie, i když formulář odeslala cizí stránka. Obranou je antiforgery token:

1. Tag helper formuláře (`<form asp-controller="..." asp-action="..." method="post">`) do každého POST formuláře automaticky vloží skryté pole s tokenem a nastaví k němu antiforgery cookie:

   ```html
   <input name="__RequestVerificationToken" type="hidden" value="CfDJ8..." />
   ```

2. Atribut `[ValidateAntiForgeryToken]` u akce ověří, že token z formuláře patří k cookie. Když chybí nebo nesedí, akce se nespustí a server vrátí **400 Bad Request**.

```csharp
// src/Zpravodajstvi.Presentation/Areas/Admin/Controllers/ArticleController.cs
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(ArticleCreateViewModel model)
```

Atribut mají všechny akce, které přijímají POST (celkem 25):

| Controller | Akce |
| :--- | :--- |
| `AccountController` | `Register`, `Login`, `Logout`, `ChangePassword` |
| `CommentsController` | `Add`, `Edit`, `Delete` |
| `Admin/ArticleController` | `Create`, `Edit`, `DeleteConfirmed` |
| `Admin/CategoryController` | `Create`, `Edit`, `DeleteConfirmed` |
| `Admin/TagController` | `Create`, `Edit`, `DeleteConfirmed` |
| `Admin/CommentController` | `Edit`, `DeleteConfirmed` |
| `Admin/UserController` | `Create`, `Edit`, `EditRole`, `Delete` |
| `Admin/RoleController` | `Create`, `Edit`, `DeleteConfirmed` |

Všechny POST formuláře ve views používají tag helper (`asp-action`, `asp-controller`, `asp-area`), takže token obsahují, včetně formulářů s nahráváním souborů (`enctype="multipart/form-data"`). Aplikace neposílá žádné AJAX požadavky. Kdyby je v budoucnu posílala, musela by token přidat do hlavičky požadavku.

## 11. Vyhledávání s normalizací českých znaků

**Soubory:**
- `src/Zpravodajstvi.Domain/Search/SearchNormalizer.cs` (převod textu)
- `src/Zpravodajstvi.Domain/Entities/Article.cs`, `Tag.cs` (normalizované sloupce)
- `src/Zpravodajstvi.Infrastructure/Data/ApplicationDbContext.cs` (plnění sloupců při uložení)
- `src/Zpravodajstvi.Infrastructure/Repositories/ArticleRepository.cs` (dotaz)
- `src/Zpravodajstvi.Infrastructure/Migrations/20261012203628_AddSearchFields.cs` (přidání sloupců)
- `src/Zpravodajstvi.Presentation/Views/Home/Index.cshtml` (vyhledávací formulář, odkazy štítků)

### Proč nestačí `ToLower()` v dotazu

EF Core přeloží `a.Title.ToLower()` na SQL funkci `lower()`. Ta v SQLite převádí jen písmena ASCII, takže z „Český“ udělá „Český“ (velké Č zůstane), zatímco hledaný výraz převedený v C# je „český“. Řetězce se neshodují a dotaz nic nenajde.

### Normalizované sloupce

Aplikace proto ukládá ke každému článku a štítku normalizovanou kopii textu (malá písmena bez diakritiky) a hledá v ní. Převod dělá jediná funkce, kterou používá ukládání i hledání:

```csharp
public static string Normalize(string? text)
{
    if (string.IsNullOrEmpty(text))
    {
        return string.Empty;
    }

    // Rozklad znaků (FormD) oddělí diakritiku od písmene ("č" -> "c" + háček), háček pak vynecháme
    var decomposed = text.Normalize(NormalizationForm.FormD);
    var builder = new StringBuilder(decomposed.Length);

    foreach (var c in decomposed)
    {
        if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
        {
            builder.Append(c);
        }
    }

    // ToLowerInvariant (na rozdíl od SQLite funkce lower()) převádí i jiná než ASCII písmena
    return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
}
```

Sloupce plní `ApplicationDbContext` před každým uložením. Funguje to pro vytvoření i úpravu článku či štítku a pro ukázková data, aniž by na to musel myslet kód služeb:

```csharp
public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
{
    UpdateSearchFields();
    return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
}

private void UpdateSearchFields()
{
    foreach (var entry in ChangeTracker.Entries<Article>())
    {
        if (entry.State is EntityState.Added or EntityState.Modified)
        {
            entry.Entity.SearchTitle = SearchNormalizer.Normalize(entry.Entity.Title);
            entry.Entity.SearchPerex = SearchNormalizer.Normalize(entry.Entity.Perex);
            entry.Entity.SearchContent = SearchNormalizer.Normalize(entry.Entity.Content);
        }
    }
    // ... stejně pro Tag.SearchName
}
```

Záznamy, které v databázi byly už před přidáním sloupců, se naplnily jednorázově při převodu databáze z repozitáře. Migrace samotná jen přidává sloupce, při vytváření nové databáze jsou tabulky v tomto kroku ještě prázdné.

### Dotaz

Repozitář normalizuje hledaný výraz stejnou funkcí a porovnává ho s normalizovanými sloupci. Výraz začínající `#` se hledá jako štítek, stejně jako odkaz `/?tag=...`:

```csharp
if (!string.IsNullOrWhiteSpace(search))
{
    var trimmedSearch = search.Trim();
    if (trimmedSearch.StartsWith('#'))
    {
        // Hledání hashtagu (#Česko) funguje stejně jako filtr podle štítku
        var normalizedTag = NormalizeTag(trimmedSearch);
        query = query.Where(a => a.Tags.Any(t => t.SearchName == normalizedTag));
    }
    else
    {
        var normalizedSearch = SearchNormalizer.Normalize(trimmedSearch);
        query = query.Where(a =>
            a.SearchTitle.Contains(normalizedSearch) ||
            a.SearchPerex.Contains(normalizedSearch) ||
            a.SearchContent.Contains(normalizedSearch));
    }
}

// "#Česko", "česko" i "ČESKO" -> "cesko"
private static string NormalizeTag(string tag)
{
    return SearchNormalizer.Normalize(tag.Trim().TrimStart('#').Trim());
}
```

| Hledaný výraz | Po normalizaci | Najde |
| :--- | :--- | :--- |
| `český`, `Český`, `ČESKÝ`, `cesky` | `cesky` | článek „Český hokejový tým…“ |
| `#Česko`, `#česko`, `#ČESKO` | štítek `cesko` | články se štítkem Česko |
| odkaz `/?tag=Česko` | štítek `cesko` | články se štítkem Česko |

Celý dotaz je LINQ, v projektu není žádný ručně psaný SQL (`FromSqlRaw`, `ExecuteSqlRaw` ani SQL v migracích).

## 12. Mazání kategorie s články

**Soubory:**
- `src/Zpravodajstvi.Infrastructure/Data/ApplicationDbContext.cs` (vazba `Restrict`)
- `src/Zpravodajstvi.Application/Services/CategoryService.cs` (`DeleteCategoryAsync`)
- `src/Zpravodajstvi.Presentation/Areas/Admin/Controllers/CategoryController.cs` (`DeleteConfirmed`)
- `src/Zpravodajstvi.Presentation/Areas/Admin/Views/Category/Delete.cshtml`

Každý článek musí patřit do kategorie (`Article.CategoryId` není nullable). Vazba `Category` → `Article` má proto `DeleteBehavior.Restrict` a databáze smazání kategorie s články odmítne (SQLite Error 19: FOREIGN KEY constraint failed). Aby se uživatel místo chyby 500 dozvěděl, co dělat, služba počet článků zkontroluje předem a vyhodí výjimku se srozumitelnou hláškou:

```csharp
var articlesCount = category.Articles?.Count ?? 0;
if (articlesCount > 0)
{
    throw new InvalidOperationException(GetCategoryNotEmptyMessage(articlesCount));
}

await _repository.DeleteAsync(category);
```

Controller výjimku zachytí a vrátí stránku mazání s hláškou:

```csharp
catch (InvalidOperationException ex)
{
    // Kategorie ještě obsahuje články – místo chyby 500 vrátíme stránku mazání se srozumitelnou hláškou
    var category = await _categoryService.GetCategoryByIdAsync(id);
    // ...
    ModelState.AddModelError(string.Empty, ex.Message);
    return View(category);
}
```

Hláška respektuje české skloňování: „obsahuje 1 článek … Nejprve ho přesuňte nebo smažte.“, „obsahuje 3 články …“, „obsahuje 5 článků … Nejprve je přesuňte nebo smažte.“ Stránka mazání ji zobrazí ve shrnutí validace:

```cshtml
<div asp-validation-summary="ModelOnly" class="alert alert-warning mt-3" role="alert"></div>
```

Články lze přesunout do jiné kategorie v jejich úpravě (`/Admin/Article/Edit/{id}`). Prázdnou kategorii služba smaže a controller přesměruje na seznam kategorií.

## 13. Testy

**Složka:** `tests/Zpravodajstvi.Tests` (xUnit + Moq). Spuštění: `dotnet test Zpravodajstvi.sln`. Testů je 91.

| Soubor | Co ověřuje |
| :--- | :--- |
| `ValidationTests/NoProfanityAttributeTests.cs` | vulgární slova se zamítnou, běžná slova „sekundu“ a „operace“ projdou, vlastní seznam slov |
| `ServicesTests/CommentServiceTests.cs` | přidání komentáře s autorem dohledaným službou, neznámý uživatel, řazení, aktuální jméno z `Comment.User`, vlastník podle `UserId`, moderátor smí upravit cizí komentář, komentář bez `UserId` smaže jen moderátor |
| `ServicesTests/ArticleServiceTests.cs` | nový článek dostane autora, štítky a obrázek, úprava nahradí obrázek a smaže starý soubor, autor článku i komentářů v detailu, „Neznámý autor“, smazání článku i se soubory |
| `ServicesTests/CategoryServiceTests.cs` | kategorie s články se nesmaže a hláška má správný tvar (1/3/5 článků), prázdná se smaže, přejmenování |
| `ControllersTests/CommentsControllerTests.cs` | autor se bere z claimu, nevalidní přidání a úprava vrátí detail článku, Edit a Delete předávají službě `Id` uživatele, Admin i Redaktor jsou moderátoři, cizí komentář vrátí `Forbid` |
| `ControllersTests/AdminArticleControllerTests.cs` | autorem nového článku je přihlášený uživatel, nevalidní formulář se vrátí s výběrem kategorií a štítků, úprava pošle službě `UpdateArticleDto` |
| `ControllersTests/AccountControllerTests.cs` | změna hesla: služba dostane `Id` přihlášeného uživatele, nevalidní formulář službu nevolá, chyba služby se zobrazí ve formuláři |
| `ControllersTests/AuthorizationTests.cs` | účty a role jen pro Admin, obsah pro Admin a Redaktor, žádná akce oprávnění neobchází, test pokrývá všechny controllery administrace, profil a změna hesla vyžadují přihlášení |
| `ControllersTests/CategoryControllerTests.cs` | kategorie s články vrátí stránku mazání s hláškou, prázdná se smaže, úprava s validací |
| `ControllersTests/HomeControllerTests.cs` | výpis a detail článku, neexistující článek vrátí 404 |
| `RepositoriesTests/ArticleRepositoryTests.cs` | vyhledávání `český`/`Český`/`ČESKÝ`/`cesky`/`#Česko`, filtr štítku a autora, načtení autora článku i komentářů přes `Include` nad skutečnou SQLite v paměti |
| `IdentityTests/IdentityServicesTests.cs` | `UserService`, `RoleService` a `AccountService` se skutečným Identity nad SQLite: změna vlastního hesla (správné i špatné současné heslo), úprava účtu administrátorem nemění `PasswordHash`, admin si neodebere roli ani nesmaže svůj účet, po smazání uživatele zůstanou články a komentáře bez autora, systémové role nejdou přejmenovat ani smazat |

Testy controllerů a služeb pracují s mocky rozhraní (služby, repozitáře), takže nepotřebují databázi. Testy repozitáře a služeb Identity vytvářejí databázi metodou `Migrate()`, takže zároveň ověřují, že se všechny migrace dají aplikovat na prázdnou databázi.

## 14. Opravené chyby

### Chyba 1: Čtenář dostal při mazání vlastního komentáře chybu 403

- **Projev:** Po kliknutí na „Smazat“ u vlastního komentáře se zobrazila stránka „Přístup odepřen“.
- **Příčina:** `CommentsController.Delete` předával službě `User.Identity.Name`, což je e-mail, a služba ho porovnávala se jménem autora komentáře (`AuthorName`, tj. `FullName`). E-mail se se jménem nikdy neshodoval.
- **Oprava:** Vlastník se určuje podle `Id` uživatele: controller čte `User.FindFirstValue(ClaimTypes.NameIdentifier)` a `CommentService` ho porovná s `Comment.UserId` (viz bod 5 a [kapitola 8](#8-účty-role-a-oprávnění)).
- **Soubory:** `Presentation/Controllers/CommentsController.cs`, `Application/Services/CommentService.cs`, `Application/Interfaces/ICommentService.cs`.

### Chyba 2: Úprava komentáře propouštěla vulgární slova

- **Projev:** Při úpravě šlo do komentáře vložit text, který by při vytvoření neprošel.
- **Příčina:** Akce `Edit` přijímala `string content` bez atributu `[NoProfanity]` a nekontrolovala `ModelState`. DTO pro úpravu (`Updatecomment`) existovalo, ale nikde se nepoužívalo.
- **Oprava:** DTO je přejmenované na `UpdateComment` a má stejná pravidla jako při vytvoření (`[Required]`, `[StringLength(1000, MinimumLength = 3)]`, `[NoProfanity]`). `Edit` ho přijímá s prefixem `EditComment`, kontroluje `ModelState.IsValid` a při chybě vrátí detail článku s hláškou u upravovaného komentáře, stejně jako `Add`.
- **Soubory:** `Application/DTOs/UpdateComment.cs` (dříve `Updatecomment.cs`), `Application/DTOs/ArticleDtos.cs` (`EditComment`), `Presentation/Controllers/CommentsController.cs`, `Presentation/Views/Home/Details.cshtml`.

### Chyba 3: Běžná slova „sekundu“ a „operace“ byla označena jako vulgární

- **Projev:** Komentář se slovem „sekundu“ nebo „operace“ byl zamítnut.
- **Příčina:** `HasProfanity` kromě regulárního výrazu s `\b` testoval i `normalizedInput.Contains(normalizedWord)`, tedy výskyt zakázaného slova jako podřetězce („se**kundu**“, „o**pera**ce“). Stejnou chybu měla klientská validace v `custom-validation.js` (`indexOf`), takže prohlížeč formulář zablokoval ještě před odesláním.
- **Oprava:** Na serveru zůstalo jen hledání celých slov přes `\b` (bez ohledu na velikost písmen, po odstranění diakritiky). V JavaScriptu nová funkce `containsWholeWord` hledá celá slova s unicodovými hranicemi. Slova „sekundu“ a „operace“ jsou v testech jako příklady textu, který musí projít.
- **Soubory:** `Application/Validation/NoProfanityAttribute.cs`, `Presentation/wwwroot/js/custom-validation.js`, `tests/.../ValidationTests/NoProfanityAttributeTests.cs`.

### Chyba 4: Komentář šlo odeslat pod cizím jménem

- **Projev:** Úpravou HTML (odstraněním `readonly`) nebo vlastním POST požadavkem šlo komentář podepsat libovolným jménem.
- **Příčina:** `readonly` chrání pole jen v prohlížeči. Server doplnil jméno z účtu jen tehdy, když pole přišlo prázdné.
- **Oprava:** `CreateCommentDto` už vlastnost `AuthorName` nemá, takže se jméno z formuláře nemá kam navázat. `CommentsController.Add` vezme `Id` přihlášeného uživatele z claimu a `CommentService` si k němu jméno dohledá přes `IUserService`. Pole se jménem ve formuláři je jen informativní a nemá atribut `name`.
- **Soubory:** `Application/DTOs/CommentDtos.cs`, `Application/Services/CommentService.cs`, `Presentation/Controllers/CommentsController.cs`, `Presentation/Controllers/HomeController.cs`, `Presentation/Views/Home/Details.cshtml`.

### Chyba 5: Uživatel se stejným jménem mohl upravovat cizí komentáře

- **Projev:** Kdo se zaregistroval se stejným celým jménem jako autor komentáře, mohl jeho komentář upravit i smazat.
- **Příčina:** Vlastník se určoval porovnáním jmen (`AuthorName` vs. `FullName`), a jméno není jedinečné.
- **Oprava:**
  - entita `Comment` má pole `UserId` s cizím klíčem na `AspNetUsers` (`OnDelete(SetNull)`) a indexem, později přibyla i navigační vlastnost `Comment.User`,
  - při vytvoření komentáře se `UserId` vyplní z přihlášeného účtu, ukázková data ho vyplňují také,
  - `Edit` i `Delete` kontrolují vlastníka podle `UserId` (`CommentService.IsOwner`), Admin (a po úpravách z [kapitoly 15](#15-úpravy-podle-kontrolního-seznamu) i Redaktor) smí upravovat a mazat všechny komentáře,
  - u komentářů, které v databázi už byly, se `UserId` doplnilo jednorázově při převodu databáze podle shody `AuthorName` s `FullName`, ale jen když byla shoda jednoznačná (právě jeden uživatel s tím jménem). Ostatní komentáře zůstaly s `UserId = null` a spravuje je jen moderátor.
- **Soubory:** `Domain/Entities/Comment.cs`, `Infrastructure/Data/ApplicationDbContext.cs`, `Infrastructure/Migrations/20261012203511_AddCommentUserId.cs`, `Infrastructure/Data/DbInitializer.cs`, `Application/Services/CommentService.cs`, `Application/DTOs/CommentDtos.cs` (`CommentDto.UserId`), `Application/Services/ArticleService.cs`, `Presentation/Views/Home/Details.cshtml`.
- **Navíc:** Projekt dříve vytvářel databázi přes `EnsureCreated()` a migrace neměl. Přibyla výchozí migrace `InitialCreate` a `DbInitializer` teď volá `MigrateAsync()` (viz [kapitola 5](#5-migrace-a-výchozí-data)).

### Chyba 6: Smazání kategorie s články skončilo chybou 500

- **Projev:** „SQLite Error 19: FOREIGN KEY constraint failed“ po potvrzení smazání kategorie, která obsahovala články.
- **Příčina:** Vazba `Category` → `Article` má `DeleteBehavior.Restrict`, ale `CategoryController.DeleteConfirmed` mazal kategorii bez kontroly článků.
- **Oprava:** Před smazáním se zjistí počet článků. Pokud kategorie nějaké obsahuje, nesmaže se a vrátí se stránka mazání s hláškou „Rubriku nelze smazat, obsahuje N článků. Nejprve je přesuňte nebo smažte.“ (se správným tvarem pro 1, 2–4 a 5+ článků). Chování `Restrict` zůstalo. Kontrola je teď ve službě `CategoryService` (viz [kapitola 12](#12-mazání-kategorie-s-články)). Na stránce mazání byla opravena i věta, podle které mohly po smazání zůstat články bez kategorie.
- **Soubory:** `Application/Services/CategoryService.cs`, `Presentation/Areas/Admin/Controllers/CategoryController.cs`, `Presentation/Areas/Admin/Views/Category/Delete.cshtml`.

### Chyba 7: Formuláře článků přijímaly požadavek bez antiforgery tokenu

- **Projev:** Na `/Admin/Article/Create` a `/Admin/Article/Edit` šlo poslat POST bez tokenu (riziko CSRF).
- **Příčina:** Akce `Create` a `Edit` (POST) v `Admin/ArticleController` neměly atribut `[ValidateAntiForgeryToken]`.
- **Oprava:** Obě akce atribut dostaly. Formuláře `Create.cshtml` a `Edit.cshtml` token posílají, protože používají tag helper formuláře. Ostatní POST akce byly zkontrolované a atribut už měly (přehled v [kapitole 10](#10-ochrana-formulářů-antiforgery-tokenem)). Požadavek bez tokenu teď skončí odpovědí 400.
- **Soubory:** `Presentation/Areas/Admin/Controllers/ArticleController.cs`.

### Chyba 8: Hledání „#Česko“ nebo „český“ nic nenašlo

- **Projev:** Hledání slov s českými znaky (a filtr štítku „Česko“) nevracelo žádné články.
- **Příčina:** Dotaz porovnával `Title.ToLower()` a `Name.ToLower()`, což EF Core přeloží na SQLite `lower()`. Ta převádí jen ASCII, takže „Č“ zůstalo velké a neshodovalo se s hledaným „č“. Vyhledávací pole navíc s hashtagy vůbec nepočítalo.
- **Oprava:** Články mají normalizované sloupce `SearchTitle`, `SearchPerex`, `SearchContent` a štítky `SearchName` (malá písmena bez diakritiky, `SearchNormalizer`). Plní se automaticky při každém uložení (`ApplicationDbContext.SaveChanges`), existující záznamy se naplnily jednorázově při převodu databáze. Repozitář normalizuje hledaný výraz stejně a výraz začínající `#` hledá jako štítek. `#Česko`, `český`, `Český`, `ČESKÝ` i `cesky` teď fungují stejně.
- **Soubory:** `Domain/Search/SearchNormalizer.cs`, `Domain/Entities/Article.cs`, `Domain/Entities/Tag.cs`, `Infrastructure/Data/ApplicationDbContext.cs`, `Infrastructure/Repositories/ArticleRepository.cs`, `Infrastructure/Migrations/20261012203628_AddSearchFields.cs`, `tests/.../RepositoriesTests/ArticleRepositoryTests.cs`.

## 15. Úpravy podle kontrolního seznamu

Projekt byl zkontrolován podle požadavků semestrální práce. Tato tabulka shrnuje, co se kvůli nim změnilo:

| Požadavek | Co se změnilo |
| :--- | :--- |
| Článek i komentář mají cizí klíč a navigační vlastnost na uživatele | `ApplicationUser` je v doméně, `Article` má `AuthorId` + `Author`, `Comment` má `User`; vazby v DbContextu (`SetNull`), migrace `AddArticleAuthor` |
| Autor se zobrazuje všude, kde jsou články a komentáře | výpis a detail článku, komentáře, profil, administrace článků a komentářů; repozitáře načítají autora přes `Include` / `ThenInclude` |
| Autora určuje server | `Id` z claimu `NameIdentifier`, službě ho předá controller; formuláře autora neobsahují |
| `UserManager`, `SignInManager`, `RoleManager` jen v Infrastructure | rozhraní `IAccountService`, `IUserService`, `IRoleService` v Application, implementace v `Infrastructure/Identity`; controllery, views i `_LoginPartial` je nepoužívají |
| Presentation používá Infrastructure jen kvůli DI | registrace v `AddInfrastructure()`, názvy rolí přesunuty z `DbInitializer` do `AppRoles` v doméně |
| Admin nemění heslo uživatele | z úpravy účtu zmizelo pole „Nové heslo“ i jeho zpracování, `UserService` `PasswordHash` nepřepisuje; každý uživatel si mění vlastní heslo sám (`IAccountService.ChangePasswordAsync`) |
| Žádný SQL v kódu | z `DbInitializer` zmizel převod staré databáze přes `ExecuteSqlRawAsync`, migrace `AddCommentUserId` a `AddSearchFields` obsahují jen operace EF Core; databáze v repozitáři byla předtím jednou převedena |
| Generic repository | `IRepository<T>` v doméně, `Repository<T>` v Infrastructure, specifické repozitáře z nich dědí; kategorie a štítky mají vlastní repozitáře |
| Tenké controllery, logika ve službách | ukládání obrázků v `IFileStorageService`, kontrola mazání kategorie v `CategoryService`, správa účtů a rolí ve službách |
| Administrace všech entit včetně Edit, uživatelů a rolí | přibyla úprava kategorie, správa štítků, moderování komentářů, vytvoření účtu a správa rolí; štítky lze přiřadit článku |
| Role Admin, Manager, User | `Redaktor` plní roli Manager (spravuje obsah včetně moderování komentářů, ne účty a role), `Ctenar` roli User |
| Validace na serveru i v prohlížeči | formuláře článků mají validační atributy a načítají validační skripty, kategorie a štítky mají ViewModely s `ModelState.IsValid`, formulář úpravy komentáře pod článkem se kontroluje i v prohlížeči; druhý vlastní atribut `[ImageFile]` |
| Adaptivní vzhled | tabulky administrace v `table-responsive`, společná navigace administrace |
| Logování | `ILogger` ve všech controllerech, které mění data (účty, komentáře, administrace) |
| Nahrávání souborů | povolené jen obrázky do 5 MB, nový obrázek nahradí starý a smaže jeho soubor, obrázky nahrané za běhu vrací `UseStaticFiles()` |
| Oprávnění podle rolí | `UserController` a `RoleController` jen `Admin`, obsah `Admin,Redaktor`; hlídá test `AuthorizationTests` |
| Migrace odpovídají modelu | databáze vytvořená znovu přes `dotnet ef database update`, seeding ověřen, `has-pending-model-changes` a zkušební `migrations add` bez změn |
| Unit testy | 91 testů, nově služby článků a kategorií, administrace článků, změna hesla, oprávnění a služby Identity nad SQLite |
