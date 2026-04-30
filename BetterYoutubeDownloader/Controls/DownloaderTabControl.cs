using BetterYoutubeDownloader.Models;
using BetterYoutubeDownloader.Services;
using BetterYoutubeDownloader.Utilities;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace BetterYoutubeDownloader;

/// <summary>
/// User interface and orchestration logic for discovering and downloading YouTube streams.
/// </summary>
internal sealed class DownloaderTabControl : UserControl
{
    private readonly YoutubeClient _youtube = new();
    private readonly TextBox _urlTextBox = new();
    private readonly Button _searchButton = new();
    private readonly Button _downloadButton = new();
    private readonly ListView _streamListView = new();
    private readonly Label _titleLabel = new();
    private readonly Label _statusLabel = new();
    private readonly ProgressBar _progressBar = new();
    private readonly List<DownloadOption> _downloadOptions = [];

    /// <summary>
    /// Creates the downloader tab and initializes its child controls.
    /// </summary>
    public DownloaderTabControl()
    {
        Controls.Add(CreateContent());
    }

    /// <summary>
    /// Builds the downloader tab layout: URL entry, stream list, action button, and progress/status region.
    /// </summary>
    private Control CreateContent()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(12),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var searchPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
        };
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _urlTextBox.Dock = DockStyle.Fill;
        _urlTextBox.PlaceholderText = T("UrlPlaceholder");
        _urlTextBox.Margin = new Padding(0, 0, 8, 8);
        _urlTextBox.KeyDown += UrlTextBoxOnKeyDown;

        _searchButton.Text = T("Search");
        _searchButton.AutoSize = true;
        _searchButton.Margin = new Padding(0, 0, 0, 8);
        _searchButton.Click += async (_, _) => await SearchAsync();

        searchPanel.Controls.Add(_urlTextBox, 0, 0);
        searchPanel.Controls.Add(_searchButton, 1, 0);

        _titleLabel.AutoSize = true;
        _titleLabel.Margin = new Padding(0, 0, 0, 8);
        _titleLabel.Font = new Font(Font, FontStyle.Bold);

        _streamListView.Dock = DockStyle.Fill;
        _streamListView.FullRowSelect = true;
        _streamListView.GridLines = true;
        _streamListView.HideSelection = false;
        _streamListView.MultiSelect = false;
        _streamListView.OwnerDraw = true;
        _streamListView.View = View.Details;
        _streamListView.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        _streamListView.DrawColumnHeader += DrawStreamListColumnHeader;
        _streamListView.DrawSubItem += DrawStreamListSubItem;
        _streamListView.Resize += (_, _) => ResizeStreamColumns();
        _streamListView.Columns.Add(T("Type"), 140);
        _streamListView.Columns.Add(T("Quality"), 160);
        _streamListView.Columns.Add(T("Container"), 100);
        _streamListView.Columns.Add(T("Size"), 120);
        _streamListView.Columns.Add(T("Details"), 340);
        ResizeStreamColumns();
        _streamListView.SelectedIndexChanged += (_, _) =>
            _downloadButton.Enabled = _streamListView.SelectedItems.Count > 0 && _searchButton.Enabled;
        _streamListView.DoubleClick += async (_, _) => await DownloadSelectedAsync();

        var actionPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 8, 0, 8),
        };

        _downloadButton.Text = T("DownloadSelected");
        _downloadButton.AutoSize = true;
        _downloadButton.Enabled = false;
        _downloadButton.Click += async (_, _) => await DownloadSelectedAsync();
        actionPanel.Controls.Add(_downloadButton);

        _progressBar.Dock = DockStyle.Top;
        _progressBar.Height = 18;

        _statusLabel.AutoSize = true;
        _statusLabel.Margin = new Padding(0, 6, 0, 0);
        _statusLabel.Text = T("DownloaderReady");

        root.Controls.Add(searchPanel, 0, 0);
        root.Controls.Add(_titleLabel, 0, 1);
        root.Controls.Add(_streamListView, 0, 2);
        root.Controls.Add(actionPanel, 0, 3);
        root.Controls.Add(_progressBar, 0, 4);
        root.Controls.Add(_statusLabel, 0, 5);

        return root;
    }

    /// <summary>
    /// Resolves the user-entered YouTube URL, loads the stream manifest, and populates all direct and merged download options.
    /// </summary>
    private async Task SearchAsync()
    {
        var input = _urlTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            MessageBox.Show(this, T("EnterUrlFirst"), T("AppTitle"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            SetBusy(true, T("SearchingStreams"));
            _downloadOptions.Clear();
            _streamListView.Items.Clear();
            _titleLabel.Text = "";
            _progressBar.Value = 0;

            var videoIdOrUrl = YouTubeUrlHelper.NormalizeVideoInput(input);
            var video = await _youtube.Videos.GetAsync(videoIdOrUrl);
            var manifest = await _youtube.Videos.Streams.GetManifestAsync(video.Id);

            _titleLabel.Text = video.Title;

            var bestAudioStream = manifest.GetAudioOnlyStreams()
                .OrderByDescending(stream => stream.Bitrate.BitsPerSecond)
                .FirstOrDefault();
            var videoOnlyStreams = manifest.GetVideoOnlyStreams()
                .OrderByDescending(GetVideoHeight)
                .ThenByDescending(stream => stream.Bitrate.BitsPerSecond)
                .ToArray();

            if (bestAudioStream is not null)
            {
                foreach (var stream in videoOnlyStreams)
                {
                    AddMergedOption(T("VideoAudioMerged"), GetQualityLabel(stream), "mkv",
                        $"{stream.Size} + {bestAudioStream.Size}",
                        $"{stream.VideoCodec}, {stream.Bitrate} + {bestAudioStream.AudioCodec}, {bestAudioStream.Bitrate}",
                        stream, bestAudioStream);
                }
            }

            foreach (var stream in manifest.GetMuxedStreams().OrderByDescending(GetVideoHeight))
            {
                AddStreamOption(T("VideoAudio"), GetQualityLabel(stream), stream.Container.Name, stream.Size.ToString(),
                    $"{stream.VideoCodec}, {stream.AudioCodec}, {stream.Bitrate}", stream);
            }

            foreach (var stream in videoOnlyStreams)
            {
                AddStreamOption(T("VideoOnly"), GetQualityLabel(stream), stream.Container.Name, stream.Size.ToString(),
                    $"{stream.VideoCodec}, {stream.Bitrate}", stream);
            }

            foreach (var stream in manifest.GetAudioOnlyStreams().OrderByDescending(stream => stream.Bitrate.BitsPerSecond))
            {
                AddStreamOption(T("AudioOnly"), stream.Bitrate.ToString(), stream.Container.Name, stream.Size.ToString(),
                    $"{stream.AudioCodec}, {stream.Bitrate}", stream);
            }

            _statusLabel.Text = _downloadOptions.Count == 0
                ? T("NoStreams")
                : T("FoundStreams", _downloadOptions.Count);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = T("SearchFailed");
            MessageBox.Show(this, ex.Message, T("SearchFailed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// Downloads the currently selected stream option, using ffmpeg when a separate video and audio stream must be merged.
    /// </summary>
    private async Task DownloadSelectedAsync()
    {
        if (_streamListView.SelectedItems.Count == 0)
        {
            return;
        }

        var selectedIndex = _streamListView.SelectedItems[0].Index;
        var option = _downloadOptions[selectedIndex];
        var defaultFileName = $"{FileNameHelper.SanitizeFileName(_titleLabel.Text)} - {FileNameHelper.SanitizeFileName(option.Quality)}.{option.OutputExtension}";

        using var saveFileDialog = new SaveFileDialog
        {
            FileName = defaultFileName,
            Filter = $"{option.OutputExtension.ToUpperInvariant()} file|*.{option.OutputExtension}|All files|*.*",
            Title = T("SaveDownloadTitle"),
        };

        if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            SetBusy(true, option.IsMerged ? T("DownloadingVideoAudio") : T("Downloading"));
            _progressBar.Value = 0;

            var progress = new Progress<double>(value =>
            {
                var percent = Math.Clamp((int)Math.Round(value * 100), 0, 100);
                _progressBar.Value = percent;
                _statusLabel.Text = $"{T("Downloading")} {percent}%";
            });

            if (option.IsMerged)
            {
                await DownloadMergedAsync(option, saveFileDialog.FileName);
            }
            else if (option.StreamInfo is not null)
            {
                await _youtube.Videos.Streams.DownloadAsync(option.StreamInfo, saveFileDialog.FileName, progress);
            }

            _progressBar.Value = 100;
            _statusLabel.Text = T("SavedTo", saveFileDialog.FileName);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = T("DownloadFailed");
            MessageBox.Show(this, ex.Message, T("DownloadFailed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// Downloads a video-only stream and an audio-only stream to temporary files, then muxes them into one output file.
    /// </summary>
    private async Task DownloadMergedAsync(DownloadOption option, string outputFilePath)
    {
        if (option.VideoStreamInfo is null || option.AudioStreamInfo is null)
        {
            throw new InvalidOperationException(T("MissingMergedStreams"));
        }

        if (!FfmpegService.IsAvailable())
        {
            throw new InvalidOperationException(T("FfmpegMissing"));
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), "BetterYoutubeDownloader", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        var videoPath = Path.Combine(tempDirectory, $"video.{option.VideoStreamInfo.Container.Name}");
        var audioPath = Path.Combine(tempDirectory, $"audio.{option.AudioStreamInfo.Container.Name}");

        try
        {
            await _youtube.Videos.Streams.DownloadAsync(option.VideoStreamInfo, videoPath,
                new Progress<double>(value => UpdateDownloadProgress(value, 0, 45, T("DownloadingVideo"))));

            await _youtube.Videos.Streams.DownloadAsync(option.AudioStreamInfo, audioPath,
                new Progress<double>(value => UpdateDownloadProgress(value, 45, 45, T("DownloadingAudio"))));

            _progressBar.Value = 92;
            _statusLabel.Text = T("MergingVideoAudio");
            await FfmpegService.MuxAsync(videoPath, audioPath, outputFilePath);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
            catch
            {
                // Temp cleanup failure should not hide a successful download.
            }
        }
    }

    /// <summary>
    /// Adds a directly downloadable stream option to the backing list and visible table.
    /// </summary>
    private void AddStreamOption(string type, string quality, string container, string size, string details, IStreamInfo streamInfo)
    {
        _downloadOptions.Add(DownloadOption.ForStream(quality, streamInfo));
        AddListItem(type, quality, container, size, details);
    }

    /// <summary>
    /// Adds a synthetic option that represents a video-only stream paired with the best audio stream.
    /// </summary>
    private void AddMergedOption(
        string type,
        string quality,
        string container,
        string size,
        string details,
        IVideoStreamInfo videoStreamInfo,
        IStreamInfo audioStreamInfo)
    {
        _downloadOptions.Add(DownloadOption.ForMerged(quality, videoStreamInfo, audioStreamInfo));
        AddListItem(type, quality, container, size, details);
    }

    /// <summary>
    /// Appends one row to the stream table.
    /// </summary>
    private void AddListItem(string type, string quality, string container, string size, string details)
    {
        var item = new ListViewItem(type);
        item.SubItems.Add(quality);
        item.SubItems.Add(container);
        item.SubItems.Add(size);
        item.SubItems.Add(details);
        _streamListView.Items.Add(item);
    }

    /// <summary>
    /// Enables or disables controls while a search or download operation is running.
    /// </summary>
    private void SetBusy(bool isBusy, string? status = null)
    {
        _urlTextBox.Enabled = !isBusy;
        _searchButton.Enabled = !isBusy;
        _downloadButton.Enabled = !isBusy && _streamListView.SelectedItems.Count > 0;
        _streamListView.Enabled = !isBusy;

        if (status is not null)
        {
            _statusLabel.Text = status;
        }
    }

    /// <summary>
    /// Maps a sub-operation's progress into the shared download progress bar.
    /// </summary>
    private void UpdateDownloadProgress(double value, int offset, int range, string label)
    {
        var percent = Math.Clamp(offset + (int)Math.Round(value * range), 0, 100);
        _progressBar.Value = percent;
        _statusLabel.Text = $"{label}... {percent}%";
    }

    /// <summary>
    /// Starts a search when the user presses Enter in the URL box.
    /// </summary>
    private void UrlTextBoxOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        _ = SearchAsync();
    }

    /// <summary>
    /// Returns the maximum vertical resolution for sorting video streams from highest to lowest.
    /// </summary>
    private static int GetVideoHeight(IVideoStreamInfo stream)
    {
        return stream.VideoQuality.MaxHeight;
    }

    /// <summary>
    /// Formats quality, resolution, and framerate for display in the stream table.
    /// </summary>
    private static string GetQualityLabel(IVideoStreamInfo stream)
    {
        return $"{stream.VideoQuality.Label} / {stream.VideoResolution.Width}x{stream.VideoResolution.Height} / {stream.VideoQuality.Framerate}fps";
    }

    /// <summary>
    /// Resizes stream columns so the table fills the available width without leaving a blank area.
    /// </summary>
    private void ResizeStreamColumns()
    {
        if (_streamListView.Columns.Count != 5 || _streamListView.ClientSize.Width <= 0)
        {
            return;
        }

        var availableWidth = Math.Max(_streamListView.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4, 500);
        var typeWidth = Math.Max(130, availableWidth * 15 / 100);
        var qualityWidth = Math.Max(180, availableWidth * 24 / 100);
        var containerWidth = Math.Max(110, availableWidth * 12 / 100);
        var sizeWidth = Math.Max(120, availableWidth * 12 / 100);
        var detailsWidth = Math.Max(260, availableWidth - typeWidth - qualityWidth - containerWidth - sizeWidth);

        _streamListView.Columns[0].Width = typeWidth;
        _streamListView.Columns[1].Width = qualityWidth;
        _streamListView.Columns[2].Width = containerWidth;
        _streamListView.Columns[3].Width = sizeWidth;
        _streamListView.Columns[4].Width = detailsWidth;
    }

    /// <summary>
    /// Draws the list header with readable dark-theme colors.
    /// </summary>
    private void DrawStreamListColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        var backColor = Color.FromArgb(248, 250, 252);
        var foreColor = Color.FromArgb(15, 23, 42);
        var borderColor = Color.FromArgb(203, 213, 225);

        using var backBrush = new SolidBrush(backColor);
        using var borderPen = new Pen(borderColor);
        e.Graphics.FillRectangle(backBrush, e.Bounds);
        e.Graphics.DrawRectangle(borderPen, e.Bounds);
        TextRenderer.DrawText(
            e.Graphics,
            e.Header?.Text ?? "",
            Font,
            new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, e.Bounds.Width - 12, e.Bounds.Height),
            foreColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }

    /// <summary>
    /// Draws stream table rows with alternating dark-theme backgrounds and selected-row contrast.
    /// </summary>
    private void DrawStreamListSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        var selected = e.Item?.Selected == true;
        var evenRow = e.ItemIndex % 2 == 0;
        var backColor = selected
            ? Color.FromArgb(191, 219, 254)
            : evenRow ? Color.White : Color.FromArgb(248, 250, 252);
        var foreColor = Color.FromArgb(15, 23, 42);
        var gridColor = Color.FromArgb(226, 232, 240);

        using var backBrush = new SolidBrush(backColor);
        using var gridPen = new Pen(gridColor);
        e.Graphics.FillRectangle(backBrush, e.Bounds);
        e.Graphics.DrawRectangle(gridPen, e.Bounds);
        TextRenderer.DrawText(
            e.Graphics,
            e.SubItem?.Text ?? "",
            Font,
            new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, e.Bounds.Width - 12, e.Bounds.Height),
            foreColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }

    private string T(string key, params object[] args)
    {
        return AppText.T(key, args);
    }
}
