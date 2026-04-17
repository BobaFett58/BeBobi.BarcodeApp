using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net.Sockets;
using System.Text;
using BarcodeApp.Models;
using BarcodeApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace BarcodeApp.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly ImportService _importService = new();

    private ProductRowViewModel? _selectedRow;
    private string _statusMessage = "Import a CSV/XLS file to begin.";
    private string _printerHost = string.Empty;
    private string _printerPort = "9100";
    private bool _includeProductName = true;

    public MainWindowViewModel()
    {
        Rows.CollectionChanged += OnRowsCollectionChanged;

        AddRowCommand = new RelayCommand(AddRow);
        RemoveSelectedRowCommand = new RelayCommand(RemoveSelectedRow, () => Rows.Count > 0);
        ClearRowsCommand = new RelayCommand(ClearRows, () => Rows.Count > 0);
        SendToPrinterCommand = new AsyncRelayCommand(SendToPrinterAsync);
    }

    public ObservableCollection<ProductRowViewModel> Rows { get; } = [];

    public ProductRowViewModel? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (SetProperty(ref _selectedRow, value))
            {
                RemoveSelectedRowCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string PrinterHost
    {
        get => _printerHost;
        set => SetProperty(ref _printerHost, value);
    }

    public string PrinterPort
    {
        get => _printerPort;
        set => SetProperty(ref _printerPort, value);
    }

    public bool IncludeProductName
    {
        get => _includeProductName;
        set => SetProperty(ref _includeProductName, value);
    }

    public int ValidRowsCount => Rows.Count(row => row.IsValid);

    public int InvalidRowsCount => Rows.Count - ValidRowsCount;

    public int TotalLabels => Rows
        .Where(row => row.IsValid)
        .Select(row => int.TryParse(row.QuantityText, out var qty) ? qty : 0)
        .Sum();

    public IRelayCommand AddRowCommand { get; }

    public IRelayCommand RemoveSelectedRowCommand { get; }

    public IRelayCommand ClearRowsCommand { get; }

    public IAsyncRelayCommand SendToPrinterCommand { get; }

    public void ImportFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "No input file selected.";
            return;
        }

        try
        {
            var result = _importService.Import(path);
            Rows.Clear();

            foreach (var row in result.Rows)
            {
                var vmRow = ProductRowViewModel.FromInput(row);
                AttachRowEvents(vmRow);
                Rows.Add(vmRow);
            }

            if (Rows.Count == 0)
            {
                StatusMessage = "File loaded, but no data rows were recognized.";
            }
            else
            {
                var warningPart = result.Warnings.Count > 0
                    ? $" Warnings: {string.Join("; ", result.Warnings)}"
                    : string.Empty;
                StatusMessage = $"Imported {Rows.Count} rows from {Path.GetFileName(path)}.{warningPart}";
            }

            NotifyStatsChanged();
            ClearRowsCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }
    }

    public void ExportZplToPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "No output file selected.";
            return;
        }

        var validData = CollectValidData();
        if (validData.Count == 0)
        {
            StatusMessage = "No valid rows to export. Fix row errors first.";
            return;
        }

        var zpl = ZplBuilder.Build(validData, new ZplBuildOptions
        {
            IncludeProductName = IncludeProductName
        });

        File.WriteAllText(path, zpl, Encoding.ASCII);
        StatusMessage = $"ZPL exported to {Path.GetFileName(path)} ({validData.Count} products, {TotalLabels} labels).";
    }

    public void RemoveRow(ProductRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!Rows.Contains(row))
        {
            return;
        }

        DetachRowEvents(row);
        Rows.Remove(row);
        if (ReferenceEquals(SelectedRow, row))
        {
            SelectedRow = Rows.LastOrDefault();
        }

        ClearRowsCommand.NotifyCanExecuteChanged();
        NotifyStatsChanged();
    }

    private async Task SendToPrinterAsync()
    {
        var validData = CollectValidData();
        if (validData.Count == 0)
        {
            StatusMessage = "No valid rows to print. Fix row errors first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(PrinterHost))
        {
            StatusMessage = "Provide printer host (IP or DNS name).";
            return;
        }

        if (!int.TryParse(PrinterPort, out var port) || port is < 1 or > 65535)
        {
            StatusMessage = "Printer port must be in range 1-65535.";
            return;
        }

        var zpl = ZplBuilder.Build(validData, new ZplBuildOptions
        {
            IncludeProductName = IncludeProductName
        });

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(PrinterHost.Trim(), port);

            await using var stream = client.GetStream();
            var bytes = Encoding.ASCII.GetBytes(zpl);
            await stream.WriteAsync(bytes);
            await stream.FlushAsync();

            StatusMessage = $"Sent {TotalLabels} labels to {PrinterHost}:{port}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Print send failed: {ex.Message}";
        }
    }

    private void AddRow()
    {
        var row = ProductRowViewModel.CreateEmpty();
        AttachRowEvents(row);
        Rows.Add(row);
        SelectedRow = row;
        ClearRowsCommand.NotifyCanExecuteChanged();
        NotifyStatsChanged();
    }

    private void RemoveSelectedRow()
    {
        var rowToRemove = SelectedRow ?? Rows.LastOrDefault();
        if (rowToRemove is null)
        {
            return;
        }

        DetachRowEvents(rowToRemove);
        Rows.Remove(rowToRemove);
        SelectedRow = Rows.LastOrDefault();
        ClearRowsCommand.NotifyCanExecuteChanged();
        NotifyStatsChanged();
    }

    private void ClearRows()
    {
        foreach (var row in Rows)
        {
            DetachRowEvents(row);
        }

        Rows.Clear();
        SelectedRow = null;
        StatusMessage = "Rows cleared.";
        ClearRowsCommand.NotifyCanExecuteChanged();
        NotifyStatsChanged();
    }

    private List<ValidProductData> CollectValidData()
    {
        var validData = new List<ValidProductData>();
        foreach (var row in Rows)
        {
            row.Validate();
            if (row.TryBuildValidData(out var data))
            {
                validData.Add(data);
            }
        }

        NotifyStatsChanged();
        return validData;
    }

    private void OnRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<ProductRowViewModel>())
            {
                AttachRowEvents(item);
            }
        }

        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<ProductRowViewModel>())
            {
                DetachRowEvents(item);
            }
        }

        NotifyStatsChanged();
    }

    private void AttachRowEvents(ProductRowViewModel row)
    {
        row.RowChanged -= OnRowChanged;
        row.RowChanged += OnRowChanged;
    }

    private void DetachRowEvents(ProductRowViewModel row)
    {
        row.RowChanged -= OnRowChanged;
    }

    private void OnRowChanged(object? sender, EventArgs e)
    {
        NotifyStatsChanged();
    }

    private void NotifyStatsChanged()
    {
        OnPropertyChanged(nameof(ValidRowsCount));
        OnPropertyChanged(nameof(InvalidRowsCount));
        OnPropertyChanged(nameof(TotalLabels));
        RemoveSelectedRowCommand.NotifyCanExecuteChanged();
        ClearRowsCommand.NotifyCanExecuteChanged();
    }
}
