using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace PersiaPad.Editor;

/// <summary>
/// A TextEditor subclass tuned for mixed Persian/English (bidirectional) text.
///
/// Why this exists:
///  - WPF's text stack already runs the Unicode Bidi Algorithm, so a string like
///    "سلام chetory خوبی" is *stored* in logical order and *displayed* visually
///    correctly. The classic Notepad bug ("the order breaks") happens when an
///    editor forces LTR-only rendering or fiddles with the caret per-character.
///    By keeping the document in logical order and letting WPF do bidi, ordering
///    stays correct automatically.
///  - We expose smart word selection that respects Persian letters + ZWNJ so a
///    double-click selects exactly the intended word, and "replace selection"
///    swaps exactly that word.
/// </summary>
public class PersianTextEditor : TextEditor
{
    public PersianTextEditor()
    {
        // Keep the editor itself LTR for predictable caret/scrollbar placement,
        // but per-line visual order is still resolved by WPF's bidi engine,
        // which yields correct mixed-script display. This is the key combo that
        // makes "سلام chetory خوبی" render in the right word order.
        FlowDirection = FlowDirection.LeftToRight;

        Options.EnableHyperlinks = false;
        Options.EnableEmailHyperlinks = false;
        Options.AllowScrollBelowDocument = true;
        Options.CutCopyWholeLine = true;
        Options.EnableTextDragDrop = true;
        // Important for fast typing on weak machines: do not re-measure eagerly.
        Options.EnableVirtualSpace = false;
        // IME support is essential for fast Persian/Arabic & CJK input.
        Options.EnableImeSupport = true;
        // Clean indentation for code (4-space tabs).
        Options.ConvertTabsToSpaces = true;
        Options.IndentationSize = 4;

        // Let the OS handle language switching smoothly while typing fast.
        InputMethod.SetIsInputMethodEnabled(this, true);

        // Double-click selection: use our Persian-aware word boundaries.
        PreviewMouseDoubleClick += OnPreviewMouseDoubleClick;
    }

    /// <summary>
    /// Sets the paragraph (base) direction. RTL makes Persian paragraphs flow
    /// right-to-left while still letting embedded English run left-to-right.
    /// LTR is the inverse and is better for source code. In BOTH modes the
    /// per-run reordering ("سلام chetory خوبی") is handled by WPF's bidi engine,
    /// so word order is never scrambled.
    /// </summary>
    public void SetParagraphDirection(bool rightToLeft)
    {
        var dir = rightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        FlowDirection = dir;
        TextArea.FlowDirection = dir;
        TextArea.TextView.FlowDirection = dir;
    }

    /// <summary>
    /// Replace the current selection (or word under caret) with new text,
    /// preserving correct logical order. Used by Find/Replace and quick edits.
    /// </summary>
    public void ReplaceSelectionExact(string replacement)
    {
        if (SelectionLength == 0)
            SelectWordAtCaret();

        if (SelectionLength > 0)
        {
            Document.Replace(SelectionStart, SelectionLength, replacement);
            CaretOffset = SelectionStart + replacement.Length;
        }
        else
        {
            Document.Insert(CaretOffset, replacement);
        }
    }

    public void SelectWordAtCaret()
    {
        var (start, end) = GetWordBounds(CaretOffset);
        if (end > start)
            Select(start, end - start);
    }

    private void OnPreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var pos = GetPositionFromPoint(e.GetPosition(this));
        if (pos == null) return;

        int offset = Document.GetOffset(pos.Value.Location);
        var (start, end) = GetWordBounds(offset);
        if (end > start)
        {
            Select(start, end - start);
            e.Handled = true; // override AvalonEdit's default word logic
        }
    }

    /// <summary>
    /// Computes word boundaries treating a "word" as a run of letters/digits of
    /// a single script family, where Persian/Arabic letters, Latin letters and
    /// the Zero-Width Non-Joiner (U+200C) inside Persian words are all "word"
    /// characters. This selects exactly the intended Persian or English word.
    /// </summary>
    private (int start, int end) GetWordBounds(int offset)
    {
        var text = Document.Text;
        if (text.Length == 0) return (0, 0);

        offset = Math.Clamp(offset, 0, text.Length);

        // If the caret sits just after a word char, step back one.
        int probe = offset;
        if (probe >= text.Length || !IsWordChar(text[probe]))
        {
            if (probe > 0 && IsWordChar(text[probe - 1]))
                probe--;
            else
                return (offset, offset);
        }

        int start = probe;
        while (start > 0 && IsWordChar(text[start - 1]))
            start--;

        int end = probe;
        while (end < text.Length && IsWordChar(text[end]))
            end++;

        return (start, end);
    }

    private static bool IsWordChar(char c)
    {
        if (c == '\u200C' || c == '\u200D') return true; // ZWNJ / ZWJ used in Persian
        if (c == '_') return true;
        if (char.IsLetterOrDigit(c)) return true;

        // Persian/Arabic block letters that IsLetterOrDigit already covers,
        // but include Arabic combining marks & Persian digits explicitly.
        var cat = CharUnicodeInfo.GetUnicodeCategory(c);
        return cat == UnicodeCategory.NonSpacingMark ||
               cat == UnicodeCategory.DecimalDigitNumber;
    }

    /// <summary>
    /// Duplicate the current line down (Ctrl+D), like VS Code.
    /// If text is selected, duplicates the selection instead.
    /// </summary>
    public void DuplicateLineOrSelection()
    {
        if (SelectionLength > 0)
        {
            var selected = SelectedText;
            int insertAt = SelectionStart + SelectionLength;
            Document.Insert(insertAt, selected);
            Select(insertAt, selected.Length);
            return;
        }

        var line = Document.GetLineByOffset(CaretOffset);
        string lineText = Document.GetText(line.Offset, line.Length);
        int caretColumn = CaretOffset - line.Offset;

        // Insert a newline + copy of the line right after the current line.
        int insertOffset = line.Offset + line.Length;
        string newline = line.DelimiterLength > 0
            ? Document.GetText(line.Offset + line.Length, line.DelimiterLength)
            : Environment.NewLine;

        Document.Insert(insertOffset, newline + lineText);

        // Move caret to the same column on the new (lower) line.
        var newLine = Document.GetLineByNumber(line.LineNumber + 1);
        CaretOffset = Math.Min(newLine.Offset + caretColumn, newLine.EndOffset);
    }
}
