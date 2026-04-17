using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BarcodeApp.Theming;
using BarcodeApp.ViewModels;
using BarcodeApp.Views;

namespace BarcodeApp;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        RequestedThemeVariant = ThemeConfiguration.ToThemeVariant(
            ThemeConfiguration.ResolveFromEnvironment(Environment.GetEnvironmentVariable("BARCODEAPP_THEME")));
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };

        base.OnFrameworkInitializationCompleted();
    }
}