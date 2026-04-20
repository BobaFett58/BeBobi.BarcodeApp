using Qivisoft.BarcodeApp.Models;
using Qivisoft.BarcodeApp.ViewModels;

namespace Qivisoft.BarcodeApp.Tests;

public sealed class ProductRowViewModelTests
{
    [Fact]
    public void CreateEmpty_HasInvalidState()
    {
        var row = ProductRowViewModel.CreateEmpty();

        Assert.False(row.IsValid);
        Assert.Contains("EAN", row.ValidationMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void FromInput_MapsValues_AndValidatesOnSetters()
    {
        var row = ProductRowViewModel.FromInput(new ProductInputRow
        {
            Ean = "5903949788051",
            Name = "Produkt A",
            QuantityText = "5",
            SourceRowNumber = 2
        });

        Assert.Equal("5903949788051", row.Ean);
        Assert.Equal("Produkt A", row.Name);
        Assert.Equal("5", row.QuantityText);
        Assert.True(row.IsValid);
        Assert.Equal("OK", row.ValidationMessage);
    }

    [Fact]
    public void TryBuildValidData_ReturnsFalse_ForInvalidRow()
    {
        var row = ProductRowViewModel.CreateEmpty();

        var ok = row.TryBuildValidData(out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryBuildValidData_ReturnsData_ForValidRow()
    {
        var row = ProductRowViewModel.CreateEmpty();
        row.Ean = "5903949788051";
        row.Name = "Produkt A";
        row.QuantityText = "7";

        var ok = row.TryBuildValidData(out var data);

        Assert.True(ok);
        Assert.Equal("5903949788051", data.Ean);
        Assert.Equal("Produkt A", data.Name);
        Assert.Equal(7, data.Quantity);
    }

    [Fact]
    public void TryBuildValidData_ComposesDescription_FromSkuNamePrice()
    {
        var row = ProductRowViewModel.CreateEmpty();
        row.Ean = "5903949788051";
        row.Sku = "HEYEHE";
        row.Name = "Kuk+";
        row.Price = "59,00";
        row.QuantityText = "1";

        var ok = row.TryBuildValidData(out var data);

        Assert.True(ok);
        Assert.Equal("Kuk+ HEYEHE 59,00", data.Name);
    }

    [Fact]
    public void TryBuildValidData_ComposesDescription_SkipsEmptyParts()
    {
        var row = ProductRowViewModel.CreateEmpty();
        row.Ean = "5903949788051";
        row.Sku = "";
        row.Name = "Kuk+";
        row.Price = "";
        row.QuantityText = "1";

        var ok = row.TryBuildValidData(out var data);

        Assert.True(ok);
        Assert.Equal("Kuk+", data.Name);
    }

    [Fact]
    public void Validate_RaisesRowChanged()
    {
        var row = ProductRowViewModel.CreateEmpty();
        var raised = 0;
        row.RowChanged += (_, _) => raised++;

        row.Ean = "5903949788051";

        Assert.True(raised > 0);
    }

    [Fact]
    public void Validate_Ean8_IsValidFor8Digits()
    {
        var row = ProductRowViewModel.CreateEmpty();
        row.BarcodeType = BarcodeSymbology.Ean8;
        row.Ean = "55123457";
        row.Name = "Produkt";
        row.QuantityText = "1";

        Assert.True(row.IsValid);
        Assert.Equal("OK", row.ValidationMessage);
    }

    [Fact]
    public void Validate_UpcA_IsInvalidForShortValue()
    {
        var row = ProductRowViewModel.CreateEmpty();
        row.BarcodeType = BarcodeSymbology.UpcA;
        row.Ean = "12345";
        row.Name = "Produkt";
        row.QuantityText = "1";

        Assert.False(row.IsValid);
        Assert.Contains("UPC-A", row.ValidationMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_Code128_AllowsAlnumText()
    {
        var row = ProductRowViewModel.CreateEmpty();
        row.BarcodeType = BarcodeSymbology.Code128;
        row.Ean = "ABC-123-XYZ";
        row.Name = "Produkt";
        row.QuantityText = "2";

        Assert.True(row.IsValid);
    }

    [Fact]
    public void PreviewDescription_SplitsIntoTwoLines_ForLongDescription()
    {
        var row = ProductRowViewModel.CreateEmpty();
        row.Sku = "HEYEHE";
        row.Name = "Obroza dla psa Hau are you sun";
        row.Price = "59,00";

        Assert.False(string.IsNullOrWhiteSpace(row.PreviewDescriptionLine1));
        Assert.False(string.IsNullOrWhiteSpace(row.PreviewDescriptionLine2));
        Assert.Contains("Obroza", row.PreviewDescriptionLine1, StringComparison.Ordinal);
        Assert.Contains("HEYEHE", row.PreviewDescriptionLine2, StringComparison.Ordinal);
        Assert.Contains("59,00", row.PreviewDescriptionLine2, StringComparison.Ordinal);
    }

    [Fact]
    public void PreviewDescription_UsesSingleLine_ForShortDescription()
    {
        var row = ProductRowViewModel.CreateEmpty();
        row.Name = "Kuk+";

        Assert.Equal("Kuk+", row.PreviewDescriptionLine1);
        Assert.Equal(string.Empty, row.PreviewDescriptionLine2);
    }

    [Fact]
    public void PreviewDescription_DoesNotUseSecondLine_WhenShortWithExtras()
    {
        var row = ProductRowViewModel.CreateEmpty();
        row.Name = "Kuk+";
        row.Sku = "HEYEHE";
        row.Price = "59,00";

        Assert.Equal("Kuk+ HEYEHE 59,00", row.PreviewDescriptionLine1);
        Assert.Equal(string.Empty, row.PreviewDescriptionLine2);
    }

    [Fact]
    public void PreviewBarcode_UpdatesWhenEanChanges()
    {
        var row = ProductRowViewModel.CreateEmpty();
        row.Ean = "5906358794453";

        Assert.Contains("5 9 0 6 3 5", row.PreviewBarcodeText, StringComparison.Ordinal);
        Assert.Contains("|", row.PreviewBarcodeBars, StringComparison.Ordinal);
    }
}