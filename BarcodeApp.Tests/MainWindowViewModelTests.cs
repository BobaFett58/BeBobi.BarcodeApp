using BarcodeApp.Tests.TestHelpers;
using BarcodeApp.ViewModels;

namespace BarcodeApp.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void AddAndRemoveRow_UpdatesCounters()
    {
        var vm = new MainWindowViewModel();

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
        var vm = new MainWindowViewModel();

        vm.ImportFromPath(null);

        Assert.Equal("Nie wybrano pliku wejściowego.", vm.StatusMessage);
    }

    [Fact]
    public void ExportZplToPath_NoValidRows_ShowsMessage()
    {
        using var temp = new TempPath();
        var output = temp.GetFilePath("out.zpl");
        var vm = new MainWindowViewModel();

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
        var vm = new MainWindowViewModel();

        vm.AddRowCommand.Execute(null);
        vm.Rows[0].Ean = "5903949788051";
        vm.Rows[0].Name = "Produkt A";
        vm.Rows[0].QuantityText = "2";

        vm.ExportZplToPath(output);

        Assert.True(File.Exists(output));
        var content = File.ReadAllText(output);
        Assert.Contains("^FD5903949788051^FS", content);
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
        var vm = new MainWindowViewModel();
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

        var vm = new MainWindowViewModel();
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
        var vm = new MainWindowViewModel();

        Assert.Throws<ArgumentNullException>(() => vm.RemoveRow(null!));
    }

    [Fact]
    public void RemoveSelectedRowCommand_WhenSelectedRowNull_RemovesLastRow()
    {
        var vm = new MainWindowViewModel();
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
        var vm = new MainWindowViewModel();
        vm.AddRowCommand.Execute(null);
        vm.Rows[0].Ean = "5903949788051";
        vm.Rows[0].Name = "Produkt A";
        vm.Rows[0].QuantityText = "1";
        return vm;
    }
}