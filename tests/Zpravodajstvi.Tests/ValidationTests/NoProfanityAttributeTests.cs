using System.ComponentModel.DataAnnotations;
using Zpravodajstvi.Application.Validation;
using Xunit;

namespace Zpravodajstvi.Tests.ValidationTests;

public class NoProfanityAttributeTests
{
    private readonly NoProfanityAttribute _attribute = new();

    [Theory]
    [InlineData("Skvělý článek, moc děkuji za přínosné informace!")]
    [InlineData("Zajímavý pohled na moderní technologie a architekturu.")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsValid_CleanText_ReturnsSuccess(string? cleanText)
    {
        // Act
        var result = _attribute.GetValidationResult(cleanText, new ValidationContext(new object()));

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Theory]
    [InlineData("Tohle napsal úplný debil.")]
    [InlineData("To je ale IDIOT!")]
    [InlineData("Autor je kretén.")]
    [InlineData("Je to obyčejný spam")]
    [InlineData("Takové hovno jsem ještě nečetl")]
    public void IsValid_TextWithProfanity_ReturnsValidationError(string vulgarText)
    {
        // Act
        var result = _attribute.GetValidationResult(vulgarText, new ValidationContext(new object()));

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("Text obsahuje nepovolená nebo vulgární slova", result.ErrorMessage);
    }

    [Fact]
    public void HasProfanity_CustomBannedWords_DetectsCustomWord()
    {
        // Arrange
        var customAttribute = new NoProfanityAttribute("zakazane", "nepovoleno");

        // Act & Assert
        Assert.True(customAttribute.HasProfanity("Toto je zakázané slovo v textu."));
        Assert.False(customAttribute.HasProfanity("Toto je v pořádku."));
    }
}
