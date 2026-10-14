# 📰 Zpravodajství

Webová aplikace zpravodajského portálu postavená na **ASP.NET Core MVC (.NET 10)**. Návštěvníci čtou články rozdělené do rubrik, filtrují je podle štítků (#hashtagů) a vyhledávají v nich. Přihlášení čtenáři píší komentáře a redakce spravuje obsah v administraci. Data ukládá **Entity Framework Core** do databáze **SQLite**, uživatele a role spravuje **ASP.NET Core Identity**.

Podrobný popis toho, jak aplikace funguje uvnitř, je v souboru [TUTORIAL.md](TUTORIAL.md).

## Autoři

- **Osoba A** (administrace, články, databáze a soubory): Mykhailo Melnyk
- **Osoba B** (uživatelé, komentáře, validace, architektura a testy): Tomáš Svobodník

## Přehled funkcí

**Veřejná část**
- výpis článků od nejnovějšího s obrázkem, perexem, autorem, štítky a počtem komentářů,
- filtrování podle rubrik (kategorií) a štítků,
- vyhledávání v titulku, perexu i textu článku bez ohledu na velikost písmen a diakritiku (`český`, `Český`, `ČESKÝ` i `cesky` najdou totéž),
- hledání podle hashtagu přímo ve vyhledávacím poli (`#Česko`),
- detail článku s autorem, galerií obrázků a diskuzí,
- přepínač světlého a tmavého motivu (volba se ukládá do `localStorage`).

**Účty a komentáře**
- registrace (nový účet dostane roli Čtenář), přihlášení a odhlášení,
- profil přihlášeného uživatele (`/Account/Profile`) s jeho články a komentáři,
- změna vlastního hesla (`/Account/ChangePassword`) – vyžaduje současné heslo, nové heslo se zadává dvakrát,
- přidávání komentářů pro přihlášené uživatele; autora (Id i jméno) vždy určí server podle účtu,
- úprava a smazání vlastních komentářů (vlastník se pozná podle Id uživatele); administrátor a redaktor moderují všechny komentáře,
- filtr vulgárních slov `[NoProfanity]` na serveru i v prohlížeči (i ve formuláři úpravy komentáře pod článkem), kontroluje celá slova.

**Administrace (`/Admin`)**
- články: vytvoření, úprava a smazání, výběr štítků, nahrání obrázku (nový obrázek nahradí starý); autorem je přihlášený uživatel,
- kategorie: vytvoření, přejmenování a smazání (smazat lze jen kategorii bez článků, jinak se zobrazí srozumitelná hláška),
- štítky: vytvoření, přejmenování a smazání,
- komentáře: přehled všech komentářů, úprava a smazání,
- uživatelé (jen administrátor): vytvoření účtu, změna jména, e-mailu a role, smazání účtu; heslo uživatele administrátor měnit nemůže, každý si ho mění sám na stránce Změna hesla,
- role (jen administrátor): přehled s počtem uživatelů, vytvoření, přejmenování a smazání vlastních rolí; systémové role jsou chráněné.

**Bezpečnost a provoz**
- autorizace podle rolí (`[Authorize(Roles = ...)]`, názvy rolí jsou v `AppRoles`),
- všechny formuláře odesílané metodou POST chrání antiforgery token,
- nahrávat lze jen obrázky (`.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`) do 5 MB,
- databázové schéma spravují migrace EF Core, aplikují se automaticky při startu; v kódu není žádný ručně psaný SQL,
- logování přes Serilog do konzole a do souborů `logs/log<datum>.txt`.

## Použité technologie

| Oblast | Technologie |
| :--- | :--- |
| Platforma | .NET 10 (`net10.0`), verze SDK je určená v `global.json` |
| Web | ASP.NET Core MVC, Razor views, Areas, Tag Helpers |
| Databáze | Entity Framework Core 10.0.12, SQLite, migrace EF Core |
| Uživatelé a role | ASP.NET Core Identity (`Admin`, `Redaktor`, `Ctenar`) |
| Logování | Serilog (`Serilog.AspNetCore`, `Serilog.Sinks.File`) |
| Klientská část | Bootstrap 5.3.3, jQuery 3.7.1, jQuery Validation + Unobtrusive |
| Testy | xUnit, Moq, SQLite v paměti (vyhledávání, služby pro účty a role nad skutečným Identity) |

## Požadavky

- **.NET SDK 10** – `global.json` vyžaduje verzi `10.0.201` nebo novější z řady 10.0 (`rollForward: latestFeature`). Instalaci ověříte příkazem `dotnet --list-sdks`.
- Volitelně nástroj **dotnet-ef** (jen pro ruční práci s migracemi, aplikace ho ke spuštění nepotřebuje).
- Volitelně důvěryhodný vývojový certifikát pro HTTPS: `dotnet dev-certs https --trust`.

## Instalace a spuštění

Všechny příkazy se spouštějí z kořenové složky repozitáře. Ta obsahuje dva soubory řešení se stejnými projekty včetně testů (`Zpravodajstvi.sln` a `Zpravodajstvi.slnx`), proto je v příkazech řešení vždy uvedeno.

1. **Sestavení** (obnoví i balíčky NuGet):

   ```bash
   dotnet build Zpravodajstvi.sln
   ```

2. **Spuštění webu:**

   ```bash
   dotnet run --project src/Zpravodajstvi.Presentation
   ```

   Web poběží na `http://localhost:5082`. Varianta s HTTPS (`https://localhost:7099`):

   ```bash
   dotnet run --project src/Zpravodajstvi.Presentation --launch-profile https
   ```

3. **Testy:**

   ```bash
   dotnet test Zpravodajstvi.sln
   ```

### Databáze a migrace

- Databáze je soubor `src/Zpravodajstvi.Presentation/zpravodajstvi.db`, cestu určuje connection string `DefaultConnection` v `src/Zpravodajstvi.Presentation/appsettings.json`. Databáze v repozitáři je vytvořená čistě z migrací a obsahuje jen ukázková data ze seedingu.
- Složka `backup/` v kořeni řešení je v `.gitignore` a slouží pro lokální zálohy databáze (do gitu se neukládá).
- **Migrace se aplikují automaticky při startu aplikace.** Metoda `DbInitializer.SeedDataAsync` zavolá `MigrateAsync()` a potom vytvoří role, výchozí účty a ukázkové články (jen do prázdné databáze).
- Migrace jsou ve složce `src/Zpravodajstvi.Infrastructure/Migrations`:

  | Migrace | Co dělá |
  | :--- | :--- |
  | `InitialCreate` | výchozí schéma (články, rubriky, štítky, obrázky, komentáře, tabulky Identity) |
  | `AddCommentUserId` | sloupec `Comments.UserId` s cizím klíčem na uživatele |
  | `AddSearchFields` | normalizované sloupce pro vyhledávání (`Articles.Search*`, `Tags.SearchName`) |
  | `AddArticleAuthor` | sloupec `Articles.AuthorId` s cizím klíčem na autora článku |

- **Ruční práce s migracemi** (volitelné):

  ```bash
  dotnet tool install --global dotnet-ef --version 10.0.12

  # aplikace migrací na databázi
  dotnet ef database update --project src/Zpravodajstvi.Infrastructure --startup-project src/Zpravodajstvi.Presentation

  # vytvoření nové migrace po změně modelu
  dotnet ef migrations add NazevMigrace --project src/Zpravodajstvi.Infrastructure --startup-project src/Zpravodajstvi.Presentation --output-dir Migrations

  # kontrola, že migrace odpovídají modelu (ModelSnapshot je aktuální)
  dotnet ef migrations has-pending-model-changes --project src/Zpravodajstvi.Infrastructure --startup-project src/Zpravodajstvi.Presentation
  ```

  `dotnet ef database update` vytvoří jen schéma. Role, výchozí účty a ukázková data doplní `DbInitializer` při prvním spuštění aplikace.

- **Čistá databáze:** zastavte aplikaci, smažte `zpravodajstvi.db` (případně i `zpravodajstvi.db-shm` a `zpravodajstvi.db-wal`) a aplikaci spusťte znovu. Databáze se vytvoří z migrací a naplní ukázkovými daty. Stejně postupujte, pokud máte starou kopii databáze bez tabulky `__EFMigrationsHistory` (vznikla ještě přes `EnsureCreated()`) a aplikace při startu hlásí, že tabulka už existuje.

## Výchozí účty a role

Při startu se vytvoří tři role a tyto účty (pokud ještě neexistují):

| Role | Odpovídá | E-mail | Heslo | Oprávnění |
| :--- | :--- | :--- | :--- | :--- |
| `Admin` | Admin | `admin@zpravodajstvi.cz` | `Heslo123!` | vše: obsah, moderování komentářů, správa účtů a rolí |
| `Redaktor` | Manager | `redaktor@zpravodajstvi.cz` | `Heslo123!` | správa obsahu: články, kategorie, štítky, moderování komentářů; nespravuje účty ani role |
| `Ctenar` | User | `ctenar@zpravodajstvi.cz` | `Heslo123!` | čtení a komentování, úprava a mazání vlastních komentářů, vlastní profil a změna vlastního hesla |

Každý nově zaregistrovaný uživatel dostane roli `Ctenar`. Roli může později změnit administrátor ve správě účtů. Vlastní heslo si může změnit každý přihlášený uživatel na stránce Změna hesla (odkaz je v profilu).

Oprávnění controllerů v administraci:

| Controller (Area `Admin`) | `[Authorize(Roles = ...)]` |
| :--- | :--- |
| `UserController`, `RoleController` | `Admin` |
| `ArticleController`, `CategoryController`, `TagController`, `CommentController` | `Admin,Redaktor` |

## Struktura projektu

```text
Zpravodajstvi.sln, .slnx              řešení se všemi projekty včetně testů
global.json                           verze .NET SDK
src/
  Zpravodajstvi.Domain/               entity a rozhraní repozitářů, bez referencí na jiné vrstvy
    Constants/                        AppRoles (názvy rolí)
    Entities/                         Article, Category, Comment, Tag, Image, ApplicationUser
    Interfaces/                       IRepository<T> a specifické repozitáře (Article, Comment, Category, Tag)
    Search/                           SearchNormalizer (malá písmena bez diakritiky)
  Zpravodajstvi.Application/          aplikační logika
    DTOs/                             objekty pro přenos dat mezi vrstvami a z formulářů
    Interfaces/                       rozhraní služeb (články, kategorie, štítky, komentáře, účty, uživatelé, role, soubory)
    Services/                         ArticleService, CategoryService, TagService, CommentService
    Validation/                       vlastní validační atributy NoProfanity a ImageFile
    DependencyInjection.cs            AddApplication() – registrace služeb
  Zpravodajstvi.Infrastructure/       přístup k datům a k Identity
    Data/                             ApplicationDbContext, DbInitializer (migrace a výchozí data)
    Identity/                         AccountService, UserService, RoleService (UserManager, SignInManager, RoleManager)
    Files/                            LocalFileStorageService (ukládání obrázků)
    Migrations/                       migrace EF Core
    Repositories/                     Repository<T>, ArticleRepository, CommentRepository, CategoryRepository, TagRepository
    DependencyInjection.cs            AddInfrastructure() – DbContext, Identity, repozitáře, služby
  Zpravodajstvi.Presentation/         webová aplikace ASP.NET Core MVC
    Controllers/                      Home, Comments, Account
    Areas/Admin/                      administrace: Article, Category, Tag, Comment, User, Role
    Views/                            Razor pohledy a sdílený layout
    wwwroot/                          CSS, JavaScript (custom-validation.js), knihovny, nahrané obrázky
    Program.cs                        konfigurace služeb a HTTP pipeline
    appsettings.json                  connection string a nastavení logování
tests/
  Zpravodajstvi.Tests/                unit testy (xUnit + Moq) a testy nad SQLite v paměti
```

Závislosti mezi vrstvami: `Presentation → Application, Infrastructure`, `Infrastructure → Application, Domain`, `Application → Domain`. Doména neodkazuje na žádný jiný projekt, používá jen balíček `Microsoft.Extensions.Identity.Stores` kvůli třídě `IdentityUser`. Presentation používá Infrastructure jen v `Program.cs` (registrace do DI a inicializace databáze), controllery i views pracují pouze s rozhraními a DTO z vrstvy Application.
