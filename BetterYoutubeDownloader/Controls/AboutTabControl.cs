using BetterYoutubeDownloader.Services;

namespace BetterYoutubeDownloader;

/// <summary>
/// Displays ownership and high-level application information.
/// </summary>
internal sealed class AboutTabControl : UserControl
{
    private readonly Label _titleLabel = new();
    private readonly Label _authorLabel = new();
    private readonly Label _descriptionLabel = new();
    private readonly Label _detailsLabel = new();

    /// <summary>
    /// Creates the about view shown in the main tab control.
    /// </summary>
    public AboutTabControl()
    {
        Controls.Add(CreateContent());
    }

    /// <summary>
    /// Builds the static about layout.
    /// </summary>
    private Control CreateContent()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(24),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _titleLabel.AutoSize = true;
        _titleLabel.Font = new Font(Font.FontFamily, 18, FontStyle.Bold);
        _titleLabel.Margin = new Padding(0, 0, 0, 8);
        _titleLabel.Text = AppText.T("AppTitle");

        _authorLabel.AutoSize = true;
        _authorLabel.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
        _authorLabel.Margin = new Padding(0, 0, 0, 16);
        _authorLabel.Text = AppText.T("AboutAuthor");

        _descriptionLabel.AutoSize = false;
        _descriptionLabel.Dock = DockStyle.Top;
        _descriptionLabel.Height = 110;
        _descriptionLabel.Margin = new Padding(0, 0, 0, 16);
        _descriptionLabel.Text = AppText.T("AboutDescription");

        _detailsLabel.AutoSize = true;
        _detailsLabel.Text = AppText.T("AboutPowered");

        root.Controls.Add(_titleLabel, 0, 0);
        root.Controls.Add(_authorLabel, 0, 1);
        root.Controls.Add(_descriptionLabel, 0, 2);
        root.Controls.Add(_detailsLabel, 0, 3);

        return root;
    }
}
