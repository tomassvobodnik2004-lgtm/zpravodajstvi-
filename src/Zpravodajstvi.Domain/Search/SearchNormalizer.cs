using System.Globalization;
using System.Text;

namespace Zpravodajstvi.Domain.Search;

// Převod textu do podoby pro vyhledávání: malá písmena a bez diakritiky ("ČESKÝ" -> "cesky").
// Normalizují se jím uložená pole (Article.Search*, Tag.SearchName) i hledaný výraz,
// takže vyhledávání nezávisí na velikosti písmen ani na diakritice.
public static class SearchNormalizer
{
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
}
