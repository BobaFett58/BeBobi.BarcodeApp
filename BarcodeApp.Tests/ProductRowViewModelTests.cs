using BarcodeApp.Models;
using BarcodeApp.ViewModels;

namespace BarcodeApp.Tests;

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

        var ok = row.TryBuildValidData(out var _);

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
    public void Validate_RaisesRowChanged()
    {
        var row = ProductRowViewModel.CreateEmpty();
        var raised = 0;
        row.RowChanged += (_, _) => raised++;

        row.Ean = "5903949788051";

        Assert.True(raised > 0);
    }
}
