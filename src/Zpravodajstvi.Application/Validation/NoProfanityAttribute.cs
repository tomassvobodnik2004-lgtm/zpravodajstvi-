using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Zpravodajstvi.Application.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class NoProfanityAttribute : ValidationAttribute, IClientModelValidator
{
    public static readonly string[] DefaultBannedWords =
    [
       "debil", "idiot", "kreten", "blbec", "hovno", "curak", "kokot", "kurva", "sračka", "sračky",
    "sračku", "sračko", "sračkama", "sračkách", "pice", "pico", "pica", "prdel", "fuck", "shit",
    "bitch", "spam", "picovina", "blbost", "hovadina", "blb", "blbka", "blboun", "blbounka",

    // --- Skloňování: Debil, Idiot, Kretén, Blbec, Blb ---
    "kretén", "kreténi", "kreténů", "kreténům", "kreténem", "kretény", "kreténe", "kreteni", "kretenu", "kretenom", "kretenem", "kreteny",
    "debilové", "debilove", "debilů", "debilu", "debilům", "debilum", "debily", "debile", "debilovi", "debilní", "debilni", "debilita",
    "idioti", "idiotů", "idiotu", "idiotům", "idiotum", "idiote", "idiotovi", "idiotský", "idiotsky",
    "blbce", "blbců", "blbcu", "blbcům", "blbcum", "blbcem", "blbci", "blbky", "blbkám", "blbkam", "blbko", "blbá", "blba", "blbé", "blbe",
    "blbouni", "blbounů", "blbounu", "blbounům", "blbounum", "blbouny", "blbounko", "blboune",

    // --- Skloňování: Piča / Píča / Piče / Pičovina ---
    "píča", "piča", "píče", "píči", "píču", "piču", "píčo", "pičo", "píčou", "pičou",
    "píči", "piči", "píčám", "pičám", "píčách", "pičách", "píčami", "pičami", "pičama",
    "pičovina", "pičoviny", "pičovině", "pičovinu", "pičovino", "pičovinou", "pičovinám", "pičovinách", "pičovinami", "pičovinama",
    "picoviny", "picovine", "picovinu", "picovino", "picovinou", "picovinam", "picovinach", "picovinami", "picovinama",
    "pičusek", "pičus", "picus", "pičusi", "picusi", "pičuse", "picuse",

    // --- Skloňování: Čurák / Čurák / Kokot ---
    "čurák", "curak", "čuráci", "curaci", "čuráků", "curaku", "čurákům", "curakum", "čurákovi", "curakovi", "čuráky", "curaky", "čuráku", "curaku", "čurákem", "curakem",
    "čuráček", "curacek", "čuráčci", "curacci",
    "kokoti", "kokotů", "kokotu", "kokotům", "kokotum", "kokotovi", "kokoty", "kokote", "kokotem", "kokotina", "kokotiny", "kokotině", "kokotinu", "kokotinou",

    // --- Skloňování: Kurva / Kurvit ---
    "kurvy", "kurvě", "kurve", "kurvu", "kurvo", "kurvou", "kurvám", "kurvam", "kurvách", "kurvach", "kurvami", "kurvama",
    "kurvit", "zkurvit", "paskurvit", "rozkurvit", "zkurvený", "zkurveny", "zkurvená", "zkurvena", "zkurvené", "zkurvene", "zkurvení", "zkurveni", "zkurveného", "zkurveneho", "zkurvysyn", "zkurvysynu",

    // --- Skloňování: Hovno / Sračka / Prdel ---
    "hovna", "hovnu", "hovnem", "hovnám", "hovnam", "hovnách", "hovnach", "hovny", "stojí za hovno", "stoji za hovno",
    "sračky", "sracku", "sracko", "srackama", "srackach", "sračce", "sracce", "sraček", "sracek", "sračkám", "srackam", "sračkami", "sračkou", "srackou",
    "sračka", "sracka", "sráč", "srac", "sráči", "sraci", "sráče", "srace", "sráčů", "sracu",
    "prdele", "prdeli", "prdelí", "prdeli", "prdelím", "prdelim", "prdelích", "prdelich", "prdelemi", "prdelama", "doprdele", "do prdele", "naprdel", "vypíčený",

    // --- Skloňování: Hovadina / Blbost ---
    "hovadiny", "hovadině", "hovadine", "hovadinu", "hovadino", "hovadinou", "hovadinám", "hovadinam", "hovadinách", "hovadinach", "hovadinami", "hovadinama",
    "blbosti", "blbostí", "blbostech", "blbostem", "blbostmi", "blbostma",

    // --- Další běžné české urážky a sprostá slova ---
    "pinda", "pindu", "pindy", "pindo", "pindou",
    "mrdka", "mrdky", "mrdce", "mrdku", "mrdko", "mrdkou", "mrdkám", "mrdkam", "mrdkách", "mrdkach", "mrdkami", "mrdkama",
    "mrdat", "mrdám", "mrdam", "mrdáš", "mrdas", "mrdá", "mrda", "mrdáme", "mrdame", "mrdáte", "mrdate", "mrdají", "mrdaji", "zmrd", "zmrde", "zmrdi", "zmrdů", "zmrdům", "zmrdy",
    "kunda", "kundy", "kundě", "kunde", "kundu", "kundo", "kundou", "kundám", "kundam", "kundách", "kundach", "kundami", "kundama", "kundička", "kundicka",
    "šoustat", "soustat", "píchat", "pichat", "jebat", "jebal", "jebnutý", "jebnuty", "zmrdat", "vymrdat", "poyebat",
    "kozy", "koza", "kozičky", "kozicky", "péro", "pero", "péra", "pera", "poverlap",
    "píčus", "picus", "ksicht", "hňup", "hnup", "hňupe", "hnupe", "hňupi", "hnupi", "blbeček", "blbecek", "blbečku", "blbecku", "blbečci", "blbecci",
    "vytřený", "vytreny", "vymletý", "vymlety", "magořina", "magorina", "magor", "magoři", "magori", "magore", "magorů", "magoru",
    "prasopes", "hajzl", "hajzle", "hajzli", "hajzlů", "hajzlu", "hajzlům", "hajzlum", "šmejd", "smejd", "šmejdi", "smejdi", "suka", "blyat", "cyka"
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
