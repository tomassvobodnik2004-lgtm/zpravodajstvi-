namespace Zpravodajstvi.Domain.Constants;

// Názvy rolí aplikace na jednom místě – používají je [Authorize], views i seeding.
// Redaktor odpovídá roli "Manager" (spravuje obsah), Ctenar roli "User" (běžný uživatel).
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Redaktor = "Redaktor";
    public const string Ctenar = "Ctenar";

    // Správa obsahu (články, kategorie, štítky, komentáře) – Admin a Redaktor
    public const string ContentManagers = Admin + "," + Redaktor;

    // Všechny role, které smí přidávat komentáře
    public const string All = Admin + "," + Redaktor + "," + Ctenar;

    // Role, na kterých závisí kód aplikace – nelze je přejmenovat ani smazat
    public static readonly string[] SystemRoles = [Admin, Redaktor, Ctenar];
}
