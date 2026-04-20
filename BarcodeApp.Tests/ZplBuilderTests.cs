using Qivisoft.BarcodeApp.Models;
using Qivisoft.BarcodeApp.Services;

namespace Qivisoft.BarcodeApp.Tests;

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
        Assert.Equal(3, CountOccurrences(zpl, "^FD590394978805^FS"));
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

        Assert.DoesNotContain("^A0N,28,28", zpl);
        Assert.Contains("^FD590394978805^FS", zpl);
    }

    [Fact]
    public void Build_SanitizesWithoutTruncating_ProductName()
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

        var zpl = ZplBuilder.Build(rows, new ZplBuildOptions());

        Assert.Contains("^FDABCDEFGH\\&^FS", zpl);
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

    [Fact]
    public void Build_ForEan13_Uses12DigitPayloadForBE()
    {
        var rows = new[]
        {
            new ValidProductData { Ean = "5012345678900", Name = "Test", Quantity = 1 }
        };

        var zpl = ZplBuilder.Build(rows, new ZplBuildOptions { IncludeProductName = false });

        Assert.Contains("^FD501234567890^FS", zpl);
        Assert.DoesNotContain("^FD5012345678900^FS", zpl);
    }

    [Fact]
    public void Build_ForEan8_UsesB8And7DigitPayload()
    {
        var rows = new[]
        {
            new ValidProductData { Ean = "55123457", Name = "Test", Quantity = 1 }
        };

        var zpl = ZplBuilder.Build(rows, new ZplBuildOptions
        {
            IncludeProductName = false,
            BarcodeType = BarcodeSymbology.Ean8
        });

        Assert.Contains("^B8N", zpl);
        Assert.Contains("^FD5512345^FS", zpl);
    }

    [Fact]
    public void Build_ForUpcA_UsesBUAnd11DigitPayload()
    {
        var rows = new[]
        {
            new ValidProductData { Ean = "036000291452", Name = "Test", Quantity = 1 }
        };

        var zpl = ZplBuilder.Build(rows, new ZplBuildOptions
        {
            IncludeProductName = false,
            BarcodeType = BarcodeSymbology.UpcA
        });

        Assert.Contains("^BUN", zpl);
        Assert.Contains("^FD03600029145^FS", zpl);
    }

    [Fact]
    public void Build_ForCode128_UsesBCAndFullPayload()
    {
        var rows = new[]
        {
            new ValidProductData { Ean = "ABC12345XYZ", Name = "Test", Quantity = 1 }
        };

        var zpl = ZplBuilder.Build(rows, new ZplBuildOptions
        {
            IncludeProductName = false,
            BarcodeType = BarcodeSymbology.Code128
        });

        Assert.Contains("^BCN", zpl);
        Assert.Contains("^FDABC12345XYZ^FS", zpl);
    }

    [Fact]
    public void Build_IncludesName_UsesCenteredTwoLineFieldBlock()
    {
        var rows = new[]
        {
            new ValidProductData { Ean = "5903949788051", Name = "Produkt testowy", Quantity = 1 }
        };

        var zpl = ZplBuilder.Build(rows, new ZplBuildOptions { LabelWidthDots = 600 });

        // ^FB600,2,2,C — width=600, maxLines=2, lineSpacing=2, center-justified
        Assert.Contains("^FB600,2,2,C^A0N,28,28^FDProdukt testowy\\&^FS", zpl);
    }

    [Fact]
    public void Build_CenteredBarcodeX_IsApproximatelyHalf_ForEan13()
    {
        var rows = new[]
        {
            new ValidProductData { Ean = "5903949788051", Name = "Test", Quantity = 1 }
        };

        var zpl = ZplBuilder.Build(rows, new ZplBuildOptions
        {
            LabelWidthDots = 600,
            IncludeProductName = false
        });

        // For EAN-13 at ^BY2, barcodeWidth≈228, so x=(600-228)/2=186
        Assert.Contains("^FO186,", zpl);
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