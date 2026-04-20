using Qivisoft.BarcodeApp.Models;
using Qivisoft.BarcodeApp.ViewModels;

namespace Qivisoft.BarcodeApp.Tests;

public sealed class StickerPreviewViewModelTests
{
    [Fact]
    public void Ctor_HidesDescription_WhenIncludeProductNameIsFalse()
    {
        var row = ProductRowViewModel.CreateEmpty();
        row.Sku = "HEYEHE";
        row.Name = "Kuk+";
        row.Price = "59,00";

        var vm = new StickerPreviewViewModel(row, false, BarcodeSymbology.Ean13);

        Assert.Equal(string.Empty, vm.DescriptionLine1);
        Assert.Equal(string.Empty, vm.DescriptionLine2);
    }

    [Fact]
    public void Ctor_UsesRowPreviewValues_WhenIncludeProductNameIsTrue()
    {
        var row = ProductRowViewModel.CreateEmpty();
        row.Ean = "5906358794453";
        row.Sku = "HEYEHE";
        row.Name = "Obroza dla psa";
        row.Price = "59,00";

        var vm = new StickerPreviewViewModel(row, true, BarcodeSymbology.Ean13);

        Assert.False(string.IsNullOrWhiteSpace(vm.DescriptionLine1));
        Assert.Contains("5 9 0", vm.BarcodeText, StringComparison.Ordinal);
        Assert.Contains("|", vm.BarcodeBars, StringComparison.Ordinal);
        Assert.Equal("EAN-13", vm.BarcodeCaption);
    }
}
