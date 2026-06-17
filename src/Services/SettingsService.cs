using System.IO;
using System.Text.Json;
using PersiaPad.Models;

namespace PersiaPad.Services;

/// <summary>
/// Loads and saves <see cref="AppSettings"/>. Writes are atomic
/// (temp file + move) so a crash mid-write never corrupts settings.
/// </summary>
public static class SettingsService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(AppPaths.SettingsFile))
            {
                var json = File.ReadAllText(AppPaths.SettingsFile);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s != null) return s;
            }
        }
        catch { /* fall through to defaults */ }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            AppPaths.EnsureCreated();
            var tmp = AppPaths.SettingsFile + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(settings, JsonOpts));
            if (File.Exists(AppPaths.SettingsFile))
                File.Replace(tmp, AppPaths.SettingsFile, null);
            else
                File.Move(tmp, AppPaths.SettingsFile);
        }
        catch { /* best effort; never crash on settings save */ }
    }
}
