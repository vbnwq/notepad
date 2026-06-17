using System.IO;

namespace PersiaPad.Services;

/// <summary>
/// Centralizes every path PersiaPad uses. All user data lives under
/// %LOCALAPPDATA%\PersiaPad so the app stays portable and needs no admin rights.
/// </summary>
public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PersiaPad");

    /// <summary>Folder holding the live auto-save snapshots of open tabs.</summary>
    public static string SessionDir { get; } = Path.Combine(Root, "session");

    /// <summary>Settings file (themes, font, colors, etc.).</summary>
    public static string SettingsFile { get; } = Path.Combine(Root, "settings.json");

    /// <summary>Description of the last session (which tabs were open).</summary>
    public static string SessionIndexFile { get; } = Path.Combine(SessionDir, "session.json");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(SessionDir);
    }

    public static string SnapshotFor(string tabId) =>
        Path.Combine(SessionDir, tabId + ".snapshot");
}
