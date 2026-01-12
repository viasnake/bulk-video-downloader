using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BulkVideoDownloader.Models;

namespace BulkVideoDownloader.Services;

public sealed class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BulkVideoDownloader");
        _settingsPath = Path.Combine(basePath, "settings.json");
    }

    public async Task<SettingsModel> LoadAsync()
    {
        if (!File.Exists(_settingsPath))
        {
            return new SettingsModel();
        }

        var json = await File.ReadAllTextAsync(_settingsPath).ConfigureAwait(false);
        return JsonSerializer.Deserialize<SettingsModel>(json) ?? new SettingsModel();
    }

    public async Task SaveAsync(SettingsModel settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_settingsPath, json).ConfigureAwait(false);
    }
}
