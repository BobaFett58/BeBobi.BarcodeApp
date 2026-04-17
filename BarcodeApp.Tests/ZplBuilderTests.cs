using BarcodeApp.Models;
using BarcodeApp.Services;

namespace BarcodeApp.Tests;

public sealed class ZplBuilderTests
{
    [Fact]
    public void Build_GeneratesOneLabelPerQuantity()
    {
        var rows = new[]
        {
            new ValidProductData
            {
                Ean = "5903949788051",
                Name = "Produkt A",
                Quantity = 3
            }
        };

        var zpl = ZplBuilder.Build(rows, new ZplBuildOptions());

        Assert.Equal(3, CountOccurrences(zpl, "^XA"));
        Assert.Equal(3, CountOccurrences(zpl, "^XZ"));
        Assert.Equal(3, CountOccurrences(zpl, "^FD5903949788051^FS"));
    }

    [Fact]
    public void Build_ExcludesName_WhenOptionDisabled()
    {
        var rows = new[]
        {
            new ValidProductData
            {
                Ean = "5903949788051",
                Name = "Produkt A",
                Quantity = 1
            }
        };

        var zpl = ZplBuilder.Build(rows, new ZplBuildOptions
        {
            IncludeProductName = false
        });

        Assert.DoesNotContain("^A0N,32,32", zpl);
        Assert.Contains("^FD5903949788051^FS", zpl);
    }

    [Fact]
    public void Build_SanitizesAndTruncates_ProductName()
    {
        var rows = new[]
        {
            new ValidProductData
            {
                Ean = "5903949788051",
                Name = "  AB^CD~EFGH  ",
                Quantity = 1
            }
        };

        var zpl = ZplBuilder.Build(rows, new ZplBuildOptions
        {
            MaxProductNameLength = 6
        });

        Assert.Contains("^FDABCDEF^FS", zpl);
    }

    [Fact]
    public void Build_Throws_OnNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => ZplBuilder.Build(null!, new ZplBuildOptions()));
        Assert.Throws<ArgumentNullException>(() => ZplBuilder.Build(Array.Empty<ValidProductData>(), null!));
    }

    [Fact]
    public void Build_EmitsLabelLength_WhenLabelHeightDotsIsPositive()
    {
        var rows = new[]
        {
            new ValidProductData { Ean = "5903949788051", Name = "Test", Quantity = 1 }
        };

        var zpl = ZplBuilder.Build(rows, new ZplBuildOptions { LabelHeightDots = 400 });

        Assert.Contains("^LL400", zpl);
    }

    [Fact]
    public void Build_OmitsLabelLength_WhenLabelHeightDotsIsZero()
    {
        var rows = new[]
        {
            new ValidProductData { Ean = "5903949788051", Name = "Test", Quantity = 1 }
        };

        var zpl = ZplBuilder.Build(rows, new ZplBuildOptions { LabelHeightDots = 0 });

        Assert.DoesNotContain("^LL", zpl);
    }

    [Fact]
    public void Build_RespectesLabelWidthAndBarcodeHeight()
    {
        var rows = new[]
        {
            new ValidProductData { Ean = "5903949788051", Name = "Test", Quantity = 1 }
        };

        var zpl = ZplBuilder.Build(rows, new ZplBuildOptions
        {
            LabelWidthDots = 800,
            BarcodeHeightDots = 150,
            LabelHeightDots = 0
        });

        Assert.Contains("^PW800", zpl);
        Assert.Contains("^BY2,2,150^BEN", zpl);
    }

    private static int CountOccurrences(string content, string token)
    {
        var count = 0;
        var index = 0;

        while ((index = content.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }
}