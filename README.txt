================================================================================
                    ZPRAVODAJSTVÍ - DOKUMENTACE PROJEKTU (.NET 10)
================================================================================

Tento dokument popisuje celkovou strukturu, architekturu, naplnění požadavků a
fungování webové aplikace Zpravodajství vytvořené v rámci vrstvené architektury
ASP.NET Core (net10.0) s Entity Framework Core a ASP.NET Core Identity.

--------------------------------------------------------------------------------
AUTOŘI PROJEKTU:
- Osoba A (Administrace, Články, Databáze & Soubory): Mykhailo Melnyk
- Osoba B (Uživatelé, Komentáře, Validace, Architektura & Testy): Tomáš Svobodník
--------------------------------------------------------------------------------

================================================================================
1. POUŽITÉ TECHNOLOGIE A PROSTŘEDÍ
================================================================================
- Cílový framework: .NET 10 (Target Framework: net10.0, SDK: 10.0.401)
- Soubor global.json: Uzamyká verzi SDK na 10.0.401
- ORM: Entity Framework Core 10.0.12 (SQLite + DbContext)
- Webový framework: ASP.NET Core Web App (MVC)
- Identita & Autorizace: ASP.NET Core Identity s 3 rolemi (Admin, Redaktor, Ctenar)
- Logování: Serilog (Serilog.AspNetCore, Serilog.Sinks.File -> logs/log.txt)
- Testovací framework: xUnit + Moq (19 testů v Zpravodajstvi.Tests)
- Styling: Bootstrap 5 + Vlastní CSS (site.css) s podporou Light / Black motivů

================================================================================
2. ARCHITEKTURA A STRUKTURA VRSTEV
================================================================================
Aplikace je striktně rozdělena do 4 oddělených projektů (vrstev):

1. Zpravodajstvi.Domain (src/Zpravodajstvi.Domain)
   - Typ: Class Library (net10.0)
   - Odpovědnost: Obsahuje pouze čisté C# třídy (Entity). Nemá ŽÁDNÉ vnější závislosti.
   - Entity: Category, Article, Comment, Tag, Image.

2. Zpravodajstvi.Application (src/Zpravodajstvi.Application)
   - Typ: Class Library (net10.0)
   - Odpovědnost: Obsahuje rozhraní (Interfaces), aplikační služby (ArticleService, CommentService), 
     DTOs (ArticleListDto, ArticleDetailDto, CreateArticleDto, CommentDtos) a custom validační atributy.
   - Závislosti: Má referenci pouze na Zpravodajstvi.Domain.

3. Zpravodajstvi.Infrastructure (src/Zpravodajstvi.Infrastructure)
   - Typ: Class Library (net10.0)
   - Odpovědnost: Přístup k databázi přes EF Core, DbContext (ApplicationDbContext),
     Identity entitu ApplicationUser, inicializátor DbInitializer a repozitáře (ArticleRepository, CommentRepository).
   - Závislosti: Má referenci na Zpravodajstvi.Application a Zpravodajstvi.Domain.

4. Zpravodajstvi.Presentation (src/Zpravodajstvi.Presentation)
   - Typ: ASP.NET Core Web App MVC (net10.0)
   - Odpovědnost: Uživatelské rozhraní (Controllers, Views, Admin Area, ViewModels, JS validace).
   - Závislosti: Referenčně napojeno na Zpravodajstvi.Application a DI konfiguraci v Program.cs.

================================================================================
3. KONTROLA PLNĚNÍ POŽADAVKŮ A FUNKCIONALIT
================================================================================

--------------------------------------------------------------------------------
FÁZE 1: ODEVZDÁNÍ V 7. TÝDNU (ZÁKLAD A ARCHITEKTURA)
--------------------------------------------------------------------------------
[OSOBA A: MYKHAILO MELNYK - DATABÁZE A INFRASTRUKTURA]
✔ Založení Solution a 4 vrstev (Domain, App, Infra, Web) - HOTOVO
✔ Vytvoření C# tříd pro doménové entity v Domain vrstvě - HOTOVO
✔ Integrace Entity Framework Core (konfigurace ApplicationDbContext) - HOTOVO
✔ Vytvoření Code-First inicializace a seedování dat (DbInitializer) - HOTOVO

[OSOBA B: TOMÁŠ SVOBODNÍK - ARCHITEKTURA A ABSTRAKCE]
✔ Návrh rozhraní (Interfaces) v Application/Domain vrstvě (IArticleRepository, ICommentRepository, IArticleService, ICommentService) - HOTOVO
✔ Implementace Repozitářů v Infrastructure vrstvě (ArticleRepository, CommentRepository) - HOTOVO
✔ Registrace služeb (Dependency Injection) v Program.cs - HOTOVO

--------------------------------------------------------------------------------
FÁZE 2: SPRÁVA OBSAHU, UŽIVATELÉ, VALIDACE, TÉMATA A TESTY
--------------------------------------------------------------------------------
[OSOBA A: MYKHAILO MELNYK - ADMINISTRACE, ČLÁNKY A SOUBORY]
✔ Vytvoření Area "Admin": Konfigurace routování v Program.cs, vytvoření složek Areas/Admin/(Controllers, Models, Views), napojení na Area("Admin").
✔ Application Services: Naprogramování ArticleService pro kompletní správu článků (načítání, tvorba, vazba na obrázky).
✔ Admin UI (CRUD): Vytvořen přehled článků v Bootstrap tabulce (Index.cshtml) a formulář pro přidání článku (Create.cshtml) s využitím ArticleCreateViewModel.
✔ Bonus - Upload souborů: Implementováno nahrávání náhledových obrázků k článkům přes <input type="file">. Soubory jsou ukládány do wwwroot/uploads a propojeny s entitou Image.
✔ Bonus - Logování: Integrován Serilog (Serilog.AspNetCore + Serilog.Sinks.File), logování chyb, varování a úspěšných operací v ArticleControlleru s výstupem do logs/log.txt a konzole.

[OSOBA B: TOMÁŠ SVOBODNÍK - UŽIVATELÉ, KOMENTÁŘE, VALIDACE, TÉMATA A TESTY]
✔ Identity Framework: Nastavena autentizace a autorizace. Vytvořeny a nasazeny 3 úlohy/role:
  1. Admin (plný přístup vč. správy účtů, e-mailů, hesel a rolí)
  2. Redaktor (správa a tvorba článků a kategorií)
  3. Ctenar (čtení a přidávání komentářů)
  Zabezpečení akcí pomocí [Authorize(Roles = ...)].
✔ Správa uživatelských účtů (Admin): Výpis uživatelů, úprava e-mailu, celého jména, změna role, nastavení nového hesla a mazání účtů.
✔ Validace dat: Nastavena serverová i klientská validace formulářů (Required, StringLength, EmailAddress, Compare, ValidationScriptsPartial).
✔ Vlastní validační atribut: Vytvořen custom atribut [NoProfanity] pro kontrolu vulgarismů v komentářích i jménech (funguje na serveru i na klientovi přes custom-validation.js).
✔ Veřejné UI & Komentáře: Úvodní stránka s výpisem článků, vyhledáváním, filtrací podle kategorií a štítků (#tag) + detail článku. U komentářů je jméno přihlášeného uživatele uzamčené (readonly).
✔ Přepínač motivů (Light / Black): Tlačítko v navigaci umožňuje přepínat mezi světlým a tmavým režimem s ukládáním do localStorage a vyladěným vysokokontrastním zobrazením textu i pozadí diskuze.
✔ Bonus - Unit Testy: Vytvořen projekt Zpravodajstvi.Tests s 19 xUnit + Moq unit testy pokrývajícími NoProfanityAttribute, CommentService, CommentsController a HomeController (všechny testy v pořádku procházejí).

================================================================================
4. JAK PROJEKT SESTAVIT A SPUSTIT
================================================================================
1. Sestavení celého řešení:
   dotnet build Zpravodajstvi.sln

2. Spuštění unit testů:
   dotnet test Zpravodajstvi.sln

3. Spuštění webové aplikace:
   dotnet run --project src/Zpravodajstvi.Presentation/Zpravodajstvi.Presentation.csproj

Webová aplikace bude dostupná na adrese: https://localhost:5001 / http://localhost:5000 (nebo příslušný dev port).

Výchozí přihlašovací údaje (připravené v DbInitializer):
- Admin:    admin@zpravodajstvi.cz    / Heslo123!
- Redaktor: redaktor@zpravodajstvi.cz / Heslo123!
- Čtenář:   ctenar@zpravodajstvi.cz   / Heslo123!

================================================================================
