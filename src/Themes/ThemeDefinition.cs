using System.Windows.Media;

namespace PersiaPad.Themes;

/// <summary>
/// A single visual theme. Colors are stored as hex strings so themes can be
/// declared compactly and converted to brushes on demand.
/// </summary>
public class ThemeDefinition
{
    public string Name { get; init; } = "";
    public string DisplayName { get; init; } = "";

    // Window / chrome
    public string WindowBackground { get; init; } = "#1E1E1E";
    public string ChromeBackground { get; init; } = "#252526";
    public string ChromeForeground { get; init; } = "#D4D4D4";
    public string AccentColor { get; init; } = "#0E639C";
    public string BorderColor { get; init; } = "#3F3F46";

    // Editor surface
    public string EditorBackground { get; init; } = "#1E1E1E";
    public string EditorForeground { get; init; } = "#D4D4D4";
    public string LineNumberForeground { get; init; } = "#858585";
    public string CurrentLineBackground { get; init; } = "#2A2A2A";
    public string SelectionBackground { get; init; } = "#264F78";
    public string CaretBrush { get; init; } = "#FFFFFF";

    // Tabs
    public string TabActiveBackground { get; init; } = "#1E1E1E";
    public string TabInactiveBackground { get; init; } = "#2D2D2D";

    public static Color ParseColor(string hex) =>
        (Color)ColorConverter.ConvertFromString(hex)!;

    public static SolidColorBrush Brush(string hex)
    {
        var b = new SolidColorBrush(ParseColor(hex));
        b.Freeze();
        return b;
    }
}
