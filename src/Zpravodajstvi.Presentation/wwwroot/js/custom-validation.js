// Klientská validace pro vlastní atribut [NoProfanity]
(function ($) {
    if (!$ || !$.validator || !$.validator.unobtrusive) {
        return;
    }

    function removeDiacritics(text) {
        if (!text) return "";
        return text.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
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

            var pattern = new RegExp("\\b" + word + "\\b", "i");
            if (pattern.test(normalizedText) || normalizedText.indexOf(word) !== -1) {
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
