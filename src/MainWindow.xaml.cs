using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Highlighting;
using PersiaPad.Editor;
using PersiaPad.Models;
using PersiaPad.Services;
using PersiaPad.Themes;

namespace PersiaPad;

public partial class MainWindow : Window
{
    private AppSettings _settings = new();
    private readonly List<TabEntry> _tabs = new();

    /// <summary>Couples a TabItem, its editor control and its document model.</summary>
    private sealed class TabEntry
    {
        public required TabItem Tab { get; init; }
        public required PersianTextEditor Editor { get; init; }
        public required EditorDocument Doc { get; init; }
        public DispatcherTimer? SaveTimer { get; set; }
    }

    public MainWindow()
    {
        InitializeComponent();

        _settings = SettingsService.Load();
        ThemeManager.Apply(_settings.ThemeName);
        BuildThemeMenu();
        ThemeManager.ThemeChanged += _ => RefreshAllEditorsTheme();

        WordWrapMenu.IsChecked = _settings.WordWrap;
        LineNumbersMenu.IsChecked = _settings.ShowLineNumbers;
        RtlMenu.IsChecked = _settings.RightToLeft;

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;

        SetupCommandBindings();
    }

    // ===================================================================
    //  Startup / session restore
    // ===================================================================
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 1) Files passed on the command line (default-app open) win first.
        var opened = false;
        foreach (var path in App.StartupFiles)
        {
            if (File.Exists(path))
            {
                OpenFileInTab(path);
                opened = true;
            }
        }

        // 2) Restore any crash/close snapshots from the previous session.
        if (_settings.RestoreSession)
            opened |= RestoreSession();

        // 3) Nothing to show? Start with a fresh empty tab.
        if (!opened)
            NewTab();

        UpdateStatus();
    }

    private bool RestoreSession()
    {
        var restored = false;
        try
        {
            if (!Directory.Exists(AppPaths.SessionDir)) return false;
            foreach (var snap in Directory.GetFiles(AppPaths.SessionDir, "*.snapshot"))
            {
                var doc = EditorDocument.TryRestore(snap);
                if (doc != null)
                {
                    AddTabForDocument(doc, doc.RestoredCaret);
                    restored = true;
                }
            }
        }
        catch { }
        return restored;
    }

    // ===================================================================
    //  Tab creation
    // ===================================================================
    private TabEntry NewTab()
    {
        var doc = new EditorDocument();
        return AddTabForDocument(doc, 0);
    }

    private TabEntry AddTabForDocument(EditorDocument doc, int caret)
    {
        var editor = new PersianTextEditor
        {
            Document = doc.Document,
            FontFamily = new FontFamily(_settings.FontFamily),
            FontSize = _settings.FontSize,
            WordWrap = _settings.WordWrap,
            ShowLineNumbers = _settings.ShowLineNumbers,
        };
        // Paragraph direction (RTL for Persian, LTR for code). Mixed text inside
        // a line is still reordered correctly by WPF's bidi engine either way.
        editor.SetParagraphDirection(_settings.RightToLeft);

        ApplyEditorTheme(editor);
        ApplyHighlighting(editor, doc);

        // ---- Fast, debounced auto-save on every change -----------------
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_settings.AutoSaveDebounceMs)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            doc.WriteSnapshot(editor.CaretOffset);
        };

        doc.Document.TextChanged += (_, _) =>
        {
            timer.Stop();
            timer.Start();           // debounce burst typing -> snapshot once it settles
            ScheduleHighlightRefresh(editor, doc);
        };

        editor.TextArea.Caret.PositionChanged += (_, _) => UpdateStatus();
        editor.TextArea.SelectionChanged += (_, _) => UpdateStatus();

        // ---- Ctrl + mouse wheel => font size --------------------------
        editor.PreviewMouseWheel += (s, ev) =>
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                AdjustFontSize(ev.Delta > 0 ? 1 : -1);
                ev.Handled = true;
            }
        };

        // ---- Ctrl + D => duplicate line --------------------------------
        editor.PreviewKeyDown += (s, ev) =>
        {
            if (ev.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
            {
                editor.DuplicateLineOrSelection();
                ev.Handled = true;
            }
        };

        var header = new TextBlock();
        var entry = new TabEntry { Tab = new TabItem(), Editor = editor, Doc = doc, SaveTimer = timer };

        void UpdateHeader()
        {
            header.Text = doc.DisplayTitle;
            if (Tabs.SelectedItem == entry.Tab) UpdateStatus();
        }
        doc.HeaderChanged += () => Dispatcher.Invoke(UpdateHeader);
        UpdateHeader();

        // Header with a small close button.
        var headerPanel = new DockPanel { LastChildFill = true };
        var closeBtn = new Button
        {
            Content = "✕", Width = 18, Height = 18, Margin = new Thickness(6, 0, 0, 0),
            Padding = new Thickness(0), FontSize = 10, Cursor = Cursors.Hand,
            Background = Brushes.Transparent, BorderThickness = new Thickness(0),
            Foreground = (Brush)Application.Current.Resources["ChromeForegroundBrush"]
        };
        closeBtn.Click += (_, _) => CloseTabEntry(entry);
        DockPanel.SetDock(closeBtn, Dock.Right);
        headerPanel.Children.Add(closeBtn);
        headerPanel.Children.Add(header);

        entry.Tab.Header = headerPanel;
        entry.Tab.Content = editor;

        _tabs.Add(entry);
        Tabs.Items.Add(entry.Tab);
        Tabs.SelectedItem = entry.Tab;

        if (caret > 0 && caret <= doc.Document.TextLength)
            editor.CaretOffset = caret;

        editor.Focus();
        return entry;
    }

    private void OpenFileInTab(string path)
    {
        // Already open? Just activate it.
        var existing = _tabs.FirstOrDefault(t =>
            string.Equals(t.Doc.FilePath, path, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            Tabs.SelectedItem = existing.Tab;
            return;
        }

        try
        {
            var doc = EditorDocument.LoadFromFile(path);
            AddTabForDocument(doc, 0);
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطا در باز کردن فایل:\n" + ex.Message, "PersiaPad",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===================================================================
    //  Helpers
    // ===================================================================
    private TabEntry? Current =>
        _tabs.FirstOrDefault(t => t.Tab == Tabs.SelectedItem);

    private void ApplyEditorTheme(PersianTextEditor editor)
    {
        var t = ThemeManager.Current;
        editor.Background = ThemeDefinition.Brush(t.EditorBackground);
        editor.Foreground = string.IsNullOrWhiteSpace(_settings.TextColorOverride)
            ? ThemeDefinition.Brush(t.EditorForeground)
            : ThemeDefinition.Brush(_settings.TextColorOverride);
        editor.LineNumbersForeground = ThemeDefinition.Brush(t.LineNumberForeground);
        editor.TextArea.SelectionBrush = ThemeDefinition.Brush(t.SelectionBackground);
        editor.TextArea.Caret.CaretBrush = ThemeDefinition.Brush(t.CaretBrush);

        editor.Options.HighlightCurrentLine = _settings.HighlightCurrentLine;
        editor.TextArea.TextView.CurrentLineBackground = ThemeDefinition.Brush(t.CurrentLineBackground);
        editor.TextArea.TextView.CurrentLineBorder =
            new System.Windows.Media.Pen(ThemeDefinition.Brush(t.CurrentLineBackground), 0);
    }

    private void RefreshAllEditorsTheme()
    {
        foreach (var t in _tabs)
            ApplyEditorTheme(t.Editor);
    }

    private void ApplyHighlighting(PersianTextEditor editor, EditorDocument doc)
    {
        var det = LanguageDetector.Detect(doc.FilePath, doc.Document.Text);
        editor.SyntaxHighlighting = det.HighlightName == "None"
            ? null
            : HighlightingManager.Instance.GetDefinition(det.HighlightName);
    }

    private readonly Dictionary<PersianTextEditor, DispatcherTimer> _hlTimers = new();
    private void ScheduleHighlightRefresh(PersianTextEditor editor, EditorDocument doc)
    {
        // Re-detecting language on every keystroke is wasteful; debounce it.
        if (!_hlTimers.TryGetValue(editor, out var timer))
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                ApplyHighlighting(editor, doc);
                if (Current?.Editor == editor) UpdateStatus();
            };
            _hlTimers[editor] = timer;
        }
        timer.Stop();
        timer.Start();
    }

    private void AdjustFontSize(int direction)
    {
        var cur = Current;
        if (cur == null) return;
        double newSize = Math.Clamp(cur.Editor.FontSize + direction, 6, 96);
        // Apply to all editors so zoom is consistent across tabs.
        foreach (var t in _tabs) t.Editor.FontSize = newSize;
        _settings.FontSize = newSize;
        SettingsService.Save(_settings);
        StatusMessage.Text = $"اندازه فونت: {newSize:0}";
    }

    private void UpdateStatus()
    {
        var cur = Current;
        if (cur == null) return;

        var ed = cur.Editor;
        var caret = ed.TextArea.Caret;
        StatusCaret.Text = $"خط {caret.Line}، ستون {caret.Column}";

        int len = ed.Document.TextLength;
        if (ed.SelectionLength > 0)
            StatusLength.Text = $"{len} نویسه ({ed.SelectionLength} انتخاب)";
        else
            StatusLength.Text = $"{len} نویسه";

        StatusEncoding.Text = EncodingName(cur.Doc.Encoding);

        var det = LanguageDetector.Detect(cur.Doc.FilePath, ed.Document.Text);
        StatusLanguage.Text = det.StatusLabel;

        Title = cur.Doc.DisplayTitle.Replace(" ●", "") + " — PersiaPad";
    }

    private static string EncodingName(Encoding enc)
    {
        if (enc is UTF8Encoding u)
            return u.GetPreamble().Length > 0 ? "UTF-8 BOM" : "UTF-8";
        if (Equals(enc, Encoding.Unicode)) return "UTF-16 LE";
        if (Equals(enc, Encoding.BigEndianUnicode)) return "UTF-16 BE";
        return enc.WebName.ToUpperInvariant();
    }

    // ===================================================================
    //  Theme menu
    // ===================================================================
    private void BuildThemeMenu()
    {
        ThemeMenu.Items.Clear();
        foreach (var theme in ThemeManager.All)
        {
            var mi = new MenuItem
            {
                Header = theme.DisplayName,
                IsCheckable = true,
                IsChecked = theme.Name == ThemeManager.Current.Name,
                Tag = theme.Name
            };
            mi.Click += (s, _) =>
            {
                var name = (string)((MenuItem)s).Tag;
                ThemeManager.Apply(name);
                _settings.ThemeName = name;
                SettingsService.Save(_settings);
                foreach (MenuItem item in ThemeMenu.Items)
                    item.IsChecked = (string)item.Tag == name;
            };
            ThemeMenu.Items.Add(mi);
        }
    }

    // ===================================================================
    //  Closing -> flush everything (never lose work)
    // ===================================================================
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        FlushAllAutoSaves();
        SettingsService.Save(_settings);
        // NOTE: we deliberately keep snapshots on disk so dirty/unsaved tabs are
        // restored next launch. Saved (clean) docs already cleared their snapshot.
    }

    /// <summary>Force-write the snapshot of every tab right now.</summary>
    public void FlushAllAutoSaves()
    {
        foreach (var t in _tabs)
        {
            try
            {
                if (t.Doc.IsDirty)
                    t.Doc.WriteSnapshot(t.Editor.CaretOffset);
                else
                    t.Doc.ClearSnapshot();
            }
            catch { }
        }
    }

    private void CloseTabEntry(TabEntry entry)
    {
        // Closing a clean tab discards its snapshot; a dirty tab keeps it so the
        // text survives even an accidental close (user can reopen next launch).
        if (!entry.Doc.IsDirty)
            entry.Doc.ClearSnapshot();

        entry.SaveTimer?.Stop();
        _tabs.Remove(entry);
        Tabs.Items.Remove(entry.Tab);

        if (_tabs.Count == 0)
            NewTab();
    }
}
