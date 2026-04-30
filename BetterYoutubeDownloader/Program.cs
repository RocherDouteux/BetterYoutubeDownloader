namespace BetterYoutubeDownloader;

/// <summary>
/// Application entry point for the Windows Forms client.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Initializes WinForms application settings and opens the main window.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
