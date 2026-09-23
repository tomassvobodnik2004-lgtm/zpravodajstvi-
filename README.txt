================================================================================
                    ZPRAVODAJSTVÍ - DOKUMENTACE PROJEKTU (.NET 10)
================================================================================

Tento dokument popisuje celkovou strukturu, architekturu a fungování aplikace 
Zpravodajství vytvořené v rámci vrstvené architektury a EF Core pro odevzdání v 7. týdnu.

--------------------------------------------------------------------------------
1. POUŽITÉ TECHNOLOGIE A PŘÍPRAVA (Krok 1)
--------------------------------------------------------------------------------
- Cílový framework: .NET 10 (Target Framework: net10.0, SDK: 10.0.401)
- Soubor global.json: Uzamyká verzi SDK na 10.0.401
- ORM: Entity Framework Core 10.0.12
- Webový framework: ASP.NET Core Web App (MVC)

--------------------------------------------------------------------------------
2. ARCHITEKTURA A STRUKTURA VRSTEV (Krok 2 & Krok 3)
--------------------------------------------------------------------------------
Aplikace je striktně rozdělena do 4 oddělených projektů (vrstev):

1. Zpravodajstvi.Domain (src/Zpravodajstvi.Domain)
   - Typ: Class Library (net10.0)
   - Odpovědnost: Obsahuje pouze čisté C# třídy (Entity). Nemá ŽÁDNÉ vnější závislosti.

2. Zpravodajstvi.Application (src/Zpravodajstvi.Application)
   - Typ: Class Library (net10.0)
   - Odpovědnost: Obsahuje rozhraní (Interfaces), aplikační služby, DTOs a business logiku.
   - Závislosti: Má referenci pouze na Zpravodajstvi.Domain.

3. Zpravodajstvi.Infrastructure (src/Zpravodajstvi.Infrastructure)
   - Typ: Class Library (net10.0)
   - Odpovědnost: Obsahuje přístup k databázi, Entity Framework Core, DbContext a repozitáře.
   - Závislosti: Má referenci na Zpravodajstvi.Application a Zpravodajstvi.Domain.

4. Zpravodajstvi.Presentation (src/Zpravodajstvi.Presentation)
   - Typ: ASP.NET Core Web App MVC (net10.0)
   - Odpovědnost: Uživatelské rozhraní, Controllers, Views, konfigurace aplikace.
   - Závislosti: Má referenci na Zpravodajstvi.Application (a na Zpravodajstvi.Infrastructure 
     výhradně v Program.cs pro registraci závislostí a DbContextu v Dependency Injection).

--------------------------------------------------------------------------------
3. DOMÉNOVÉ ENTITY (Krok 4)
--------------------------------------------------------------------------------
Entity jsou umístěny ve složce `Zpravodajstvi.Domain/Entities`:

- Category (Kategorie článků):
  - Id (int)
  - Name (string)
  - Articles (ICollection<Article>) -> Relace 1:N s články

- Article (Článek):
  - Id (int)
  - Title (string)
  - Perex (string)
  - Content (string)
  - CreatedAt (DateTime)
  - UpdatedAt (DateTime?)
  - CategoryId (int) + Category (Category) -> Cizí klíč a navigace na kategorii
  - Comments (ICollection<Comment>) -> Relace 1:N s komentáři
  - Tags (ICollection<Tag>) -> Relace M:N se štítky
  - Images (ICollection<Image>) -> Relace 1:N s obrázky

- Comment (Komentář k článku):
  - Id (int)
  - AuthorName (string)
  - Content (string)
  - CreatedAt (DateTime)
  - ArticleId (int) + Article (Article) -> Cizí klíč a navigace na článek

- Tag (Štítek / Tag):
  - Id (int)
  - Name (string)
  - Articles (ICollection<Article>) -> Relace M:N s články

- Image (Obrázek k článku):
  - Id (int)
  - Url (string)
  - Caption (string)
  - ArticleId (int) + Article (Article) -> Cizí klíč a navigace na článek

--------------------------------------------------------------------------------
4. ENTITY FRAMEWORK CORE A DBCONTEXT (Krok 5 & Krok 6)
--------------------------------------------------------------------------------
- DbContext třída: `ApplicationDbContext` (v namespace `Zpravodajstvi.Infrastructure.Data`)
- Obsahuje DbSet vlastnosti pro všech 5 entit:
  - DbSet<Article> Articles
  - DbSet<Category> Categories
  - DbSet<Comment> Comments
  - DbSet<Tag> Tags
  - DbSet<Image> Images
- V metodě `OnModelCreating` jsou nakonfigurovány cizí klíče, relace a pravidla kaskádového mazání.

Nainstalované NuGet balíčky:
- Zpravodajstvi.Infrastructure:
  - Microsoft.EntityFrameworkCore (10.0.12)
  - Microsoft.EntityFrameworkCore.SqlServer (10.0.12)
  - Microsoft.EntityFrameworkCore.Sqlite (10.0.12)
- Zpravodajstvi.Presentation:
  - Microsoft.EntityFrameworkCore.Design (10.0.12)

--------------------------------------------------------------------------------
5. JAK PROJEKT SESTAVIT A SPUSTIT
--------------------------------------------------------------------------------
Sestavení celého řešení:
  dotnet build Zpravodajstvi.sln

Spuštění webové prezentace:
  dotnet run --project src/Zpravodajstvi.Presentation/Zpravodajstvi.Presentation.csproj

================================================================================
