using System.Text.Json;
using BarcodeApp.Models;

namespace BarcodeApp.Services;

public interface IAppSettingsStore
{
    AppUserSettings Load();
    void Save(AppUserSettings settings);
}

public sealed class FileAppSettingsStore : IAppSettingsStore
{
    private readonly string _settingsPath;

    public FileAppSettingsStore(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? BuildDefaultSettingsPath();
    }

    public AppUserSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new AppUserSettings();

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppUserSettings>(json) ?? new AppUserSettings();
        }
        catch
        {
            return new AppUserSettings();
        }
    }

    public void Save(AppUserSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Best effort persistence. The app should continue to work even if writing settings fails.
        }
    }

    private static string BuildDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "BarcodeApp", "settings.json");
    }
}
