using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Zpravodajstvi.Application.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class NoProfanityAttribute : ValidationAttribute, IClientModelValidator
{
    public static readonly string[] DefaultBannedWords =
    [
        "debil", "idiot", "kreten", "blbec", "hovno", "curak", "kokot", "kurva", "sračka", "sračky", "sračku", "sračko", "sračkama", "sračkách", "pice", "pico",
        "pica", "prdel", "fuck", "shit", "bitch", "spam", "picovina", "blbost", "hovadina", "blb", "blbka", "blbec", "blboun", "blbounka",
    ];

    public string[] BannedWords { get; }

    public NoProfanityAttribute() : this(DefaultBannedWords)
    {
    }

    public NoProfanityAttribute(params string[] bannedWords)
    {
        BannedWords = bannedWords.Length > 0 ? bannedWords : DefaultBannedWords;
        ErrorMessage = "Text obsahuje nepovolená nebo vulgární slova.";
    }

    public bool HasProfanity(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalizedInput = RemoveDiacritics(input).ToLowerInvariant();

        foreach (var word in BannedWords)
        {
            var normalizedWord = RemoveDiacritics(word).ToLowerInvariant();
            // Hledá celé slovo nebo výskyt v textu pomocí regex boundary nebo Contains
            var pattern = $@"\b{Regex.Escape(normalizedWord)}\b";
            if (Regex.IsMatch(normalizedInput, pattern, RegexOptions.IgnoreCase) || normalizedInput.Contains(normalizedWord))
            {
                return true;
            }
        }

        return false;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string strValue && HasProfanity(strValue))
        {
            var displayName = validationContext.DisplayName;
            return new ValidationResult(FormatErrorMessage(displayName));
        }

        return ValidationResult.Success;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-noprofanity", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
        MergeAttribute(context.Attributes, "data-val-noprofanity-words", string.Join(",", BannedWords));
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}
