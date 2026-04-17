using Avalonia.Styling;
using Qivisoft.BarcodeApp.Theming;

namespace Qivisoft.BarcodeApp.Tests;

public class ThemeConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknown")]
    [InlineData("high-contrast")]
    public void Parse_ReturnsSystem_ForNullEmptyWhitespaceOrUnknown(string? value)
    {
        var result = ThemeConfiguration.Parse(value);

        Assert.Equal(AppThemeMode.System, result);
    }

    [Theory]
    [InlineData("system", AppThemeMode.System)]
    [InlineData("SYSTEM", AppThemeMode.System)]
    [InlineData(" light ", AppThemeMode.Light)]
    [InlineData("DARK", AppThemeMode.Dark)]
    public void Parse_IsCaseInsensitive_AndTrimsInput(string value, AppThemeMode expected)
    {
        var result = ThemeConfiguration.Parse(value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToThemeVariant_MapsAllModes()
    {
        Assert.Equal(ThemeVariant.Default, ThemeConfiguration.ToThemeVariant(AppThemeMode.System));
        Assert.Equal(ThemeVariant.Light, ThemeConfiguration.ToThemeVariant(AppThemeMode.Light));
        Assert.Equal(ThemeVariant.Dark, ThemeConfiguration.ToThemeVariant(AppThemeMode.Dark));
    }

    [Theory]
    [InlineData(null, AppThemeMode.System)]
    [InlineData("", AppThemeMode.System)]
    [InlineData("dark", AppThemeMode.Dark)]
    [InlineData("LIGHT", AppThemeMode.Light)]
    [InlineData("system", AppThemeMode.System)]
    public void ResolveFromEnvironment_UsesParseRules(string? value, AppThemeMode expected)
    {
        var result = ThemeConfiguration.ResolveFromEnvironment(value);

        Assert.Equal(expected, result);
    }
}