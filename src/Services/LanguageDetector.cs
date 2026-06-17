using System.IO;
using System.Text.RegularExpressions;

namespace PersiaPad.Services;

/// <summary>
/// Detects the content language of a document, both from file extension and,
/// when there is no extension (or it is .txt), from the actual content.
/// Produces the AvalonEdit highlighting key and the status-bar label
/// (e.g. "text", "text, html", "html", "json", ...).
/// </summary>
public static class LanguageDetector
{
    public record Detection(string HighlightName, string StatusLabel);

    private static readonly Detection PlainText = new("None", "text");

    /// <summary>Map of extension -> (AvalonEdit highlight key, label).</summary>
    private static readonly Dictionary<string, Detection> ByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".json"] = new("Json", "json"),
        [".html"] = new("HTML", "html"),
        [".htm"]  = new("HTML", "html"),
        [".xml"]  = new("XML", "xml"),
        [".css"]  = new("CSS", "css"),
        [".php"]  = new("PHP", "php"),
        [".cs"]   = new("C#", "c#"),
        [".cpp"]  = new("C++", "c++"),
        [".cc"]   = new("C++", "c++"),
        [".cxx"]  = new("C++", "c++"),
        [".hpp"]  = new("C++", "c++"),
        [".h"]    = new("C++", "c++"),
        [".c"]    = new("C++", "c"),
        [".py"]   = new("Python", "python"),
        [".js"]   = new("JavaScript", "javascript"),
        [".ts"]   = new("JavaScript", "typescript"),
        [".java"] = new("Java", "java"),
        [".sql"]  = new("TSQL", "sql"),
        [".md"]   = new("MarkDown", "markdown"),
        [".xaml"] = new("XML", "xaml"),
        [".csv"]  = new("None", "csv"),
        [".txt"]  = PlainText,
        [".log"]  = PlainText,
    };

    public static Detection Detect(string? filePath, string content)
    {
        // 1) Trust a meaningful extension first.
        if (!string.IsNullOrEmpty(filePath))
        {
            var ext = Path.GetExtension(filePath);
            if (!string.IsNullOrEmpty(ext) && ByExtension.TryGetValue(ext, out var byExt))
            {
                // For .txt we still sniff content (might be pasted html/json).
                if (!ext.Equals(".txt", StringComparison.OrdinalIgnoreCase) &&
                    !ext.Equals(".log", StringComparison.OrdinalIgnoreCase))
                    return byExt;
            }
        }

        // 2) Content sniffing for unknown / plain-text files.
        return SniffContent(content);
    }

    private static Detection SniffContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return PlainText;

        var sample = content.Length > 4000 ? content[..4000] : content;
        var trimmed = sample.TrimStart();

        // JSON: starts with { or [ and parses-ish.
        if ((trimmed.StartsWith("{") || trimmed.StartsWith("[")) && LooksLikeJson(trimmed))
            return new("Json", "json");

        bool hasHtml = Regex.IsMatch(sample,
            @"<\s*(!doctype|html|head|body|div|span|p|a|table|script|style|h[1-6]|ul|ol|li|img|br|meta|link)\b",
            RegexOptions.IgnoreCase);

        if (hasHtml)
        {
            // Is it pure HTML or text mixed with HTML?
            bool hasMeaningfulText = HasNonTagText(sample);
            return hasMeaningfulText
                ? new("HTML", "text, html")
                : new("HTML", "html");
        }

        // CSS heuristic: selector { prop: value; }
        if (Regex.IsMatch(sample, @"[.#]?[\w\-]+\s*\{[^}]*:[^}]*;", RegexOptions.Singleline))
            return new("CSS", "css");

        if (Regex.IsMatch(sample, @"<\?php") )
            return new("PHP", "php");

        if (Regex.IsMatch(sample, @"^\s*(def |class |import |from \w+ import)", RegexOptions.Multiline))
            return new("Python", "python");

        if (Regex.IsMatch(sample, @"\b(using\s+System|namespace\s+\w+|public\s+class|Console\.WriteLine)\b"))
            return new("C#", "c#");

        if (Regex.IsMatch(sample, @"#include\s*<|std::|int\s+main\s*\("))
            return new("C++", "c++");

        return PlainText;
    }

    private static bool HasNonTagText(string s)
    {
        // Strip tags, see if real words remain.
        var noTags = Regex.Replace(s, @"<[^>]+>", " ");
        noTags = Regex.Replace(noTags, @"\s+", " ").Trim();
        return noTags.Length >= 3 && Regex.IsMatch(noTags, @"\p{L}{2,}");
    }

    private static bool LooksLikeJson(string s)
    {
        try
        {
            using var _ = System.Text.Json.JsonDocument.Parse(s);
            return true;
        }
        catch
        {
            // Partial/while-typing JSON: accept if it has key:value structure.
            return Regex.IsMatch(s, "\"[^\"]+\"\\s*:");
        }
    }
}
