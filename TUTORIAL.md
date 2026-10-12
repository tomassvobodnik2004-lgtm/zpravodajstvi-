# Jak funguje Zpravodajství

Tento návod vysvětluje, jak funguje hotový kód aplikace Zpravodajství: z jakých částí se skládá, kudy prochází požadavek a kde v kódu najdete jednotlivé funkce. U každé části je uvedeno, ve kterých souborech leží, a krátká ukázka kódu. Ukázky jsou zkrácené, vynechané části označuje `// ...`. Instalace a spuštění jsou popsané v [README.md](README.md).

## Obsah

1. [Architektura: vrstvy a projekty](#1-architektura-vrstvy-a-projekty)
2. [Start aplikace (Program.cs)](#2-start-aplikace-programcs)
3. [Cesta požadavku: controller, služba, repozitář, view](#3-cesta-požadavku-controller-služba-repozitář-view)
4. [Datový model a DbContext](#4-datový-model-a-dbcontext)
5. [Migrace a výchozí data](#5-migrace-a-výchozí-data)
6. [Rubriky a články](#6-rubriky-a-články)
7. [Komentáře](#7-komentáře)
8. [Přihlášení, role a vlastník komentáře](#8-přihlášení-role-a-vlastník-komentáře)
9. [Filtr vulgárních slov](#9-filtr-vulgárních-slov)
10. [Ochrana formulářů antiforgery tokenem](#10-ochrana-formulářů-antiforgery-tokenem)
11. [Vyhledávání s normalizací českých znaků](#11-vyhledávání-s-normalizací-českých-znaků)
12. [Mazání rubriky s články](#12-mazání-rubriky-s-články)
13. [Testy](#13-testy)
14. [Opravené chyby](#14-opravené-chyby)

---

## 1. Architektura: vrstvy a projekty

Řešení `Zpravodajstvi.sln` obsahuje čtyři projekty aplikace a jeden testovací projekt. Každý projekt je jedna vrstva s vlastní odpovědností:

| Projekt | Odpovědnost | Závisí na |
| :--- | :--- | :--- |
| `src/Zpravodajstvi.Domain` | entity (`Article`, `Category`, `Comment`, `Tag`, `Image`), rozhraní repozitářů, `SearchNormalizer` | ničem |
| `src/Zpravodajstvi.Application` | služby (`ArticleService`, `CategoryService`, `CommentService`), DTO, validační atribut `[NoProfanity]` | Domain |
| `src/Zpravodajstvi.Infrastructure` | `ApplicationDbContext`, migrace, repozitáře, Identity (`ApplicationUser`), `DbInitializer` | Application, Domain |
| `src/Zpravodajstvi.Presentation` | webová aplikace MVC: controllery, views, administrace (Area `Admin`), JavaScript | Application, Infrastructure |
| `tests/Zpravodajstvi.Tests` | unit testy a test vyhledávání nad SQLite | všech čtyřech |

```text
Presentation ──► Application ──► Domain
     │                              ▲
     └──► Infrastructure ───────────┘   (Infrastructure používá i Application)
```

Doména definuje **rozhraní** repozitářů a Infrastructure je **implementuje**:

```csharp
// src/Zpravodajstvi.Domain/Interfaces/ICommentRepository.cs
public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetByArticleIdAsync(int articleId);
    Task<Comment> AddAsync(Comment comment);
    Task<Comment?> GetByIdAsync(int commentId);
    Task<Comment> UpdateAsync(Comment comment);
    Task<Comment> DeleteAsync(Comment comment);
}
```

Služby v Application pracují jen s rozhraním, takže nevědí nic o EF Core ani o SQLite. Konkrétní třídu jim dodá kontejner závislostí (Dependency Injection) v `Program.cs`. Díky tomu lze služby testovat s falešnými repozitáři (Moq).

## 2. Start aplikace (Program.cs)

**Soubor:** `src/Zpravodajstvi.Presentation/Program.cs`

Při startu se nastaví logování Serilog, databáze, Identity, služby a směrování. Nejdůležitější části:

```csharp
// 1. Konfigurace databáze SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=zpravodajstvi.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// 2. Konfigurace ASP.NET Core Identity s rolemi
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => { /* ... pravidla hesla ... */ })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    // ...
});

// 3. Registrace aplikačních služeb a repozitářů v DI
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddControllersWithViews();
var app = builder.Build();

// 5. Inicializace databáze a seeding rolí i ukázkových článků
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedDataAsync(scope.ServiceProvider);
}
```

- `AddScoped` znamená jednu instanci na jeden HTTP požadavek. Všechny repozitáře v rámci požadavku sdílejí stejný `ApplicationDbContext`.
- `AddIdentity` přidá správu uživatelů, hesel, rolí a přihlašovací cookie. Uživatele ukládá do tabulek `AspNetUsers`, `AspNetRoles` a dalších ve stejné databázi.
- `DbInitializer.SeedDataAsync` aplikuje migrace a naplní výchozí data (viz [kapitola 5](#5-migrace-a-výchozí-data)).
- Na konci jsou dvě routy: `{area:exists}/{controller=Home}/{action=Index}/{id?}` pro administraci (`/Admin/Article/...`) a výchozí `{controller=Home}/{action=Index}/{id?}`.

## 3. Cesta požadavku: controller, služba, repozitář, view

Jako příklad poslouží vyhledávání `GET /?search=český`:

1. **Routa** pošle požadavek na `HomeController.Index` (`src/Zpravodajstvi.Presentation/Controllers/HomeController.cs`). Model binding vezme parametr `search` z adresy.
2. **Controller** zavolá službu a výsledek předá do view:

   ```csharp
   public async Task<IActionResult> Index(int? categoryId = null, string? tag = null, string? search = null)
   {
       var articles = await _articleService.GetArticlesAsync(categoryId, tag, search);
       var categories = await _articleService.GetCategoriesAsync();

       ViewBag.Categories = categories;
       ViewBag.CurrentCategory = categoryId;
       ViewBag.CurrentTag = tag;
       ViewBag.CurrentSearch = search;

       return View(articles);
   }
   ```

3. **Služba** `ArticleService.GetArticlesAsync` (`src/Zpravodajstvi.Application/Services/ArticleService.cs`) si vyžádá entity z repozitáře a převede je na DTO:

   ```csharp
   var articles = await _articleRepository.GetAllAsync(categoryId, tag, search);

   return articles.Select(a => new ArticleListDto
   {
       Id = a.Id,
       Title = a.Title,
       CategoryName = a.Category?.Name ?? "Bez kategorie",
       CommentsCount = a.Comments?.Count ?? 0,
       Tags = a.Tags?.Select(t => t.Name).ToList() ?? new List<string>()
       // ...
   });
   ```

4. **Repozitář** `ArticleRepository.GetAllAsync` (`src/Zpravodajstvi.Infrastructure/Repositories/ArticleRepository.cs`) sestaví LINQ dotaz, EF Core ho přeloží do SQL a SQLite vrátí řádky (podrobně v [kapitole 11](#11-vyhledávání-s-normalizací-českých-znaků)).
5. **View** `src/Zpravodajstvi.Presentation/Views/Home/Index.cshtml` vykreslí seznam DTO do HTML pomocí Razoru a tag helperů.

### Modely, DTO a ViewModely

Views nedostávají entity EF Core, ale **DTO** (Data Transfer Object) s daty, která potřebují. Formulářové DTO navíc nesou validační atributy.

| Třída | Soubor | K čemu slouží |
| :--- | :--- | :--- |
| `ArticleListDto`, `ArticleDetailDto`, `ImageDto`, `CategoryDto`, `CreateArticleDto` | `src/Zpravodajstvi.Application/DTOs/ArticleDtos.cs` | výpis a detail článku, rubriky, vstup pro vytvoření článku |
| `CreateCommentDto`, `CommentDto` | `src/Zpravodajstvi.Application/DTOs/CommentDtos.cs` | formulář nového komentáře, zobrazení komentáře |
| `UpdateComment` | `src/Zpravodajstvi.Application/DTOs/UpdateComment.cs` | formulář úpravy komentáře |
| `ArticleCreateViewModel`, `ArticleEditViewModel`, `UserViewModel`, `EditUserViewModel` | `src/Zpravodajstvi.Presentation/Areas/Admin/Models/` | formuláře administrace (včetně nahrávaného souboru `IFormFile`) |
| `RegisterViewModel`, `LoginViewModel` | `src/Zpravodajstvi.Presentation/Models/AccountViewModels.cs` | registrace a přihlášení |

`ArticleDetailDto` slouží jako model stránky detailu. Kromě článku a komentářů obsahuje i data pro formuláře komentářů:

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
| `Views/Shared/_Layout.cshtml` | společná kostra stránky, navigace, přepínač motivu |
| `Views/Shared/_LoginPartial.cshtml` | jméno a role přihlášeného uživatele, odkaz do administrace, odhlášení |
| `Views/Home/Index.cshtml` | výpis článků, vyhledávací formulář, tlačítka rubrik, odkazy štítků |
| `Views/Home/Details.cshtml` | detail článku, formulář nového komentáře, seznam komentářů s úpravou a mazáním |
| `Views/Account/*.cshtml` | přihlášení, registrace, stránka „Přístup odepřen“ |
| `Areas/Admin/Views/**` | administrace článků, rubrik a uživatelů |

Odkazy a formuláře se generují tag helpery, takže adresy odpovídají routám. Například odkaz na štítek ve výpisu:

```cshtml
<a asp-action="Index" asp-route-tag="@tag" class="badge bg-light text-secondary text-decoration-none border me-1">
    #@tag
</a>
```

## 4. Datový model a DbContext

**Soubory:** `src/Zpravodajstvi.Domain/Entities/*.cs`, `src/Zpravodajstvi.Infrastructure/Identity/ApplicationUser.cs`, `src/Zpravodajstvi.Infrastructure/Data/ApplicationDbContext.cs`

| Entita | Hlavní vlastnosti |
| :--- | :--- |
| `Article` | `Title`, `Perex`, `Content`, `CreatedAt`, `UpdatedAt`, `SearchTitle`, `SearchPerex`, `SearchContent`, `CategoryId`, kolekce `Comments`, `Tags`, `Images` |
| `Category` | `Name`, kolekce `Articles` |
| `Comment` | `AuthorName`, `Content`, `CreatedAt`, `UserId` (autor), `ArticleId` |
| `Tag` | `Name`, `SearchName`, kolekce `Articles` |
| `Image` | `Url`, `Caption`, `ArticleId` |
| `ApplicationUser` | dědí z `IdentityUser` (`Id`, `UserName`, `Email`, hash hesla…), navíc `FullName` a `CreatedAt` |

`ApplicationDbContext` dědí z `IdentityDbContext<ApplicationUser>`, takže kromě tabulek aplikace obsahuje i tabulky Identity. Vazby a chování při mazání nastavuje `OnModelCreating`:

| Vazba | Typ | Při smazání nadřazeného záznamu |
| :--- | :--- | :--- |
| `Category` → `Article` | 1:N | `Restrict` – rubriku s články databáze smazat nedovolí |
| `Article` → `Comment` | 1:N | `Cascade` – smažou se i komentáře |
| `Article` → `Image` | 1:N | `Cascade` – smažou se i záznamy obrázků |
| `Article` ↔ `Tag` | M:N přes tabulku `ArticleTag` | smaže se jen vazba |
| `ApplicationUser` → `Comment` | 1:N přes `Comment.UserId` | `SetNull` – komentář zůstane, jen bez vlastníka |

Vazba komentáře na uživatele nemá navigační vlastnost, protože doména nesmí odkazovat na `ApplicationUser` z vrstvy Infrastructure. Cizí klíč se proto nastavuje jen v DbContextu:

```csharp
// Konfigurace relace 1:N ApplicationUser -> Comments (autor komentáře).
// Po smazání účtu komentáře zůstanou s UserId = null a spravovat je může jen administrátor.
modelBuilder.Entity<Comment>()
    .HasOne<ApplicationUser>()
    .WithMany()
    .HasForeignKey(c => c.UserId)
    .OnDelete(DeleteBehavior.SetNull);
```

DbContext navíc přepisuje `SaveChanges` a `SaveChangesAsync`, aby před každým uložením doplnil normalizovaná pole pro vyhledávání (viz [kapitola 11](#11-vyhledávání-s-normalizací-českých-znaků)).

## 5. Migrace a výchozí data

**Soubory:** `src/Zpravodajstvi.Infrastructure/Migrations/`, `src/Zpravodajstvi.Infrastructure/Data/DbInitializer.cs`

Schéma databáze popisují migrace EF Core. Každá migrace má metodu `Up` (co se má v databázi změnit) a `Down` (jak změnu vrátit), soubor `ApplicationDbContextModelSnapshot.cs` drží aktuální podobu modelu.

| Migrace | Co dělá |
| :--- | :--- |
| `20261012203328_InitialCreate` | výchozí schéma: články, rubriky, štítky, obrázky, komentáře a tabulky Identity |
| `20261012203511_AddCommentUserId` | přidá `Comments.UserId` s cizím klíčem na `AspNetUsers` a indexem, doplní autory existujících komentářů |
| `20261012203628_AddSearchFields` | přidá `Articles.SearchTitle`, `SearchPerex`, `SearchContent` a `Tags.SearchName` a naplní je pro existující záznamy |

Migrace se aplikují automaticky při startu v `DbInitializer.SeedDataAsync`:

```csharp
// Zajištění existence databáze a aplikace migrací (existující data zůstanou zachována)
await MarkLegacyDatabaseAsMigratedAsync(context);
await context.Database.MigrateAsync();
```

Starší verze aplikace vytvářela databázi přes `EnsureCreated()`, který nezapisuje historii migrací. `MigrateAsync()` by na takové databázi zkusil znovu vytvořit existující tabulky a skončil chybou. Proto `MarkLegacyDatabaseAsMigratedAsync` nejdřív zjistí, zda databáze má tabulky, ale nemá tabulku `__EFMigrationsHistory`. Pokud ano, zapíše do historie první migraci, protože schéma takové databáze jí přesně odpovídá:

```csharp
if (!await databaseCreator.ExistsAsync()
    || !await databaseCreator.HasTablesAsync()
    || await historyRepository.ExistsAsync())
{
    return;
}

var initialMigration = context.Database.GetMigrations().First();

await using var transaction = await context.Database.BeginTransactionAsync();
await context.Database.ExecuteSqlRawAsync(historyRepository.GetCreateIfNotExistsScript());
await context.Database.ExecuteSqlRawAsync(
    historyRepository.GetInsertScript(new HistoryRow(initialMigration, ProductInfo.GetVersion())));
await transaction.CommitAsync();
```

`MigrateAsync()` pak aplikuje jen `AddCommentUserId` a `AddSearchFields`. Obě migrace data nemažou, jen přidávají sloupce a doplňují do nich hodnoty.

Po migracích `DbInitializer` vytvoří tři role (`Admin`, `Redaktor`, `Ctenar`), tři výchozí účty (metoda `EnsureUserAsync`) a do prázdné databáze ukázkové rubriky, štítky, články a komentáře. Ukázkové komentáře se rovnou svážou s autory:

```csharp
var ctenar = await userManager.FindByEmailAsync("ctenar@zpravodajstvi.cz");
// ...
new()
{
    AuthorName = "Petr Čtenář",
    UserId = ctenar?.Id,
    Content = "Výborný a přehledný článek. ...",
    CreatedAt = DateTime.UtcNow.AddHours(-4)
},
```

## 6. Rubriky a články

**Soubory:**
- `src/Zpravodajstvi.Presentation/Areas/Admin/Controllers/ArticleController.cs`, `CategoryController.cs`
- `src/Zpravodajstvi.Application/Services/ArticleService.cs`, `CategoryService.cs`
- `src/Zpravodajstvi.Infrastructure/Repositories/ArticleRepository.cs` (obsahuje i operace s rubrikami)
- `src/Zpravodajstvi.Presentation/Areas/Admin/Views/Article/`, `Areas/Admin/Views/Category/`

Administrace leží v Area `Admin` a mají do ní přístup jen role Admin a Redaktor:

```csharp
[Area("Admin")]
[Authorize(Roles = $"{DbInitializer.RoleAdmin},{DbInitializer.RoleRedaktor}")]
public class ArticleController : Controller
```

### Vytvoření a úprava článku

Formulář `Create.cshtml` posílá `ArticleCreateViewModel` (titulek, text, rubrika, volitelný obrázek). Controller uloží obrázek do `wwwroot/uploads` pod jedinečným jménem a předá data službě:

```csharp
var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ImageFile.FileName)}";
// ... zápis souboru ...
imageUrl = $"/uploads/{uniqueFileName}";

var dto = new CreateArticleDto
{
    Title = model.Title,
    Content = model.Content,
    CategoryId = model.CategoryId,
    ImageUrl = imageUrl
};
var created = await _articleService.CreateArticleAsync(dto);
```

`ArticleService.CreateArticleAsync` doplní perex (prvních 150 znaků textu, pokud perex nepřišel) a k článku připojí obrázek jako entitu `Image`:

```csharp
var perex = !string.IsNullOrWhiteSpace(dto.Perex)
    ? dto.Perex
    : (dto.Content.Length > 150 ? dto.Content[..150] + "..." : dto.Content);
```

Úprava (`ArticleController.Edit` → `ArticleService.UpdateArticleAsync`) změní titulek, text, rubriku, znovu vytvoří perex, nastaví `UpdatedAt` a případný nový obrázek přidá do galerie. Normalizovaná pole pro vyhledávání se přepočítají automaticky při uložení.

### Rubriky

`CategoryService` vrací rubriky jako `CategoryDto` včetně počtu článků (`ArticlesCount`), vytváří nové rubriky a maže prázdné. Ve veřejné části jsou rubriky tlačítka nad výpisem článků, která přidají do adresy `?categoryId=...`. Repozitář pak filtruje:

```csharp
if (categoryId.HasValue)
{
    query = query.Where(a => a.CategoryId == categoryId.Value);
}
```

Mazání rubriky, která ještě obsahuje články, popisuje [kapitola 12](#12-mazání-rubriky-s-články).

### Štítky

Štítky (`Tag`) vznikají při naplnění ukázkových dat a s články je spojuje tabulka `ArticleTag`. Ve výpisu i v detailu se zobrazují jako odkazy `#štítek`, které vedou na `/?tag=štítek`. Hledat podle štítku lze i z vyhledávacího pole zápisem `#Česko`.

## 7. Komentáře

**Soubory:**
- `src/Zpravodajstvi.Presentation/Controllers/CommentsController.cs`
- `src/Zpravodajstvi.Application/Services/CommentService.cs`
- `src/Zpravodajstvi.Application/DTOs/CommentDtos.cs`, `UpdateComment.cs`
- `src/Zpravodajstvi.Infrastructure/Repositories/CommentRepository.cs`
- `src/Zpravodajstvi.Presentation/Views/Home/Details.cshtml`

### Přidání komentáře

Formulář posílá jen `ArticleId` a `Content`. DTO `CreateCommentDto` jméno autora vůbec neobsahuje, takže ho z formuláře nelze podvrhnout. Pole se jménem ve formuláři slouží jen pro informaci a nemá atribut `name`, proto se neodesílá:

```cshtml
<input id="commentAuthorName" class="form-control" value="@Model.CurrentUserName" readonly="readonly" />
```

Autora vždy určí server podle přihlášeného účtu:

```csharp
public async Task<IActionResult> Add([Bind(Prefix = "NewComment")] CreateCommentDto model)
{
    // Autora vždy určuje server podle přihlášeného účtu (Id a jméno z databáze), jméno z formuláře se nepoužívá
    var user = await _userManager.GetUserAsync(User);
    if (user == null)
    {
        return Challenge();
    }

    var authorName = GetAuthorName(user);

    if (!ModelState.IsValid)
    {
        // ... načte článek a vrátí stránku detailu s chybami validace
        return View("~/Views/Home/Details.cshtml", article);
    }

    await _commentService.AddCommentAsync(model, user.Id, authorName);
    // ...
}
```

`CommentService.AddCommentAsync` ověří, že článek existuje, a uloží komentář s `UserId` a `AuthorName` z parametrů. `AuthorName` se ukládá jako text, aby se u komentáře zobrazovalo jméno i bez načítání uživatele.

Atribut `[Bind(Prefix = "NewComment")]` říká, že pole formuláře mají prefix `NewComment.` (např. `NewComment.Content`), protože view generuje formulář z vlastnosti `ArticleDetailDto.NewComment`.

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
    if (!ModelState.IsValid)
    {
        var article = await _articleService.GetArticleDetailsAsync(model.ArticleId);
        // ...
        article.EditComment = model;
        return View("~/Views/Home/Details.cshtml", article);
    }
    // ... kontrola vlastníka a uložení
}
```

View pak u tohoto komentáře nechá formulář otevřený (třída `show`), vrátí do něj odeslaný text a zobrazí chybu:

```cshtml
var failedEdit = Model.EditComment?.Id == comment.Id ? Model.EditComment : null;
<!-- ... -->
<textarea name="EditComment.Content" class="form-control" rows="3" required>@(failedEdit?.Content ?? comment.Content)</textarea>
@if (failedEdit != null)
{
    <span asp-validation-for="EditComment.Content" class="text-danger small"></span>
}
```

### Smazání komentáře

Tlačítko „Smazat“ odešle formulář s `id` komentáře a `articleId` na `CommentsController.Delete`. Kdo smí komentář upravit nebo smazat, popisuje následující kapitola.

## 8. Přihlášení, role a vlastník komentáře

**Soubory:**
- `src/Zpravodajstvi.Presentation/Program.cs` (Identity a cookie)
- `src/Zpravodajstvi.Presentation/Controllers/AccountController.cs` (registrace, přihlášení, odhlášení)
- `src/Zpravodajstvi.Infrastructure/Data/DbInitializer.cs` (role a výchozí účty)
- `src/Zpravodajstvi.Presentation/Areas/Admin/Controllers/UserController.cs` (správa účtů)
- `src/Zpravodajstvi.Presentation/Controllers/CommentsController.cs`, `src/Zpravodajstvi.Application/Services/CommentService.cs` (vlastník komentáře)

### Role a přístup

Názvy rolí jsou konstanty v `DbInitializer` (`RoleAdmin = "Admin"`, `RoleRedaktor = "Redaktor"`, `RoleCtenar = "Ctenar"`). Registrace (`AccountController.Register`) přidělí novému uživateli roli `Ctenar` a rovnou ho přihlásí. Přístup hlídají atributy `[Authorize]`:

| Kde | Atribut | Kdo má přístup |
| :--- | :--- | :--- |
| `CommentsController.Add` | `[Authorize(Roles = "Ctenar,Redaktor,Admin")]` (zapsáno konstantami) | přihlášení s rolí |
| `CommentsController.Edit`, `Delete` | `[Authorize]` + kontrola vlastníka v `CommentService` | autor komentáře nebo Admin |
| `Admin/ArticleController`, `Admin/CategoryController` | `[Authorize(Roles = "Admin,Redaktor")]` | redakce |
| `Admin/UserController` | `[Authorize(Roles = "Admin")]` | jen administrátor |

Nepřihlášeného uživatele přesměruje cookie autentizace na `/Account/Login`, přihlášeného bez oprávnění na `/Account/AccessDenied`.

### Co obsahuje přihlášený uživatel

Po přihlášení obsahuje cookie identitu s claimy. Pro vlastnictví komentářů jsou důležité dva:

| Claim | Hodnota | Jak ho získat |
| :--- | :--- | :--- |
| `ClaimTypes.NameIdentifier` | `Id` uživatele (GUID z `AspNetUsers.Id`) | `User.FindFirstValue(ClaimTypes.NameIdentifier)` |
| `ClaimTypes.Name` | `UserName`, v této aplikaci **e-mail** | `User.Identity.Name` |

Jméno uživatele (`FullName`) ani e-mail nejsou vhodné k určení vlastníka: e-mail se nerovná jménu u komentáře a stejné celé jméno může mít více lidí. Jediný jednoznačný a neměnný údaj je `Id`.

### Kontrola vlastníka podle UserId

Controller vezme `Id` přihlášeného uživatele z claimu a předá ho službě:

```csharp
// Vlastník komentáře se pozná podle Id přihlášeného uživatele (claim NameIdentifier)
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
var isAdmin = User.IsInRole("Admin") || User.IsInRole(DbInitializer.RoleAdmin);

await _commentService.DeleteCommentAsync(id, userId, isAdmin);
```

Služba porovná `Id` s `Comment.UserId`. Administrátor smí všechno:

```csharp
// Kontrola oprávnění: Admin může vše, ostatní jen své vlastní komentáře (porovnání podle Id uživatele)
if (!isAdmin && !IsOwner(comment, currentUserId))
{
    throw new UnauthorizedAccessException("Nemáte oprávnění smazat tento komentář.");
}

// Komentář bez UserId (autora nešlo určit) nepatří nikomu – spravuje ho jen administrátor
private static bool IsOwner(Comment comment, string? currentUserId)
{
    return !string.IsNullOrEmpty(comment.UserId)
        && string.Equals(comment.UserId, currentUserId, StringComparison.Ordinal);
}
```

Výjimku `UnauthorizedAccessException` controller zachytí a vrátí `Forbid()`, což uživatele přesměruje na stránku „Přístup odepřen“. Stejně funguje úprava (`UpdateCommentAsync`).

Tlačítka „Upravit“ a „Smazat“ zobrazí view jen tomu, kdo komentář spravovat smí. Rozhoduje stejné pravidlo:

```cshtml
@{
    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
}
<!-- ... v cyklu přes komentáře: -->
var canManage = User.IsInRole("Admin") || (comment.UserId != null && comment.UserId == currentUserId);
```

Skrytá tlačítka jsou jen pohodlí pro uživatele, skutečnou ochranu zajišťuje kontrola v `CommentService`.

**Komentáře s `UserId = null`** jsou komentáře, u kterých se autora nepodařilo jednoznačně určit při migraci (viz [kapitola 14, chyba 5](#chyba-5-uživatel-se-stejným-jménem-mohl-upravovat-cizí-komentáře)), nebo komentáře smazaných účtů (vazba `SetNull`). Upravit a smazat je může jen administrátor.

## 9. Filtr vulgárních slov

**Soubory:**
- `src/Zpravodajstvi.Application/Validation/NoProfanityAttribute.cs` (seznam slov, kontrola na serveru)
- `src/Zpravodajstvi.Presentation/wwwroot/js/custom-validation.js` (kontrola v prohlížeči)
- `src/Zpravodajstvi.Presentation/Views/Shared/_ValidationScriptsPartial.cshtml` (načtení skriptů)
- použití: `CreateCommentDto.Content`, `UpdateComment.Content`

`NoProfanityAttribute` je vlastní validační atribut. Na serveru ho MVC spustí při model bindingu a chybu zapíše do `ModelState`. Zároveň implementuje `IClientModelValidator`, takže do HTML vstupního pole přidá atributy pro klientskou validaci:

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

Metodu zaregistruje `$.validator.addMethod("noprofanity", ...)` a adaptér `$.validator.unobtrusive.adapters.add("noprofanity", ["words"], ...)` ji spojí s atributy `data-val-noprofanity-*`. Formulář nového komentáře se tak zkontroluje už v prohlížeči. Formulář úpravy komentáře zkontroluje server a chybu zobrazí po odeslání.

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

Atribut mají všechny akce, které přijímají POST:

| Controller | Akce |
| :--- | :--- |
| `AccountController` | `Register`, `Login`, `Logout` |
| `CommentsController` | `Add`, `Edit`, `Delete` |
| `Admin/ArticleController` | `Create`, `Edit`, `DeleteConfirmed` |
| `Admin/CategoryController` | `Create`, `DeleteConfirmed` |
| `Admin/UserController` | `Edit`, `EditRole`, `Delete` |

Všechny POST formuláře ve views používají tag helper (`asp-action`, `asp-controller`, `asp-area`), takže token obsahují, včetně formulářů s nahráváním souborů (`enctype="multipart/form-data"`). Aplikace neposílá žádné AJAX požadavky. Kdyby je v budoucnu posílala, musela by token přidat do hlavičky požadavku.

## 11. Vyhledávání s normalizací českých znaků

**Soubory:**
- `src/Zpravodajstvi.Domain/Search/SearchNormalizer.cs` (převod textu)
- `src/Zpravodajstvi.Domain/Entities/Article.cs`, `Tag.cs` (normalizované sloupce)
- `src/Zpravodajstvi.Infrastructure/Data/ApplicationDbContext.cs` (plnění sloupců při uložení)
- `src/Zpravodajstvi.Infrastructure/Repositories/ArticleRepository.cs` (dotaz)
- `src/Zpravodajstvi.Infrastructure/Migrations/20261012203628_AddSearchFields.cs` (naplnění existujících záznamů)
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

Sloupce plní `ApplicationDbContext` před každým uložením. Funguje to pro vytvoření i úpravu článku a pro ukázková data, aniž by na to musel myslet kód služeb:

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

### Naplnění existujících záznamů

Migrace `AddSearchFields` po přidání sloupců naplní hodnoty pro články a štítky, které už v databázi jsou. Protože SQLite `lower()` české znaky neumí, migrace nejdřív nahradí písmena s diakritikou a teprve potom zavolá `lower()`:

```csharp
// Česká a slovenská písmena s diakritikou a jejich náhrada (malé písmeno bez diakritiky)
private const string DiacriticChars = "áäčďéěíĺľňóôŕřšťúůýžÁÄČĎÉĚÍĹĽŇÓÔŔŘŠŤÚŮÝŽ";
private const string PlainChars = "aacdeeillnoorrstuuyzaacdeeillnoorrstuuyz";

private static string NormalizeSql(string column)
{
    var expression = $"\"{column}\"";
    for (var i = 0; i < DiacriticChars.Length; i++)
    {
        expression = $"replace({expression}, '{DiacriticChars[i]}', '{PlainChars[i]}')";
    }

    return $"lower({expression})";
}
```

Pro český a slovenský text dává tento SQL výraz stejný výsledek jako `SearchNormalizer.Normalize`. Jiné znaky s diakritikou (např. německé „ü“) migrace nepřevede. Takový článek se znormalizuje úplně při své příští úpravě.

## 12. Mazání rubriky s články

**Soubory:**
- `src/Zpravodajstvi.Infrastructure/Data/ApplicationDbContext.cs` (vazba `Restrict`)
- `src/Zpravodajstvi.Presentation/Areas/Admin/Controllers/CategoryController.cs` (`DeleteConfirmed`)
- `src/Zpravodajstvi.Presentation/Areas/Admin/Views/Category/Delete.cshtml`

Každý článek musí patřit do rubriky (`Article.CategoryId` není nullable). Vazba `Category` → `Article` má proto `DeleteBehavior.Restrict` a databáze smazání rubriky s články odmítne (SQLite Error 19: FOREIGN KEY constraint failed). Aby se uživatel místo chyby 500 dozvěděl, co dělat, controller počet článků zkontroluje předem:

```csharp
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var category = await _categoryService.GetCategoryByIdAsync(id);
    if (category != null && category.ArticlesCount > 0)
    {
        ModelState.AddModelError(string.Empty, GetCategoryNotEmptyMessage(category.ArticlesCount));
        return View(category);
    }

    await _categoryService.DeleteCategoryAsync(id);
    return RedirectToAction(nameof(Index));
}
```

Hláška respektuje české skloňování: „obsahuje 1 článek … Nejprve ho přesuňte nebo smažte.“, „obsahuje 3 články …“, „obsahuje 5 článků … Nejprve je přesuňte nebo smažte.“ Stránka mazání ji zobrazí ve shrnutí validace:

```cshtml
<div asp-validation-summary="ModelOnly" class="alert alert-warning mt-3" role="alert"></div>
```

Články lze přesunout do jiné rubriky v jejich úpravě (`/Admin/Article/Edit/{id}`). Prázdnou rubriku controller smaže a přesměruje na seznam rubrik.

## 13. Testy

**Složka:** `tests/Zpravodajstvi.Tests` (xUnit + Moq). Spuštění: `dotnet test Zpravodajstvi.sln`.

| Soubor | Co ověřuje |
| :--- | :--- |
| `ValidationTests/NoProfanityAttributeTests.cs` | vulgární slova se zamítnou, běžná slova „sekundu“ a „operace“ projdou, vlastní seznam slov |
| `ServicesTests/CommentServiceTests.cs` | přidání komentáře s autorem z účtu, řazení, vlastník podle `UserId`, uživatel se stejným jménem nemá přístup, komentář bez `UserId` smaže jen Admin |
| `ControllersTests/CommentsControllerTests.cs` | autor se bere z přihlášeného účtu, nevalidní úprava vrátí detail článku, Edit a Delete předávají službě `Id` uživatele, cizí komentář vrátí `Forbid` |
| `ControllersTests/CategoryControllerTests.cs` | rubrika s články se nesmaže a vrátí stránku s hláškou (1/3/5 článků), prázdná rubrika se smaže |
| `ControllersTests/HomeControllerTests.cs` | výpis a detail článku, neexistující článek vrátí 404 |
| `RepositoriesTests/ArticleRepositoryTests.cs` | vyhledávání `český`/`Český`/`ČESKÝ`/`cesky`/`#Česko` a filtr štítku nad skutečnou SQLite v paměti |

Test repozitáře vytváří databázi metodou `Migrate()`, takže zároveň ověřuje, že se všechny migrace dají aplikovat na prázdnou databázi.

## 14. Opravené chyby

### Chyba 1: Čtenář dostal při mazání vlastního komentáře chybu 403

- **Projev:** Po kliknutí na „Smazat“ u vlastního komentáře se zobrazila stránka „Přístup odepřen“.
- **Příčina:** `CommentsController.Delete` předával službě `User.Identity.Name`, což je e-mail, a služba ho porovnávala se jménem autora komentáře (`AuthorName`, tj. `FullName`). E-mail se se jménem nikdy neshodoval.
- **Oprava:** Vlastník se určuje podle `Id` uživatele: controller čte `User.FindFirstValue(ClaimTypes.NameIdentifier)` a `CommentService` ho porovná s `Comment.UserId` (viz bod 5 a [kapitola 8](#8-přihlášení-role-a-vlastník-komentáře)).
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
- **Oprava:** `CreateCommentDto` už vlastnost `AuthorName` nemá, takže se jméno z formuláře nemá kam navázat. `CommentsController.Add` vždy načte přihlášeného uživatele (`_userManager.GetUserAsync(User)`) a službě předá jeho `Id` a `FullName`. Pole se jménem ve formuláři je jen informativní a nemá atribut `name`.
- **Soubory:** `Application/DTOs/CommentDtos.cs`, `Application/Services/CommentService.cs`, `Presentation/Controllers/CommentsController.cs`, `Presentation/Controllers/HomeController.cs`, `Presentation/Views/Home/Details.cshtml`.

### Chyba 5: Uživatel se stejným jménem mohl upravovat cizí komentáře

- **Projev:** Kdo se zaregistroval se stejným celým jménem jako autor komentáře, mohl jeho komentář upravit i smazat.
- **Příčina:** Vlastník se určoval porovnáním jmen (`AuthorName` vs. `FullName`), a jméno není jedinečné.
- **Oprava:**
  - entita `Comment` má nové pole `UserId` s cizím klíčem na `AspNetUsers` (`OnDelete(SetNull)`) a indexem,
  - při vytvoření komentáře se `UserId` vyplní z přihlášeného účtu, ukázková data ho vyplňují také,
  - `Edit` i `Delete` kontrolují vlastníka podle `UserId` (`CommentService.IsOwner`), Admin smí dál upravovat a mazat všechny komentáře,
  - migrace `AddCommentUserId` doplní `UserId` u existujících komentářů podle shody `AuthorName` s `FullName`, ale jen když je shoda jednoznačná (právě jeden uživatel s tím jménem). Ostatní komentáře zůstanou s `UserId = null` a spravuje je jen administrátor.
- **Soubory:** `Domain/Entities/Comment.cs`, `Infrastructure/Data/ApplicationDbContext.cs`, `Infrastructure/Migrations/20261012203511_AddCommentUserId.cs`, `Infrastructure/Data/DbInitializer.cs`, `Application/Services/CommentService.cs`, `Application/DTOs/CommentDtos.cs` (`CommentDto.UserId`), `Application/Services/ArticleService.cs`, `Presentation/Views/Home/Details.cshtml`.
- **Navíc:** Projekt dříve vytvářel databázi přes `EnsureCreated()` a migrace neměl. Přibyla výchozí migrace `InitialCreate` a `DbInitializer` teď volá `MigrateAsync()`. Existující databázi bez historie migrací při prvním startu označí jako `InitialCreate`, takže se data zachovají (viz [kapitola 5](#5-migrace-a-výchozí-data)).

### Chyba 6: Smazání rubriky s články skončilo chybou 500

- **Projev:** „SQLite Error 19: FOREIGN KEY constraint failed“ po potvrzení smazání rubriky, která obsahovala články.
- **Příčina:** Vazba `Category` → `Article` má `DeleteBehavior.Restrict`, ale `CategoryController.DeleteConfirmed` mazal rubriku bez kontroly článků.
- **Oprava:** `DeleteConfirmed` nejdřív zjistí počet článků. Pokud rubrika nějaké obsahuje, nesmaže ji a vrátí stránku mazání s hláškou „Rubriku nelze smazat, obsahuje N článků. Nejprve je přesuňte nebo smažte.“ (se správným tvarem pro 1, 2–4 a 5+ článků). Chování `Restrict` zůstalo. Na stránce mazání byla opravena i věta, podle které mohly po smazání zůstat články bez rubriky.
- **Soubory:** `Presentation/Areas/Admin/Controllers/CategoryController.cs`, `Presentation/Areas/Admin/Views/Category/Delete.cshtml`.

### Chyba 7: Formuláře článků přijímaly požadavek bez antiforgery tokenu

- **Projev:** Na `/Admin/Article/Create` a `/Admin/Article/Edit` šlo poslat POST bez tokenu (riziko CSRF).
- **Příčina:** Akce `Create` a `Edit` (POST) v `Admin/ArticleController` neměly atribut `[ValidateAntiForgeryToken]`.
- **Oprava:** Obě akce atribut dostaly. Formuláře `Create.cshtml` a `Edit.cshtml` token posílají, protože používají tag helper formuláře. Ostatní POST akce byly zkontrolované a atribut už měly (přehled v [kapitole 10](#10-ochrana-formulářů-antiforgery-tokenem)). Požadavek bez tokenu teď skončí odpovědí 400.
- **Soubory:** `Presentation/Areas/Admin/Controllers/ArticleController.cs`.

### Chyba 8: Hledání „#Česko“ nebo „český“ nic nenašlo

- **Projev:** Hledání slov s českými znaky (a filtr štítku „Česko“) nevracelo žádné články.
- **Příčina:** Dotaz porovnával `Title.ToLower()` a `Name.ToLower()`, což EF Core přeloží na SQLite `lower()`. Ta převádí jen ASCII, takže „Č“ zůstalo velké a neshodovalo se s hledaným „č“. Vyhledávací pole navíc s hashtagy vůbec nepočítalo.
- **Oprava:** Články mají normalizované sloupce `SearchTitle`, `SearchPerex`, `SearchContent` a štítky `SearchName` (malá písmena bez diakritiky, `SearchNormalizer`). Plní se automaticky při každém uložení (`ApplicationDbContext.SaveChanges`) a pro existující záznamy je naplní migrace `AddSearchFields`. Repozitář normalizuje hledaný výraz stejně a výraz začínající `#` hledá jako štítek. `#Česko`, `český`, `Český`, `ČESKÝ` i `cesky` teď fungují stejně.
- **Soubory:** `Domain/Search/SearchNormalizer.cs`, `Domain/Entities/Article.cs`, `Domain/Entities/Tag.cs`, `Infrastructure/Data/ApplicationDbContext.cs`, `Infrastructure/Repositories/ArticleRepository.cs`, `Infrastructure/Migrations/20261012203628_AddSearchFields.cs`, `tests/.../RepositoriesTests/ArticleRepositoryTests.cs`.
