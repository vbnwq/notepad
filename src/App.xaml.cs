using System.Windows;
using PersiaPad.Services;

namespace PersiaPad;

public partial class App : Application
{
    /// <summary>
    /// File paths passed on the command line (e.g. when opening a .txt by
    /// double-clicking it after PersiaPad is set as the default app).
    /// </summary>
    public static string[] StartupFiles { get; private set; } = Array.Empty<string>();

    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        StartupFiles = e.Args ?? Array.Empty<string>();

        // Make sure user data directories exist before anything else runs.
        AppPaths.EnsureCreated();

        // We declare no StartupUri in App.xaml so we can control creation order
        // (paths + crash guard first). Create and show the main window now.
        var window = new MainWindow();
        MainWindow = window;
        window.Show();

        // Global crash guard: never lose work silently.
        DispatcherUnhandledException += (_, args) =>
        {
            try
            {
                if (MainWindow is MainWindow mw)
                    mw.FlushAllAutoSaves();
            }
            catch { /* best effort */ }

            MessageBox.Show(
                "خطایی رخ داد اما کارهای ذخیره‌نشده شما حفظ شد.\n\n" + args.Exception.Message,
                "PersiaPad", MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };
    }
}
