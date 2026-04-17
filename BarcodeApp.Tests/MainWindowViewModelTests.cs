using System.Net;
using System.Net.Sockets;
using System.Text;
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

        Assert.Equal("No input file selected.", vm.StatusMessage);
    }

    [Fact]
    public void ExportZplToPath_NoValidRows_ShowsMessage()
    {
        using var temp = new TempPath();
        var output = temp.GetFilePath("out.zpl");
        var vm = new MainWindowViewModel();

        vm.ExportZplToPath(output);

        Assert.Equal("No valid rows to export. Fix row errors first.", vm.StatusMessage);
        Assert.False(File.Exists(output));
    }

    [Fact]
    public void ExportZplToPath_NullPath_ShowsMessage()
    {
        var vm = BuildSingleValidRowVm();

        vm.ExportZplToPath(null);

        Assert.Equal("No output file selected.", vm.StatusMessage);
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
        Assert.Contains("ZPL exported to", vm.StatusMessage, StringComparison.Ordinal);
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

        Assert.StartsWith("Import failed:", vm.StatusMessage, StringComparison.Ordinal);
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
        Assert.Contains("Imported 1 rows", vm.StatusMessage, StringComparison.Ordinal);
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
        Assert.Equal("Rows cleared.", vm.StatusMessage);
    }

    [Fact]
    public async Task SendToPrinterCommand_NoValidRows_ShowsMessage()
    {
        var vm = new MainWindowViewModel();

        await vm.SendToPrinterCommand.ExecuteAsync(null);

        Assert.Equal("No valid rows to print. Fix row errors first.", vm.StatusMessage);
    }

    [Fact]
    public async Task SendToPrinterCommand_InvalidPort_ShowsMessage()
    {
        var vm = BuildSingleValidRowVm();
        vm.PrinterHost = "localhost";
        vm.PrinterPort = "abc";

        await vm.SendToPrinterCommand.ExecuteAsync(null);

        Assert.Equal("Printer port must be in range 1-65535.", vm.StatusMessage);
    }

    [Fact]
    public async Task SendToPrinterCommand_MissingHost_ShowsMessage()
    {
        var vm = BuildSingleValidRowVm();
        vm.PrinterHost = "";

        await vm.SendToPrinterCommand.ExecuteAsync(null);

        Assert.Equal("Provide printer host (IP or DNS name).", vm.StatusMessage);
    }

    [Fact]
    public async Task SendToPrinterCommand_UnreachableHost_ShowsFailureMessage()
    {
        var vm = BuildSingleValidRowVm();
        vm.PrinterHost = "127.0.0.1";

        // Find a currently unused local port and close it before sending.
        int port;
        using (var listener = new TcpListener(IPAddress.Loopback, 0))
        {
            listener.Start();
            port = ((IPEndPoint)listener.LocalEndpoint).Port;
        }

        vm.PrinterPort = port.ToString();
        await vm.SendToPrinterCommand.ExecuteAsync(null);

        Assert.StartsWith("Print send failed:", vm.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendToPrinterCommand_SendsBytesToListener()
    {
        var vm = BuildSingleValidRowVm();
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        vm.PrinterHost = "127.0.0.1";
        vm.PrinterPort = port.ToString();

        var acceptTask = listener.AcceptTcpClientAsync();
        await vm.SendToPrinterCommand.ExecuteAsync(null);

        using var client = await acceptTask;
        using var stream = client.GetStream();
        using var ms = new MemoryStream();
        var buffer = new byte[1024];
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
        ms.Write(buffer, 0, read);

        var payload = Encoding.ASCII.GetString(ms.ToArray());
        Assert.Contains("^FD5903949788051^FS", payload);
        Assert.Contains("Sent 1 labels", vm.StatusMessage, StringComparison.Ordinal);
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
