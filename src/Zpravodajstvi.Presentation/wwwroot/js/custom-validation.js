// Klientská validace pro vlastní atribut [NoProfanity]
(function ($) {
    if (!$ || !$.validator || !$.validator.unobtrusive) {
        return;
    }

    function removeDiacritics(text) {
        if (!text) return "";
        return text.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
    }

    // Hledá jen celá slova: před a za slovem nesmí být písmeno, číslice ani podtržítko.
    // JavaScriptové \b zná jen ASCII, proto hranici slova skládáme z Unicode tříd (stejně jako \b v .NET).
    function containsWholeWord(text, word) {
        var escaped = word.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
        var pattern = new RegExp("(^|[^\\p{L}\\p{Mn}\\p{Nd}\\p{Pc}])" + escaped + "(?=$|[^\\p{L}\\p{Mn}\\p{Nd}\\p{Pc}])", "iu");
        return pattern.test(text);
    }

    $.validator.addMethod("noprofanity", function (value, element, params) {
        if (!value || value.trim() === "") {
            return true;
        }

        var words = (params || "").split(",");
        var normalizedText = removeDiacritics(value);

        for (var i = 0; i < words.length; i++) {
            var word = removeDiacritics(words[i].trim());
            if (!word) continue;

            if (containsWholeWord(normalizedText, word)) {
                return false;
            }
        }

        return true;
    });

    $.validator.unobtrusive.adapters.add("noprofanity", ["words"], function (options) {
        options.rules["noprofanity"] = options.params.words;
        options.messages["noprofanity"] = options.message;
    });
})(window.jQuery);
