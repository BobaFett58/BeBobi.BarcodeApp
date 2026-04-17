using Qivisoft.BarcodeApp.Services;

namespace Qivisoft.BarcodeApp.Tests;

public sealed class Ean13ValidatorTests
{
    [Theory]
    [InlineData("5903949788051")]
    [InlineData("4006381333931")]
    public void IsValid_ReturnsTrue_ForValidEan13(string ean)
    {
        var result = Ean13Validator.IsValid(ean);

        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456789012")]
    [InlineData("12345678901234")]
    [InlineData("5903949788059")]
    [InlineData("59039497880A1")]
    public void IsValid_ReturnsFalse_ForInvalidInputs(string? ean)
    {
        var result = Ean13Validator.IsValid(ean);

        Assert.False(result);
    }

    [Fact]
    public void CalculateChecksum_ReturnsExpectedDigit()
    {
        var checksum = Ean13Validator.CalculateChecksum("590394978805");

        Assert.Equal(1, checksum);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abcdefghijkl")]
    [InlineData("12345678901A")]
    public void CalculateChecksum_Throws_ForInvalidInput(string value)
    {
        Assert.Throws<ArgumentException>(() => Ean13Validator.CalculateChecksum(value));
    }
}