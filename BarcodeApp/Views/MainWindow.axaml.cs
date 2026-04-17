using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BarcodeApp.ViewModels;

namespace BarcodeApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private async void ImportFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (StorageProvider is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select CSV/XLS source file",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Input files")
                {
                    Patterns = ["*.csv", "*.xls", "*.xlsx"]
                }
            ]
        });

        var localPath = files.FirstOrDefault()?.TryGetLocalPath();
        ViewModel?.ImportFromPath(localPath);
    }

    private async void ExportZplButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (StorageProvider is null)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save generated ZPL",
            SuggestedFileName = "labels.zpl",
            FileTypeChoices =
            [
                new FilePickerFileType("ZPL files")
                {
                    Patterns = ["*.zpl"]
                }
            ]
        });

        var localPath = file?.TryGetLocalPath();
        ViewModel?.ExportZplToPath(localPath);
    }

    private void RemoveRowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ProductRowViewModel row })
        {
            return;
        }

        ViewModel?.RemoveRow(row);
    }
}