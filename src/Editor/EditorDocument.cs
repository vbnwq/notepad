using System.IO;
using System.Text;
using System.Text.Json;
using ICSharpCode.AvalonEdit.Document;
using PersiaPad.Services;

namespace PersiaPad.Editor;

/// <summary>
/// Backing model for one open tab. Owns the AvalonEdit TextDocument, knows its
/// file path (if any) and manages its own crash-safe auto-save snapshot.
///
/// Auto-save design:
///  - Every edit is recorded to an in-memory snapshot and a debounced timer
///    flushes it to %LOCALAPPDATA%\PersiaPad\session\{id}.snapshot.
///  - The snapshot stores the text PLUS the original path, so on next launch
///    we can restore exactly what the user was typing even if they never
///    pressed Ctrl+S and the app/PC crashed or was closed.
/// </summary>
public sealed class EditorDocument
{
    public string Id { get; }
    public TextDocument Document { get; } = new();

    private string? _filePath;
    public string? FilePath
    {
        get => _filePath;
        set { _filePath = value; OnHeaderChanged(); }
    }

    /// <summary>True when in-memory text differs from the file on disk.</summary>
    public bool IsDirty { get; private set; }

    /// <summary>Encoding to write back with (defaults to UTF-8 w/o BOM).</summary>
    public Encoding Encoding { get; set; } = new UTF8Encoding(false);

    public event Action? HeaderChanged;

    public string DisplayTitle =>
        (FilePath != null ? Path.GetFileName(FilePath) : "بدون عنوان") + (IsDirty ? " ●" : "");

    public EditorDocument(string? id = null)
    {
        Id = id ?? Guid.NewGuid().ToString("N");
        Document.TextChanged += (_, _) =>
        {
            IsDirty = true;
            OnHeaderChanged();
        };
    }

    private void OnHeaderChanged() => HeaderChanged?.Invoke();

    // ---- Loading -----------------------------------------------------------

    public static EditorDocument LoadFromFile(string path)
    {
        var doc = new EditorDocument { FilePath = path };
        var (text, enc) = ReadAllTextWithEncoding(path);
        doc.Document.Text = text;
        doc.Encoding = enc;
        doc.IsDirty = false;
        doc.OnHeaderChanged();
        return doc;
    }

    private static (string text, Encoding enc) ReadAllTextWithEncoding(string path)
    {
        var bytes = File.ReadAllBytes(path);

        // Detect BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return (new UTF8Encoding(true).GetString(bytes, 3, bytes.Length - 3), new UTF8Encoding(true));
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return (Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2), Encoding.Unicode);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return (Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2), Encoding.BigEndianUnicode);

        // No BOM: assume UTF-8 (best for Persian). Falls back gracefully.
        return (new UTF8Encoding(false).GetString(bytes), new UTF8Encoding(false));
    }

    // ---- Saving to the real file ------------------------------------------

    public void Save(string path)
    {
        // Atomic write: temp + replace so a crash never truncates the file.
        var tmp = path + ".pptmp";
        File.WriteAllText(tmp, Document.Text, Encoding);
        if (File.Exists(path))
            File.Replace(tmp, path, null);
        else
            File.Move(tmp, path);

        FilePath = path;
        IsDirty = false;
        OnHeaderChanged();
        // Snapshot no longer needed once it's safely on disk.
        ClearSnapshot();
    }

    // ---- Crash-safe auto-save snapshot -------------------------------------

    private record Snapshot(string? FilePath, string Text, int Caret);

    public void WriteSnapshot(int caretOffset)
    {
        try
        {
            var snap = new Snapshot(FilePath, Document.Text, caretOffset);
            var tmp = AppPaths.SnapshotFor(Id) + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(snap));
            var final = AppPaths.SnapshotFor(Id);
            if (File.Exists(final)) File.Replace(tmp, final, null);
            else File.Move(tmp, final);
        }
        catch { /* best effort */ }
    }

    public void ClearSnapshot()
    {
        try
        {
            var p = AppPaths.SnapshotFor(Id);
            if (File.Exists(p)) File.Delete(p);
        }
        catch { }
    }

    public static EditorDocument? TryRestore(string snapshotPath)
    {
        try
        {
            var json = File.ReadAllText(snapshotPath);
            var snap = JsonSerializer.Deserialize<Snapshot>(json);
            if (snap == null) return null;

            var id = Path.GetFileNameWithoutExtension(snapshotPath);
            var doc = new EditorDocument(id) { FilePath = snap.FilePath };
            doc.Document.Text = snap.Text;
            doc.RestoredCaret = snap.Caret;

            // If the snapshot equals what's on disk, it isn't dirty.
            doc.IsDirty = true;
            if (snap.FilePath != null && File.Exists(snap.FilePath))
            {
                try
                {
                    var (onDisk, _) = ReadAllTextWithEncoding(snap.FilePath);
                    if (onDisk == snap.Text) doc.IsDirty = false;
                }
                catch { }
            }
            doc.OnHeaderChanged();
            return doc;
        }
        catch
        {
            return null;
        }
    }

    public int RestoredCaret { get; private set; }
}
