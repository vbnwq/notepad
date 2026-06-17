using System.Windows;
using System.Windows.Controls;

namespace PersiaPad;

/// <summary>
/// A tiny, dependency-free Find &amp; Replace dialog built entirely in code so it
/// needs no XAML registration. Returns the find/replace strings on OK.
/// </summary>
public sealed class ReplaceWindow : Window
{
    private readonly TextBox _find = new() { Height = 28, Margin = new Thickness(0, 0, 0, 8) };
    private readonly TextBox _replace = new() { Height = 28, Margin = new Thickness(0, 0, 0, 8) };
    private readonly CheckBox _matchCase = new() { Content = "حساس به بزرگ/کوچک بودن حروف", Margin = new Thickness(0, 0, 0, 8) };

    public string FindText => _find.Text;
    public string ReplaceText => _replace.Text;
    public bool MatchCase => _matchCase.IsChecked == true;

    public ReplaceWindow(string seedFind)
    {
        Title = "یافتن و جایگزینی";
        Width = 380;
        Height = 240;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        FlowDirection = FlowDirection.RightToLeft;

        _find.Text = seedFind;

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = "متن جستجو:", Margin = new Thickness(0, 0, 0, 4) });
        panel.Children.Add(_find);
        panel.Children.Add(new TextBlock { Text = "جایگزین با:", Margin = new Thickness(0, 0, 0, 4) });
        panel.Children.Add(_replace);
        panel.Children.Add(_matchCase);

        var ok = new Button { Content = "جایگزینی همه", Width = 110, Height = 32, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new Button { Content = "انصراف", Width = 90, Height = 32, IsCancel = true };
        ok.Click += (_, _) => { DialogResult = true; Close(); };
        cancel.Click += (_, _) => { DialogResult = false; Close(); };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);

        Content = panel;

        Loaded += (_, _) => { _find.Focus(); _find.SelectAll(); };
    }
}
