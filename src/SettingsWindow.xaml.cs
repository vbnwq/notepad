using System.Windows;
using System.Windows.Media;
using PersiaPad.Models;
using PersiaPad.Themes;

namespace PersiaPad;

public partial class SettingsWindow : Window
{
    public AppSettings Result { get; private set; }

    public SettingsWindow(AppSettings current)
    {
        InitializeComponent();

        // Work on a copy; only commit on OK.
        Result = Clone(current);

        // Themes
        foreach (var t in ThemeManager.All)
            ThemeCombo.Items.Add(t.DisplayName);
        ThemeCombo.SelectedIndex = Math.Max(0,
            ThemeManager.All.ToList().FindIndex(t => t.Name == current.ThemeName));

        // Fonts (monospace-friendly first, then all installed)
        string[] preferred = { "Consolas", "Cascadia Code", "Cascadia Mono", "Courier New",
                               "Vazirmatn", "Tahoma", "Segoe UI", "B Nazanin", "Sahel" };
        foreach (var f in preferred) FontCombo.Items.Add(f);
        foreach (var ff in Fonts.SystemFontFamilies.OrderBy(f => f.Source))
            if (!FontCombo.Items.Contains(ff.Source))
                FontCombo.Items.Add(ff.Source);
        FontCombo.Text = current.FontFamily;

        SizeSlider.Value = current.FontSize;
        SizeLabel.Text = ((int)current.FontSize).ToString();
        SizeSlider.ValueChanged += (_, _) =>
        {
            SizeLabel.Text = ((int)SizeSlider.Value).ToString();
        };

        ColorBox.Text = current.TextColorOverride;
        ColorBox.TextChanged += (_, _) => UpdateColorPreview();
        UpdateColorPreview();

        WordWrapCheck.IsChecked = current.WordWrap;
        LineNumbersCheck.IsChecked = current.ShowLineNumbers;
        HighlightLineCheck.IsChecked = current.HighlightCurrentLine;
        RestoreSessionCheck.IsChecked = current.RestoreSession;
    }

    private void UpdateColorPreview()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ColorBox.Text))
            {
                ColorPreview.Background = Brushes.Transparent;
                return;
            }
            ColorPreview.Background =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString(ColorBox.Text)!);
        }
        catch
        {
            ColorPreview.Background = Brushes.Transparent;
        }
    }

    private void ClearColor_Click(object sender, RoutedEventArgs e)
    {
        ColorBox.Text = "";
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Result.ThemeName = ThemeManager.All[ThemeCombo.SelectedIndex].Name;
        Result.FontFamily = string.IsNullOrWhiteSpace(FontCombo.Text) ? "Consolas" : FontCombo.Text;
        Result.FontSize = SizeSlider.Value;
        Result.TextColorOverride = ColorBox.Text.Trim();
        Result.WordWrap = WordWrapCheck.IsChecked == true;
        Result.ShowLineNumbers = LineNumbersCheck.IsChecked == true;
        Result.HighlightCurrentLine = HighlightLineCheck.IsChecked == true;
        Result.RestoreSession = RestoreSessionCheck.IsChecked == true;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static AppSettings Clone(AppSettings s) => new()
    {
        ThemeName = s.ThemeName,
        FontFamily = s.FontFamily,
        FontSize = s.FontSize,
        TextColorOverride = s.TextColorOverride,
        WordWrap = s.WordWrap,
        ShowLineNumbers = s.ShowLineNumbers,
        HighlightCurrentLine = s.HighlightCurrentLine,
        RightToLeft = s.RightToLeft,
        AutoSaveDebounceMs = s.AutoSaveDebounceMs,
        RestoreSession = s.RestoreSession,
    };
}
