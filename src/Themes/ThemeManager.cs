using System.Windows;
using System.Windows.Media;

namespace PersiaPad.Themes;

/// <summary>
/// Owns the five built-in themes and applies one to the application's
/// resource dictionary so all controls update live.
/// </summary>
public static class ThemeManager
{
    public static IReadOnlyList<ThemeDefinition> All { get; } = new List<ThemeDefinition>
    {
        // 1) White / Light
        new()
        {
            Name = "Light", DisplayName = "تم سفید",
            WindowBackground = "#FFFFFF", ChromeBackground = "#F3F3F3", ChromeForeground = "#1F1F1F",
            AccentColor = "#0066CC", BorderColor = "#D0D0D0",
            EditorBackground = "#FFFFFF", EditorForeground = "#1F1F1F",
            LineNumberForeground = "#A0A0A0", CurrentLineBackground = "#F0F4FF",
            SelectionBackground = "#ADD6FF", CaretBrush = "#000000",
            TabActiveBackground = "#FFFFFF", TabInactiveBackground = "#E8E8E8",
        },
        // 2) Black / Dark
        new()
        {
            Name = "Dark", DisplayName = "تم مشکی",
            WindowBackground = "#1E1E1E", ChromeBackground = "#252526", ChromeForeground = "#D4D4D4",
            AccentColor = "#0E639C", BorderColor = "#3F3F46",
            EditorBackground = "#1E1E1E", EditorForeground = "#D4D4D4",
            LineNumberForeground = "#858585", CurrentLineBackground = "#2A2A2D",
            SelectionBackground = "#264F78", CaretBrush = "#FFFFFF",
            TabActiveBackground = "#1E1E1E", TabInactiveBackground = "#2D2D2D",
        },
        // 3) Code (VS Code-ish blue/dark for coding)
        new()
        {
            Name = "Code", DisplayName = "تم کد",
            WindowBackground = "#0D1117", ChromeBackground = "#161B22", ChromeForeground = "#C9D1D9",
            AccentColor = "#1F6FEB", BorderColor = "#30363D",
            EditorBackground = "#0D1117", EditorForeground = "#C9D1D9",
            LineNumberForeground = "#6E7681", CurrentLineBackground = "#161B22",
            SelectionBackground = "#1F3A5F", CaretBrush = "#58A6FF",
            TabActiveBackground = "#0D1117", TabInactiveBackground = "#161B22",
        },
        // 4) Hacker (green on black)
        new()
        {
            Name = "Hacker", DisplayName = "تم هکری",
            WindowBackground = "#000000", ChromeBackground = "#0A0A0A", ChromeForeground = "#00FF66",
            AccentColor = "#00FF66", BorderColor = "#0F3D0F",
            EditorBackground = "#000000", EditorForeground = "#00FF66",
            LineNumberForeground = "#117711", CurrentLineBackground = "#0A1A0A",
            SelectionBackground = "#0B5C2A", CaretBrush = "#00FF66",
            TabActiveBackground = "#000000", TabInactiveBackground = "#0A0A0A",
        },
        // 5) Cyberpunk (neon magenta/cyan on deep purple)
        new()
        {
            Name = "Cyberpunk", DisplayName = "تم سایبرپانک",
            WindowBackground = "#0B0221", ChromeBackground = "#160A33", ChromeForeground = "#00F0FF",
            AccentColor = "#FF2A9D", BorderColor = "#3A1B6B",
            EditorBackground = "#0B0221", EditorForeground = "#F2F0FF",
            LineNumberForeground = "#8A5CFF", CurrentLineBackground = "#1A0A40",
            SelectionBackground = "#5A1E7A", CaretBrush = "#FF2A9D",
            TabActiveBackground = "#0B0221", TabInactiveBackground = "#160A33",
        },
    };

    public static ThemeDefinition Current { get; private set; } = All[1]; // Dark default

    public static event Action<ThemeDefinition>? ThemeChanged;

    public static ThemeDefinition GetByName(string name) =>
        All.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? All[1];

    public static void Apply(string name)
    {
        var theme = GetByName(name);
        Current = theme;

        var res = Application.Current.Resources;
        void Set(string key, Brush b) => res[key] = b;

        Set("WindowBackgroundBrush",    ThemeDefinition.Brush(theme.WindowBackground));
        Set("ChromeBackgroundBrush",    ThemeDefinition.Brush(theme.ChromeBackground));
        Set("ChromeForegroundBrush",    ThemeDefinition.Brush(theme.ChromeForeground));
        Set("AccentBrush",              ThemeDefinition.Brush(theme.AccentColor));
        Set("BorderBrush",              ThemeDefinition.Brush(theme.BorderColor));
        Set("EditorBackgroundBrush",    ThemeDefinition.Brush(theme.EditorBackground));
        Set("EditorForegroundBrush",    ThemeDefinition.Brush(theme.EditorForeground));
        Set("LineNumberBrush",          ThemeDefinition.Brush(theme.LineNumberForeground));
        Set("CurrentLineBrush",         ThemeDefinition.Brush(theme.CurrentLineBackground));
        Set("SelectionBrush",           ThemeDefinition.Brush(theme.SelectionBackground));
        Set("CaretBrush",               ThemeDefinition.Brush(theme.CaretBrush));
        Set("TabActiveBrush",           ThemeDefinition.Brush(theme.TabActiveBackground));
        Set("TabInactiveBrush",         ThemeDefinition.Brush(theme.TabInactiveBackground));

        ThemeChanged?.Invoke(theme);
    }
}
