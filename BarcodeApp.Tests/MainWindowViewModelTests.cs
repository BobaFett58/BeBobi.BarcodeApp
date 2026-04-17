using Qivisoft.BarcodeApp.Tests.TestHelpers;
using Qivisoft.BarcodeApp.Models;
using Qivisoft.BarcodeApp.Services;
using Qivisoft.BarcodeApp.ViewModels;

namespace Qivisoft.BarcodeApp.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void CanExport_DependsOnValidRows()
    {
        var vm = CreateVm();

        Assert.False(vm.CanExport);

        vm.AddRowCommand.Execute(null);
        vm.Rows[0].Ean = "5903949788051";
        vm.Rows[0].Name = "Produkt A";
        vm.Rows[0].QuantityText = "1";

        Assert.True(vm.CanExport);

        vm.ClearRowsCommand.Execute(null);

        Assert.False(vm.CanExport);
    }

    [Fact]
    public void Constructor_LoadsSavedSettings()
    {
        var store = new InMemorySettingsStore
        {
            Current = new AppUserSettings
            {
                IncludeProductName = false,
                SelectedBarcodeType = BarcodeSymbology.UpcA,
                ActivePrinterProfileName = "Magazyn A",
                PrinterProfiles =
                [
                    new PrinterProfileSettings
                    {
                        Name = "Magazyn A",
                        BarcodeType = BarcodeSymbology.UpcA,
                        PrinterDpi = 300,
                        LabelWidthDotsText = "900",
                        BarcodeHeightDotsText = "140",
                        LabelHeightDotsText = "500"
                    }
                ]
            }
        };

        var vm = new MainWindowViewModel(store);

        Assert.False(vm.IncludeProductName);
        Assert.Equal(BarcodeSymbology.UpcA, vm.SelectedBarcodeType);
        Assert.Equal(300, vm.PrinterDpi);
        Assert.Equal("900", vm.LabelWidthDotsText);
        Assert.Equal("140", vm.BarcodeHeightDotsText);
        Assert.Equal("500", vm.LabelHeightDotsText);
    }

    [Fact]
    public void ChangingSettings_SavesToStore()
    {
        var store = new InMemorySettingsStore();
        var vm = new MainWindowViewModel(store);

        vm.PrinterDpi = 300;
        vm.LabelWidthDotsText = "900";
        vm.BarcodeHeightDotsText = "150";
        vm.LabelHeightDotsText = "450";
        vm.IncludeProductName = false;
        vm.SelectedBarcodeType = BarcodeSymbology.Code128;

        Assert.False(store.Current.IncludeProductName);
        Assert.Equal(BarcodeSymbology.Code128, store.Current.SelectedBarcodeType);
        Assert.Equal(300, store.Current.PrinterDpi);
        Assert.Equal("900", store.Current.LabelWidthDotsText);
        Assert.Equal("150", store.Current.BarcodeHeightDotsText);
        Assert.Equal("450", store.Current.LabelHeightDotsText);
    }

    [Fact]
    public void ResetSettingsCommand_RestoresDefaultsAndSaves()
    {
        var store = new InMemorySettingsStore();
        var vm = new MainWindowViewModel(store)
        {
            PrinterDpi = 300,
            LabelWidthDotsText = "900",
            BarcodeHeightDotsText = "150",
            LabelHeightDotsText = "450",
            IncludeProductName = false,
            SelectedBarcodeType = BarcodeSymbology.Code128
        };

        vm.ResetSettingsCommand.Execute(null);

        Assert.True(vm.IncludeProductName);
        Assert.Equal(203, vm.PrinterDpi);
        Assert.Equal("600", vm.LabelWidthDotsText);
        Assert.Equal("110", vm.BarcodeHeightDotsText);
        Assert.Equal("0", vm.LabelHeightDotsText);
        Assert.Equal(BarcodeSymbology.Ean13, vm.SelectedBarcodeType);
        Assert.Equal("Przywrócono domyślne ustawienia profilu 'Domyslny'.", vm.StatusMessage);

        Assert.True(store.Current.IncludeProductName);
        Assert.Equal(BarcodeSymbology.Ean13, store.Current.SelectedBarcodeType);
        Assert.Equal(203, store.Current.PrinterDpi);
        Assert.Equal("600", store.Current.LabelWidthDotsText);
        Assert.Equal("110", store.Current.BarcodeHeightDotsText);
        Assert.Equal("0", store.Current.LabelHeightDotsText);
    }

    [Fact]
    public void AddAndRemoveProfile_UpdatesProfilesAndSelection()
    {
        var store = new InMemorySettingsStore();
        var vm = new MainWindowViewModel(store);

        vm.AddProfileCommand.Execute(null);

        Assert.Equal(2, vm.PrinterProfileNames.Count);
        Assert.Equal("Profil", vm.SelectedPrinterProfileName);

        vm.RemoveProfileCommand.Execute(null);

        Assert.Single(vm.PrinterProfileNames);
        Assert.Equal("Domyslny", vm.SelectedPrinterProfileName);
    }

    [Fact]
    public void Apply300Preset_ChangesFields()
    {
        var vm = CreateVm();

        vm.Apply300DpiPresetCommand.Execute(null);

        Assert.Equal(300, vm.PrinterDpi);
        Assert.Equal("900", vm.LabelWidthDotsText);
        Assert.Equal("165", vm.BarcodeHeightDotsText);
    }

    [Fact]
    public void ExportZplToPath_InvalidConfiguration_ShowsValidationMessage()
    {
        using var temp = new TempPath();
        var output = temp.GetFilePath("invalid.zpl");
        var vm = BuildSingleValidRowVm();
        vm.LabelWidthDotsText = "20";

        vm.ExportZplToPath(output);

        Assert.Equal("Szerokość etykiety (dots) musi być liczbą w zakresie 120-2000.", vm.StatusMessage);
        Assert.False(File.Exists(output));
    }

    [Fact]
    public void AddAndRemoveRow_UpdatesCounters()
    {
        var vm = CreateVm();

        vm.AddRowCommand.Execute(null);
        vm.Rows[0].Ean = "5903949788051";
        vm.Rows[0].Name = "Produkt A";
        vm.Rows[0].QuantityText = "3";

        Assert.Equal(1, vm.ValidRowsCount);
        Assert.Equal(3, vm.TotalLabels);

        vm.RemoveSelectedRowCommand.Execute(null);

        Assert.Empty(vm.Rows);
        Assert.Equal(0, vm.ValidRowsCount);
        Assert.Equal(0, vm.TotalLabels);
    }

    [Fact]
    public void ImportFromPath_NullPath_UpdatesStatus()
    {
        var vm = CreateVm();

        vm.ImportFromPath(null);

        Assert.Equal("Nie wybrano pliku wejściowego.", vm.StatusMessage);
    }

    [Fact]
    public void ExportZplToPath_NoValidRows_ShowsMessage()
    {
        using var temp = new TempPath();
        var output = temp.GetFilePath("out.zpl");
        var vm = CreateVm();

        vm.ExportZplToPath(output);

        Assert.Equal("Brak poprawnych wierszy do eksportu. Najpierw popraw błędy.", vm.StatusMessage);
        Assert.False(File.Exists(output));
    }

    [Fact]
    public void ExportZplToPath_NullPath_ShowsMessage()
    {
        var vm = BuildSingleValidRowVm();

        vm.ExportZplToPath(null);

        Assert.Equal("Nie wybrano pliku wyjściowego.", vm.StatusMessage);
    }

    [Fact]
    public void ExportZplToPath_WithValidRows_WritesZplFile()
    {
        using var temp = new TempPath();
        var output = temp.GetFilePath("out.zpl");
        var vm = CreateVm();

        vm.AddRowCommand.Execute(null);
        vm.Rows[0].Ean = "5903949788051";
        vm.Rows[0].Name = "Produkt A";
        vm.Rows[0].QuantityText = "2";

        vm.ExportZplToPath(output);

        Assert.True(File.Exists(output));
        var content = File.ReadAllText(output);
        Assert.Contains("^FD590394978805^FS", content);
        Assert.Contains("Wyeksportowano ZPL do", vm.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportZplToPath_RespectsIncludeProductNameOption()
    {
        using var temp = new TempPath();
        var output = temp.GetFilePath("out-noname.zpl");
        var vm = BuildSingleValidRowVm();
        vm.IncludeProductName = false;

        vm.ExportZplToPath(output);

        var content = File.ReadAllText(output);
        Assert.DoesNotContain("^A0N,32,32", content);
    }

    [Fact]
    public void ImportFromPath_MissingFile_ShowsFailureMessage()
    {
        var vm = CreateVm();
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv");

        vm.ImportFromPath(missing);

        Assert.StartsWith("Import nie powiódł się:", vm.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ImportFromPath_ValidFile_ImportsRows()
    {
        using var temp = new TempPath();
        var path = temp.GetFilePath("in.csv");
        File.WriteAllText(path,
            "EAN,Nazwa produktu,Ilość\n" +
            "5903949788051,Produkt A,2\n");

        var vm = CreateVm();
        vm.ImportFromPath(path);

        Assert.Single(vm.Rows);
        Assert.Contains("Zaimportowano 1 wierszy", vm.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void RemoveRow_NonMember_DoesNothing()
    {
        var vm = BuildSingleValidRowVm();
        var external = ProductRowViewModel.CreateEmpty();

        vm.RemoveRow(external);

        Assert.Single(vm.Rows);
    }

    [Fact]
    public void RemoveRow_Null_Throws()
    {
        var vm = CreateVm();

        Assert.Throws<ArgumentNullException>(() => vm.RemoveRow(null!));
    }

    [Fact]
    public void RemoveSelectedRowCommand_WhenSelectedRowNull_RemovesLastRow()
    {
        var vm = CreateVm();
        vm.AddRowCommand.Execute(null);
        vm.AddRowCommand.Execute(null);
        vm.SelectedRow = null;

        vm.RemoveSelectedRowCommand.Execute(null);

        Assert.Single(vm.Rows);
    }

    [Fact]
    public void ClearRowsCommand_ClearsAndSetsStatus()
    {
        var vm = BuildSingleValidRowVm();

        vm.ClearRowsCommand.Execute(null);

        Assert.Empty(vm.Rows);
        Assert.Equal("Wiersze wyczyszczone.", vm.StatusMessage);
    }

    private static MainWindowViewModel BuildSingleValidRowVm()
    {
        var vm = CreateVm();
        vm.AddRowCommand.Execute(null);
        vm.Rows[0].Ean = "5903949788051";
        vm.Rows[0].Name = "Produkt A";
        vm.Rows[0].QuantityText = "1";
        return vm;
    }

    private static MainWindowViewModel CreateVm()
    {
        return new MainWindowViewModel(new InMemorySettingsStore());
    }

    private sealed class InMemorySettingsStore : IAppSettingsStore
    {
        public AppUserSettings Current { get; set; } = new();

        public AppUserSettings Load() => new()
        {
            IncludeProductName = Current.IncludeProductName,
            SelectedBarcodeType = Current.SelectedBarcodeType,
            ActivePrinterProfileName = Current.ActivePrinterProfileName,
            PrinterProfiles = Current.PrinterProfiles
                .Select(p => new PrinterProfileSettings
                {
                    Name = p.Name,
                    BarcodeType = p.BarcodeType,
                    PrinterDpi = p.PrinterDpi,
                    LabelWidthDotsText = p.LabelWidthDotsText,
                    BarcodeHeightDotsText = p.BarcodeHeightDotsText,
                    LabelHeightDotsText = p.LabelHeightDotsText
                })
                .ToList(),
            PrinterDpi = Current.PrinterDpi,
            LabelWidthDotsText = Current.LabelWidthDotsText,
            BarcodeHeightDotsText = Current.BarcodeHeightDotsText,
            LabelHeightDotsText = Current.LabelHeightDotsText
        };

        public void Save(AppUserSettings settings)
        {
            Current = new AppUserSettings
            {
                IncludeProductName = settings.IncludeProductName,
                SelectedBarcodeType = settings.SelectedBarcodeType,
                ActivePrinterProfileName = settings.ActivePrinterProfileName,
                PrinterProfiles = settings.PrinterProfiles
                    .Select(p => new PrinterProfileSettings
                    {
                        Name = p.Name,
                        BarcodeType = p.BarcodeType,
                        PrinterDpi = p.PrinterDpi,
                        LabelWidthDotsText = p.LabelWidthDotsText,
                        BarcodeHeightDotsText = p.BarcodeHeightDotsText,
                        LabelHeightDotsText = p.LabelHeightDotsText
                    })
                    .ToList(),
                PrinterDpi = settings.PrinterDpi,
                LabelWidthDotsText = settings.LabelWidthDotsText,
                BarcodeHeightDotsText = settings.BarcodeHeightDotsText,
                LabelHeightDotsText = settings.LabelHeightDotsText
            };
        }
    }
}