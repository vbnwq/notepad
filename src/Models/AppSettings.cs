namespace PersiaPad.Models;

/// <summary>
/// Persisted user preferences. Serialized to settings.json.
/// </summary>
public class AppSettings
{
    public string ThemeName { get; set; } = "Dark";

    // Fallback chain: a monospace font for code, then Persian-capable fonts so
    // mixed Persian/English always renders with proper glyphs (WPF picks the
    // first family that has a glyph for each character).
    public string FontFamily { get; set; } = "Consolas, Tahoma, Segoe UI, Vazirmatn";
    public double FontSize { get; set; } = 15;

    /// <summary>Optional override for editor foreground (hex like #RRGGBB). Empty = use theme.</summary>
    public string TextColorOverride { get; set; } = "";

    public bool WordWrap { get; set; } = true;
    public bool ShowLineNumbers { get; set; } = true;
    public bool HighlightCurrentLine { get; set; } = true;

    /// <summary>Paragraph direction. True = RTL (Persian), False = LTR (default for code).</summary>
    public bool RightToLeft { get; set; } = false;

    /// <summary>Auto-save snapshot interval in milliseconds (debounced after a keystroke).</summary>
    public int AutoSaveDebounceMs { get; set; } = 400;

    /// <summary>Whether to reopen the last session on startup.</summary>
    public bool RestoreSession { get; set; } = true;
}
