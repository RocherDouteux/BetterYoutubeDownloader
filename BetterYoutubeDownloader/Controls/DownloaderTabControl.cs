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
    private readonly ListView _streamListView = new BufferedListView();
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
        _streamListView.View = View.Details;
        _streamListView.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        _streamListView.Columns.Add(T("Type"), 140);
        _streamListView.Columns.Add(T("Quality"), 160);
        _streamListView.Columns.Add(T("Container"), 100);
        _streamListView.Columns.Add(T("Size"), 120);
        AutoSizeStreamColumns();
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
                        $"{stream.Size} + {bestAudioStream.Size}", stream, bestAudioStream);
                }
            }

            foreach (var stream in manifest.GetMuxedStreams().OrderByDescending(GetVideoHeight))
            {
                AddStreamOption(T("VideoAudio"), GetQualityLabel(stream), stream.Container.Name, stream.Size.ToString(), stream);
            }

            foreach (var stream in videoOnlyStreams)
            {
                AddStreamOption(T("VideoOnly"), GetQualityLabel(stream), stream.Container.Name, stream.Size.ToString(), stream);
            }

            foreach (var stream in manifest.GetAudioOnlyStreams().OrderByDescending(stream => stream.Bitrate.BitsPerSecond))
            {
                AddStreamOption(T("AudioOnly"), stream.Bitrate.ToString(), stream.Container.Name, stream.Size.ToString(), stream);
            }

            _statusLabel.Text = _downloadOptions.Count == 0
                ? T("NoStreams")
                : T("FoundStreams", _downloadOptions.Count);
            AutoSizeStreamColumns();
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
    private void AddStreamOption(string type, string quality, string container, string size, IStreamInfo streamInfo)
    {
        _downloadOptions.Add(DownloadOption.ForStream(quality, streamInfo));
        AddListItem(type, quality, container, size);
    }

    /// <summary>
    /// Adds a synthetic option that represents a video-only stream paired with the best audio stream.
    /// </summary>
    private void AddMergedOption(
        string type,
        string quality,
        string container,
        string size,
        IVideoStreamInfo videoStreamInfo,
        IStreamInfo audioStreamInfo)
    {
        _downloadOptions.Add(DownloadOption.ForMerged(quality, videoStreamInfo, audioStreamInfo));
        AddListItem(type, quality, container, size);
    }

    /// <summary>
    /// Appends one row to the stream table.
    /// </summary>
    private void AddListItem(string type, string quality, string container, string size)
    {
        var item = new ListViewItem(type);
        item.SubItems.Add(quality);
        item.SubItems.Add(container);
        item.SubItems.Add(size);
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
    /// Resizes each stream column to fit the widest header or cell value in that column.
    /// </summary>
    private void AutoSizeStreamColumns()
    {
        if (_streamListView.Columns.Count == 0)
        {
            return;
        }

        _streamListView.BeginUpdate();
        try
        {
            using var graphics = _streamListView.CreateGraphics();
            for (var columnIndex = 0; columnIndex < _streamListView.Columns.Count; columnIndex++)
            {
                var width = TextRenderer.MeasureText(
                    graphics,
                    _streamListView.Columns[columnIndex].Text,
                    _streamListView.Font).Width;

                foreach (ListViewItem item in _streamListView.Items)
                {
                    if (item.SubItems.Count <= columnIndex)
                    {
                        continue;
                    }

                    width = Math.Max(width, TextRenderer.MeasureText(
                        graphics,
                        item.SubItems[columnIndex].Text,
                        _streamListView.Font).Width);
                }

                _streamListView.Columns[columnIndex].Width = width + 28;
            }
        }
        finally
        {
            _streamListView.EndUpdate();
        }
    }

    private string T(string key, params object[] args)
    {
        return AppText.T(key, args);
    }

}
