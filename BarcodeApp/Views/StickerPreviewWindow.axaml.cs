using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Qivisoft.BarcodeApp.Views;

public partial class StickerPreviewWindow : Window
{
    public StickerPreviewWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
