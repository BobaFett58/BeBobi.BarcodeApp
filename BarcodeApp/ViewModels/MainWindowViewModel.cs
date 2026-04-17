using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using BarcodeApp.Models;
using BarcodeApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace BarcodeApp.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly ImportService _importService = new();

    public MainWindowViewModel()
    {
        Rows.CollectionChanged += OnRowsCollectionChanged;

        AddRowCommand = new RelayCommand(AddRow);
        RemoveSelectedRowCommand = new RelayCommand(RemoveSelectedRow, () => Rows.Count > 0);
        ClearRowsCommand = new RelayCommand(ClearRows, () => Rows.Count > 0);
    }

    public ObservableCollection<ProductRowViewModel> Rows { get; } = [];

    public ProductRowViewModel? SelectedRow
    {
        get;
        set
        {
            if (SetProperty(ref field, value)) RemoveSelectedRowCommand.NotifyCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get;
        private set => SetProperty(ref field, value);
    } = "Zaimportuj plik CSV/XLS, aby rozpocząć.";

    public bool? IncludeProductName
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    // ── Label size settings ──────────────────────────────────────────────────

    public int PrinterDpi
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(LabelWidthMm));
                OnPropertyChanged(nameof(LabelHeightMm));
                OnPropertyChanged(nameof(DpiHint));
            }
        }
    } = 203;

    public string LabelWidthDotsText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(LabelWidthMm));
        }
    } = "600";

    public string BarcodeHeightDotsText
    {
        get;
        set => SetProperty(ref field, value);
    } = "110";

    public string LabelHeightDotsText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(LabelHeightMm));
        }
    } = "0";

    public string LabelWidthMm =>
        int.TryParse(LabelWidthDotsText, out var w) && PrinterDpi > 0
            ? $"{w * 25.4 / PrinterDpi:F1} mm"
            : "—";

    public string LabelHeightMm =>
        int.TryParse(LabelHeightDotsText, out var h) && h > 0 && PrinterDpi > 0
            ? $"{h * 25.4 / PrinterDpi:F1} mm"
            : "auto";

    public string DpiHint =>
        PrinterDpi switch
        {
            203 => "203 dpi — 8 dots/mm (standard Zebra)",
            300 => "300 dpi — 12 dots/mm (wysoka jakość)",
            _ => $"{PrinterDpi} dpi"
        };

    public int ValidRowsCount => Rows.Count(row => row.IsValid);

    public int InvalidRowsCount => Rows.Count - ValidRowsCount;

    public int TotalLabels => Rows
        .Where(row => row.IsValid)
        .Select(row => int.TryParse(row.QuantityText, out var qty) ? qty : 0)
        .Sum();

    public IRelayCommand AddRowCommand { get; }

    public IRelayCommand RemoveSelectedRowCommand { get; }

    public IRelayCommand ClearRowsCommand { get; }

    public void ImportFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "Nie wybrano pliku wejściowego.";
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
                StatusMessage = "Plik wczytany, ale nie rozpoznano żadnych wierszy danych.";
            }
            else
            {
                var warningPart = result.Warnings.Count > 0
                    ? $" Ostrzeżenia: {string.Join("; ", result.Warnings)}"
                    : string.Empty;
                StatusMessage = $"Zaimportowano {Rows.Count} wierszy z pliku {Path.GetFileName(path)}.{warningPart}";
            }

            NotifyStatsChanged();
            ClearRowsCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import nie powiódł się: {ex.Message}";
        }
    }

    public void ExportZplToPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "Nie wybrano pliku wyjściowego.";
            return;
        }

        var validData = CollectValidData();
        if (validData.Count == 0)
        {
            StatusMessage = "Brak poprawnych wierszy do eksportu. Najpierw popraw błędy.";
            return;
        }

        var zpl = ZplBuilder.Build(validData, new ZplBuildOptions
        {
            IncludeProductName = IncludeProductName ?? true,
            LabelWidthDots = int.TryParse(LabelWidthDotsText, out var w1) && w1 > 0 ? w1 : 600,
            BarcodeHeightDots = int.TryParse(BarcodeHeightDotsText, out var bh1) && bh1 > 0 ? bh1 : 110,
            LabelHeightDots = int.TryParse(LabelHeightDotsText, out var lh1) && lh1 > 0 ? lh1 : 0
        });

        File.WriteAllText(path, zpl, Encoding.ASCII);
        StatusMessage = $"Wyeksportowano ZPL do {Path.GetFileName(path)} ({validData.Count} produktów, {TotalLabels} etykiet).";
    }

    public void RemoveRow(ProductRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!Rows.Contains(row)) return;

        DetachRowEvents(row);
        Rows.Remove(row);
        if (ReferenceEquals(SelectedRow, row)) SelectedRow = Rows.LastOrDefault();

        ClearRowsCommand.NotifyCanExecuteChanged();
        NotifyStatsChanged();
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
        if (rowToRemove is null) return;

        DetachRowEvents(rowToRemove);
        Rows.Remove(rowToRemove);
        SelectedRow = Rows.LastOrDefault();
        ClearRowsCommand.NotifyCanExecuteChanged();
        NotifyStatsChanged();
    }

    private void ClearRows()
    {
        foreach (var row in Rows) DetachRowEvents(row);

        Rows.Clear();
        SelectedRow = null;
        StatusMessage = "Wiersze wyczyszczone.";
        ClearRowsCommand.NotifyCanExecuteChanged();
        NotifyStatsChanged();
    }

    private List<ValidProductData> CollectValidData()
    {
        var validData = new List<ValidProductData>();
        foreach (var row in Rows)
        {
            row.Validate();
            if (row.TryBuildValidData(out var data)) validData.Add(data);
        }

        NotifyStatsChanged();
        return validData;
    }

    private void OnRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (var item in e.NewItems.OfType<ProductRowViewModel>())
                AttachRowEvents(item);

        if (e.OldItems is not null)
            foreach (var item in e.OldItems.OfType<ProductRowViewModel>())
                DetachRowEvents(item);

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