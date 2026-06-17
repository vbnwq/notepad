using System.IO;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Search;
using Microsoft.Win32;
using PersiaPad.Editor;
using PersiaPad.Services;

namespace PersiaPad;

/// <summary>
/// Menu/keyboard command handlers split out of MainWindow for readability.
/// </summary>
public partial class MainWindow
{
    private void SetupCommandBindings()
    {
        void Bind(RoutedUICommand cmd, ExecutedRoutedEventHandler h)
            => CommandBindings.Add(new CommandBinding(cmd, h));

        Bind(ApplicationCommands.New,    (_, _) => New_Click(null!, null!));
        Bind(ApplicationCommands.Open,   (_, _) => Open_Click(null!, null!));
        Bind(ApplicationCommands.Save,   (_, _) => Save_Click(null!, null!));
        Bind(ApplicationCommands.SaveAs, (_, _) => SaveAs_Click(null!, null!));
        Bind(ApplicationCommands.Close,  (_, _) => CloseTab_Click(null!, null!));
        Bind(ApplicationCommands.Find,   (_, _) => Find_Click(null!, null!));
        Bind(ApplicationCommands.Replace,(_, _) => Replace_Click(null!, null!));
    }

    // ---- File --------------------------------------------------------------
    private void New_Click(object sender, RoutedEventArgs e) => NewTab();

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "باز کردن فایل",
            Filter = "همه فایل‌ها (*.*)|*.*|فایل متنی (*.txt)|*.txt|" +
                     "کد (*.html;*.css;*.js;*.json;*.py;*.php;*.cs;*.cpp;*.c;*.xml)|" +
                     "*.html;*.css;*.js;*.json;*.py;*.php;*.cs;*.cpp;*.c;*.xml",
            Multiselect = true
        };
        if (dlg.ShowDialog() == true)
            foreach (var f in dlg.FileNames)
                OpenFileInTab(f);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var cur = Current;
        if (cur == null) return;
        if (cur.Doc.FilePath == null) { SaveAs_Click(sender, e); return; }
        try
        {
            cur.Doc.Save(cur.Doc.FilePath);
            StatusMessage.Text = "ذخیره شد ✔";
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطا در ذخیره:\n" + ex.Message, "PersiaPad",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        var cur = Current;
        if (cur == null) return;
        var dlg = new SaveFileDialog
        {
            Title = "ذخیره به‌نام",
            Filter = "فایل متنی (*.txt)|*.txt|همه فایل‌ها (*.*)|*.*",
            FileName = cur.Doc.FilePath != null
                ? Path.GetFileName(cur.Doc.FilePath) : "بدون عنوان.txt"
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                cur.Doc.Save(dlg.FileName);
                ApplyHighlighting(cur.Editor, cur.Doc);
                UpdateStatus();
                StatusMessage.Text = "ذخیره شد ✔";
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در ذخیره:\n" + ex.Message, "PersiaPad",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        var cur = Current;
        if (cur != null) CloseTabEntry(cur);
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    // ---- Edit --------------------------------------------------------------
    private void Undo_Click(object sender, RoutedEventArgs e) { if (Current?.Editor.CanUndo == true) Current.Editor.Undo(); }
    private void Redo_Click(object sender, RoutedEventArgs e) { if (Current?.Editor.CanRedo == true) Current.Editor.Redo(); }
    private void Cut_Click(object sender, RoutedEventArgs e) => Current?.Editor.Cut();
    private void Copy_Click(object sender, RoutedEventArgs e) => Current?.Editor.Copy();
    private void Paste_Click(object sender, RoutedEventArgs e) => Current?.Editor.Paste();
    private void SelectAll_Click(object sender, RoutedEventArgs e) => Current?.Editor.SelectAll();
    private void Duplicate_Click(object sender, RoutedEventArgs e) => Current?.Editor.DuplicateLineOrSelection();

    private void Find_Click(object sender, RoutedEventArgs e)
    {
        if (Current == null) return;
        var panel = SearchPanel.Install(Current.Editor);
        // Pre-fill with the selected word so "find selection" is one keystroke.
        if (Current.Editor.SelectionLength > 0)
            panel.SearchPattern = Current.Editor.SelectedText;
        panel.Open();
    }

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        if (Current == null) return;
        var seed = Current.Editor.SelectionLength > 0 ? Current.Editor.SelectedText : "";
        var dlg = new ReplaceWindow(seed) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            int count = ReplaceAll(Current.Editor, dlg.FindText, dlg.ReplaceText, dlg.MatchCase);
            StatusMessage.Text = count > 0 ? $"{count} مورد جایگزین شد ✔" : "موردی یافت نشد";
        }
    }

    private static int ReplaceAll(PersianTextEditor editor, string find, string replace, bool matchCase)
    {
        if (string.IsNullOrEmpty(find)) return 0;
        var text = editor.Document.Text;
        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        int count = 0, idx = 0;
        var sb = new System.Text.StringBuilder();
        while (true)
        {
            int found = text.IndexOf(find, idx, comparison);
            if (found < 0) { sb.Append(text, idx, text.Length - idx); break; }
            sb.Append(text, idx, found - idx);
            sb.Append(replace);
            idx = found + find.Length;
            count++;
        }
        if (count > 0)
        {
            editor.Document.BeginUpdate();
            editor.Document.Text = sb.ToString();
            editor.Document.EndUpdate();
        }
        return count;
    }

    // ---- View --------------------------------------------------------------
    private void WordWrap_Click(object sender, RoutedEventArgs e)
    {
        _settings.WordWrap = WordWrapMenu.IsChecked;
        foreach (var t in _tabs) t.Editor.WordWrap = _settings.WordWrap;
        SettingsService.Save(_settings);
    }

    private void LineNumbers_Click(object sender, RoutedEventArgs e)
    {
        _settings.ShowLineNumbers = LineNumbersMenu.IsChecked;
        foreach (var t in _tabs) t.Editor.ShowLineNumbers = _settings.ShowLineNumbers;
        SettingsService.Save(_settings);
    }

    private void Rtl_Click(object sender, RoutedEventArgs e)
    {
        _settings.RightToLeft = RtlMenu.IsChecked;
        foreach (var t in _tabs) t.Editor.SetParagraphDirection(_settings.RightToLeft);
        SettingsService.Save(_settings);
        StatusMessage.Text = _settings.RightToLeft ? "جهت: راست‌به‌چپ" : "جهت: چپ‌به‌راست";
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e) => AdjustFontSize(1);
    private void ZoomOut_Click(object sender, RoutedEventArgs e) => AdjustFontSize(-1);

    // ---- Tools -------------------------------------------------------------
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow(_settings) { Owner = this };
        if (win.ShowDialog() == true)
        {
            _settings = win.Result;
            SettingsService.Save(_settings);
            ApplySettingsToAllEditors();
        }
    }

    private void ApplySettingsToAllEditors()
    {
        foreach (var t in _tabs)
        {
            t.Editor.FontFamily = new System.Windows.Media.FontFamily(_settings.FontFamily);
            t.Editor.FontSize = _settings.FontSize;
            t.Editor.WordWrap = _settings.WordWrap;
            t.Editor.ShowLineNumbers = _settings.ShowLineNumbers;
            t.Editor.SetParagraphDirection(_settings.RightToLeft);
            ApplyEditorTheme(t.Editor);
        }
        ThemeManager.Apply(_settings.ThemeName);
        foreach (System.Windows.Controls.MenuItem item in ThemeMenu.Items)
            item.IsChecked = (string)item.Tag == _settings.ThemeName;
        WordWrapMenu.IsChecked = _settings.WordWrap;
        LineNumbersMenu.IsChecked = _settings.ShowLineNumbers;
    }

    private void SetDefault_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DefaultAppRegistration.RegisterAndPromptDefaults();
            StatusMessage.Text = "ثبت انجام شد. در پنجره ویندوز PersiaPad را انتخاب کنید.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "برای تنظیم به‌عنوان برنامه پیش‌فرض، PersiaPad باید نصب شده باشد.\n\n" + ex.Message,
                "PersiaPad", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Tabs_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateStatus();
        Current?.Editor.Focus();
    }
}
