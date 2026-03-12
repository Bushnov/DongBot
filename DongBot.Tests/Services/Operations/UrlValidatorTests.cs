using DongBot;

namespace DongBot.Tests;

public class UrlValidatorTests
{
    [Fact]
    public void IsValidUrl_ReturnsTrue_ForHttpUrl()
    {
        Assert.True(UrlValidator.IsValidUrl("https://giphy.com/test.gif"));
    }

    [Fact]
    public void IsValidUrl_ReturnsFalse_ForInvalidUrl()
    {
        Assert.False(UrlValidator.IsValidUrl("not-a-url"));
    }

    [Fact]
    public void ValidateGifUrl_ReturnsWarning_ForUnknownDomainInNonStrictMode()
    {
        UrlValidationResult result = UrlValidator.ValidateGifUrl("https://example.com/test.gif");

        Assert.True(result.IsValid);
        Assert.True(result.WarningOnly);
        Assert.Contains("recognized GIF hosting service", result.ErrorMessage);
    }

    [Fact]
    public void ValidateGifUrl_ReturnsError_ForUnknownDomainInStrictMode()
    {
        UrlValidationResult result = UrlValidator.ValidateGifUrl("https://example.com/test.gif", strictMode: true);

        Assert.False(result.IsValid);
        Assert.False(result.WarningOnly);
        Assert.Contains("recognized GIF hosting service", result.ErrorMessage);
    }
}
