# 📰 Zpravodajství

Webová aplikace zpravodajského portálu postavená na **ASP.NET Core MVC (.NET 10)**. Návštěvníci čtou články rozdělené do rubrik, filtrují je podle štítků (#hashtagů) a vyhledávají v nich. Přihlášení čtenáři píší komentáře a redakce spravuje obsah v administraci. Data ukládá **Entity Framework Core** do databáze **SQLite**, uživatele a role spravuje **ASP.NET Core Identity**.

Podrobný popis toho, jak aplikace funguje uvnitř, je v souboru [TUTORIAL.md](TUTORIAL.md).

## Autoři

- **Osoba A** (administrace, články, databáze a soubory): Mykhailo Melnyk
- **Osoba B** (uživatelé, komentáře, validace, architektura a testy): Tomáš Svobodník

## Přehled funkcí

**Veřejná část**
- výpis článků od nejnovějšího s obrázkem, perexem, štítky a počtem komentářů,
- filtrování podle rubrik (kategorií) a štítků,
- vyhledávání v titulku, perexu i textu článku bez ohledu na velikost písmen a diakritiku (`český`, `Český`, `ČESKÝ` i `cesky` najdou totéž),
- hledání podle hashtagu přímo ve vyhledávacím poli (`#Česko`),
- detail článku s galerií obrázků a diskuzí,
- přepínač světlého a tmavého motivu (volba se ukládá do `localStorage`).

**Účty a komentáře**
- registrace (nový účet dostane roli Čtenář), přihlášení a odhlášení,
- přidávání komentářů pro přihlášené uživatele; jméno autora vždy doplní server podle účtu,
- úprava a smazání vlastních komentářů (vlastník se pozná podle Id uživatele), administrátor spravuje všechny komentáře,
- filtr vulgárních slov `[NoProfanity]` na serveru i v prohlížeči, kontroluje celá slova.

**Administrace (`/Admin`)**
- články: vytvoření, úprava a smazání, nahrání obrázku do `wwwroot/uploads`,
- rubriky: vytvoření a smazání (smazat lze jen rubriku bez článků, jinak se zobrazí srozumitelná hláška),
- uživatelé (jen administrátor): změna jména, e-mailu, role a hesla, smazání účtu.

**Bezpečnost a provoz**
- autorizace podle rolí (`[Authorize(Roles = ...)]`),
- všechny formuláře odesílané metodou POST chrání antiforgery token,
- databázové schéma spravují migrace EF Core, aplikují se automaticky při startu,
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
| Testy | xUnit, Moq, SQLite v paměti pro test vyhledávání |

## Požadavky

- **.NET SDK 10** – `global.json` vyžaduje verzi `10.0.201` nebo novější z řady 10.0 (`rollForward: latestFeature`). Instalaci ověříte příkazem `dotnet --list-sdks`.
- Volitelně nástroj **dotnet-ef** (jen pro ruční práci s migracemi, aplikace ho ke spuštění nepotřebuje).
- Volitelně důvěryhodný vývojový certifikát pro HTTPS: `dotnet dev-certs https --trust`.

## Instalace a spuštění

Všechny příkazy se spouštějí z kořenové složky repozitáře. Ta obsahuje dva soubory řešení (`Zpravodajstvi.sln` s testy a `Zpravodajstvi.slnx` bez testů), proto je v příkazech řešení vždy uvedeno.

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

- Databáze je soubor `src/Zpravodajstvi.Presentation/zpravodajstvi.db`, cestu určuje connection string `DefaultConnection` v `src/Zpravodajstvi.Presentation/appsettings.json`.
- **Migrace se aplikují automaticky při startu aplikace.** Metoda `DbInitializer.SeedDataAsync` zavolá `MigrateAsync()` a potom vytvoří role, výchozí účty a ukázkové články (jen do prázdné databáze).
- Databázi ze starší verze aplikace, která vznikla přes `EnsureCreated()` a nemá tabulku `__EFMigrationsHistory`, aplikace při prvním startu sama označí jako odpovídající migraci `InitialCreate` a aplikuje jen novější migrace. **Existující data zůstanou zachována.**
- Migrace jsou ve složce `src/Zpravodajstvi.Infrastructure/Migrations`:

  | Migrace | Co dělá |
  | :--- | :--- |
  | `InitialCreate` | výchozí schéma (články, rubriky, štítky, obrázky, komentáře, tabulky Identity) |
  | `AddCommentUserId` | sloupec `Comments.UserId` s cizím klíčem na uživatele, doplnění autorů u existujících komentářů |
  | `AddSearchFields` | normalizované sloupce pro vyhledávání (`Articles.Search*`, `Tags.SearchName`) a jejich naplnění |

- **Ruční práce s migracemi** (volitelné):

  ```bash
  dotnet tool install --global dotnet-ef --version 10.0.12

  # aplikace migrací na databázi
  dotnet ef database update --project src/Zpravodajstvi.Infrastructure --startup-project src/Zpravodajstvi.Presentation

  # vytvoření nové migrace po změně modelu
  dotnet ef migrations add NazevMigrace --project src/Zpravodajstvi.Infrastructure --startup-project src/Zpravodajstvi.Presentation --output-dir Migrations
  ```

  Databázi ze starší verze (bez historie migrací) nejdřív jednou připravte spuštěním aplikace. `dotnet ef database update` by na ní jinak zkusil znovu vytvořit existující tabulky a skončil chybou.
- **Čistá databáze:** zastavte aplikaci, smažte `zpravodajstvi.db` (případně i `zpravodajstvi.db-shm` a `zpravodajstvi.db-wal`) a aplikaci spusťte znovu. Databáze se vytvoří z migrací a naplní ukázkovými daty.

## Výchozí účty a role

Při startu se vytvoří tři role a tyto účty (pokud ještě neexistují):

| Role | E-mail | Heslo | Oprávnění |
| :--- | :--- | :--- | :--- |
| `Admin` | `admin@zpravodajstvi.cz` | `Heslo123!` | vše: články, rubriky, správa účtů, úprava a mazání libovolného komentáře |
| `Redaktor` | `redaktor@zpravodajstvi.cz` | `Heslo123!` | administrace článků a rubrik, komentáře |
| `Ctenar` | `ctenar@zpravodajstvi.cz` | `Heslo123!` | čtení a komentování, úprava a mazání vlastních komentářů |

Každý nově zaregistrovaný uživatel dostane roli `Ctenar`. Roli může později změnit administrátor ve správě účtů.

## Struktura projektu

```text
Zpravodajstvi.sln                     řešení se všemi projekty včetně testů
global.json                           verze .NET SDK
src/
  Zpravodajstvi.Domain/               entity a rozhraní repozitářů, bez dalších závislostí
    Entities/                         Article, Category, Comment, Tag, Image
    Interfaces/                       IArticleRepository, ICommentRepository
    Search/                           SearchNormalizer (malá písmena bez diakritiky)
  Zpravodajstvi.Application/          aplikační logika
    DTOs/                             objekty pro přenos dat mezi vrstvami a z formulářů
    Interfaces/, Services/            ArticleService, CategoryService, CommentService
    Validation/                       NoProfanityAttribute (filtr vulgárních slov)
  Zpravodajstvi.Infrastructure/       přístup k datům
    Data/                             ApplicationDbContext, DbInitializer (migrace a výchozí data)
    Identity/                         ApplicationUser
    Migrations/                       migrace EF Core
    Repositories/                     ArticleRepository, CommentRepository
  Zpravodajstvi.Presentation/         webová aplikace ASP.NET Core MVC
    Controllers/                      Home, Comments, Account
    Areas/Admin/                      administrace: Article, Category, User
    Views/                            Razor pohledy a sdílený layout
    wwwroot/                          CSS, JavaScript (custom-validation.js), knihovny, nahrané obrázky
    Program.cs                        konfigurace služeb a HTTP pipeline
    appsettings.json                  connection string a nastavení logování
tests/
  Zpravodajstvi.Tests/                unit testy (xUnit + Moq) a test vyhledávání nad SQLite v paměti
```

Závislosti mezi vrstvami: `Presentation → Application, Infrastructure`, `Infrastructure → Application, Domain`, `Application → Domain`. Doména nezávisí na ničem.
