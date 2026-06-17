using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PersiaPad.Services;

/// <summary>
/// Registers PersiaPad in Windows so it can be chosen as the default app for
/// text files, and opens the Windows "Default apps" dialog for the user to
/// confirm. All registry writes go under HKEY_CURRENT_USER, so no admin rights
/// are required and nothing system-wide is touched.
/// </summary>
public static class DefaultAppRegistration
{
    private const string ProgId = "PersiaPad.txt";
    private static readonly string[] Extensions = { ".txt", ".log", ".md", ".json", ".css", ".html", ".xml" };

    public static string ExePath =>
        Process.GetCurrentProcess().MainModule?.FileName
        ?? System.Reflection.Assembly.GetExecutingAssembly().Location;

    public static void RegisterAndPromptDefaults()
    {
        RegisterProgId();
        RegisterCapabilities();
        NotifyShellChanged();
        OpenDefaultAppsSettings();
    }

    private static void RegisterProgId()
    {
        using var classes = Registry.CurrentUser.CreateSubKey(@"Software\Classes");

        // The ProgId describing how to open our files.
        using (var prog = classes.CreateSubKey(ProgId))
        {
            prog.SetValue("", "PersiaPad Document");
            using (var icon = prog.CreateSubKey("DefaultIcon"))
                icon.SetValue("", $"\"{ExePath}\",0");
            using (var cmd = prog.CreateSubKey(@"shell\open\command"))
                cmd.SetValue("", $"\"{ExePath}\" \"%1\"");
        }

        // Advertise that we *can* open each extension (adds us to "Open with").
        foreach (var ext in Extensions)
        {
            using var extKey = classes.CreateSubKey(ext);
            using var open = extKey.CreateSubKey(@"OpenWithProgids");
            open.SetValue(ProgId, Array.Empty<byte>(), RegistryValueKind.None);
        }
    }

    private static void RegisterCapabilities()
    {
        const string capPath = @"Software\PersiaPad\Capabilities";
        using (var cap = Registry.CurrentUser.CreateSubKey(capPath))
        {
            cap.SetValue("ApplicationName", "PersiaPad");
            cap.SetValue("ApplicationDescription",
                "ویرایشگر متن سبک با پشتیبانی کامل فارسی، تم‌ها و رنگ‌بندی کد.");
            using var assoc = cap.CreateSubKey("FileAssociations");
            foreach (var ext in Extensions)
                assoc.SetValue(ext, ProgId);
        }

        using var reg = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications");
        reg.SetValue("PersiaPad", capPath);
    }

    private static void OpenDefaultAppsSettings()
    {
        // Best-effort: open Windows Settings > Default apps.
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true });
        }
        catch
        {
            try { Process.Start(new ProcessStartInfo("control", "/name Microsoft.DefaultPrograms") { UseShellExecute = true }); }
            catch { /* ignore */ }
        }
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

    private static void NotifyShellChanged()
    {
        const int SHCNE_ASSOCCHANGED = 0x08000000;
        const int SHCNF_IDLIST = 0x0000;
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
    }
}
