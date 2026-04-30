using BetterYoutubeDownloader.Services;

namespace BetterYoutubeDownloader;

/// <summary>
/// Hosts the top-level application window and composes the feature tabs.
/// </summary>
internal sealed class MainForm : Form
{
    private readonly TabControl _tabControl = new();
    private readonly TabPage _downloaderTab = new();
    private readonly TabPage _converterTab = new();
    private readonly TabPage _aboutTab = new();

    /// <summary>
    /// Creates the main window, assigns the embedded application icon, and wires the tabbed interface.
    /// </summary>
    public MainForm()
    {
        Text = AppText.T("AppTitle");
        Size = new Size(1200, 820);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        _tabControl.Dock = DockStyle.Fill;

        _downloaderTab.Text = AppText.T("DownloaderTab");
        _converterTab.Text = AppText.T("ConverterTab");
        _aboutTab.Text = AppText.T("AboutTab");

        var downloaderControl = new DownloaderTabControl { Dock = DockStyle.Fill };
        var converterControl = new ConverterTabControl { Dock = DockStyle.Fill };
        var aboutControl = new AboutTabControl { Dock = DockStyle.Fill };

        _downloaderTab.Controls.Add(downloaderControl);
        _converterTab.Controls.Add(converterControl);
        _aboutTab.Controls.Add(aboutControl);

        _tabControl.TabPages.Add(_downloaderTab);
        _tabControl.TabPages.Add(_converterTab);
        _tabControl.TabPages.Add(_aboutTab);
        Controls.Add(_tabControl);
    }
}
