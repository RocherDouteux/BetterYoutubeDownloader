using BetterYoutubeDownloader.Services;
using BetterYoutubeDownloader.Utilities;

namespace BetterYoutubeDownloader;

/// <summary>
/// User interface and orchestration logic for converting local video files with ffmpeg.
/// </summary>
internal sealed class ConverterTabControl : UserControl
{
    private readonly Label _inputLabel = new();
    private readonly Label _outputLabel = new();
    private readonly Label _formatLabel = new();
    private readonly TextBox _inputTextBox = new();
    private readonly TextBox _outputTextBox = new();
    private readonly ComboBox _formatComboBox = new();
    private readonly Button _browseInputButton = new();
    private readonly Button _browseOutputButton = new();
    private readonly Button _convertButton = new();
    private readonly Label _statusLabel = new();
    private readonly ProgressBar _progressBar = new();

    /// <summary>
    /// Creates the converter tab and initializes its child controls.
    /// </summary>
    public ConverterTabControl()
    {
        Controls.Add(CreateContent());
    }

    /// <summary>
    /// Builds the converter tab layout: input picker, output picker, format selector, action button, and status controls.
    /// </summary>
    private Control CreateContent()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var inputPanel = UiFactory.CreateFilePickerRow(_inputLabel, _inputTextBox, _browseInputButton);
        _inputLabel.Text = T("InputFile");
        _inputTextBox.PlaceholderText = T("InputPlaceholder");
        _browseInputButton.Text = T("Browse");
        _browseInputButton.Click += (_, _) => BrowseInputFile();

        var outputPanel = UiFactory.CreateFilePickerRow(_outputLabel, _outputTextBox, _browseOutputButton);
        _outputLabel.Text = T("OutputFile");
        _outputTextBox.PlaceholderText = T("OutputPlaceholder");
        _browseOutputButton.Text = T("Browse");
        _browseOutputButton.Click += (_, _) => BrowseOutputFile();

        var formatPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, 8),
        };
        _formatLabel.AutoSize = true;
        _formatLabel.Margin = new Padding(0, 7, 8, 0);
        _formatLabel.MinimumSize = new Size(160, 0);
        _formatLabel.Text = T("OutputFormat");
        formatPanel.Controls.Add(_formatLabel);

        _formatComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _formatComboBox.Width = 120;
        _formatComboBox.Items.AddRange(["mp4", "mkv", "webm", "mov", "avi"]);
        _formatComboBox.SelectedIndex = 0;
        _formatComboBox.SelectedIndexChanged += (_, _) => UpdateOutputExtension();
        formatPanel.Controls.Add(_formatComboBox);

        _convertButton.Text = T("Convert");
        _convertButton.AutoSize = true;
        _convertButton.Margin = new Padding(12, 0, 0, 0);
        _convertButton.Click += async (_, _) => await ConvertVideoAsync();
        formatPanel.Controls.Add(_convertButton);

        _progressBar.Dock = DockStyle.Top;
        _progressBar.Height = 18;

        _statusLabel.AutoSize = true;
        _statusLabel.Margin = new Padding(0, 6, 0, 0);
        _statusLabel.Text = T("ConverterReady");
        root.Controls.Add(inputPanel, 0, 0);
        root.Controls.Add(outputPanel, 0, 1);
        root.Controls.Add(formatPanel, 0, 2);
        root.Controls.Add(_progressBar, 0, 3);
        root.Controls.Add(_statusLabel, 0, 4);

        return root;
    }

    /// <summary>
    /// Opens a file picker for the source video and proposes a default output path.
    /// </summary>
    private void BrowseInputFile()
    {
        using var openFileDialog = new OpenFileDialog
        {
            Filter = $"{T("VideoFiles")}|*.mp4;*.mkv;*.webm;*.mov;*.avi;*.wmv;*.flv;*.m4v|{T("AllFiles")}|*.*",
            Title = T("ChooseVideoFile"),
        };

        if (openFileDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _inputTextBox.Text = openFileDialog.FileName;

        if (string.IsNullOrWhiteSpace(_outputTextBox.Text))
        {
            _outputTextBox.Text = GetDefaultOutputFileName(GetSelectedExtension());
        }
        else
        {
            UpdateOutputExtension();
        }
    }

    /// <summary>
    /// Opens a save dialog for the converted video path.
    /// </summary>
    private void BrowseOutputFile()
    {
        var extension = GetSelectedExtension();
        using var saveFileDialog = new SaveFileDialog
        {
            Filter = $"{extension.ToUpperInvariant()} file|*.{extension}|All files|*.*",
            Title = T("ChooseConvertedFile"),
            FileName = GetDefaultOutputFileName(extension),
        };

        if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
        {
            _outputTextBox.Text = saveFileDialog.FileName;
        }
    }

    /// <summary>
    /// Validates converter inputs and starts the ffmpeg conversion.
    /// </summary>
    private async Task ConvertVideoAsync()
    {
        var inputFilePath = _inputTextBox.Text.Trim();
        var outputFilePath = _outputTextBox.Text.Trim();
        var extension = GetSelectedExtension();

        if (!File.Exists(inputFilePath))
        {
            MessageBox.Show(this, T("ChooseExistingInput"), T("AppTitle"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFilePath))
        {
            outputFilePath = GetDefaultOutputFileName(extension);
            _outputTextBox.Text = outputFilePath;
        }

        if (string.Equals(Path.GetFullPath(inputFilePath), Path.GetFullPath(outputFilePath), StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, T("ChooseDifferentOutput"), T("AppTitle"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!FfmpegService.IsAvailable())
        {
            MessageBox.Show(this, T("FfmpegMissing"), T("AppTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            SetBusy(true, T("Converting"));
            await FfmpegService.ConvertAsync(inputFilePath, outputFilePath, extension);
            _progressBar.Style = ProgressBarStyle.Blocks;
            _progressBar.Value = 100;
            _statusLabel.Text = T("SavedTo", outputFilePath);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = T("ConversionFailed");
            MessageBox.Show(this, ex.Message, T("ConversionFailed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// Keeps the output filename extension aligned with the selected target container.
    /// </summary>
    private void UpdateOutputExtension()
    {
        if (string.IsNullOrWhiteSpace(_outputTextBox.Text))
        {
            return;
        }

        var extension = GetSelectedExtension();
        var directory = Path.GetDirectoryName(_outputTextBox.Text) ?? "";
        var fileName = Path.GetFileNameWithoutExtension(_outputTextBox.Text);
        _outputTextBox.Text = Path.Combine(directory, $"{fileName}.{extension}");
    }

    /// <summary>
    /// Creates a default output filename beside the input file when possible.
    /// </summary>
    private string GetDefaultOutputFileName(string extension)
    {
        if (!string.IsNullOrWhiteSpace(_inputTextBox.Text))
        {
            var directory = Path.GetDirectoryName(_inputTextBox.Text) ?? "";
            var fileName = Path.GetFileNameWithoutExtension(_inputTextBox.Text);
            return Path.Combine(directory, $"{fileName}-converted.{extension}");
        }

        return $"converted-video.{extension}";
    }

    /// <summary>
    /// Returns the selected output container extension.
    /// </summary>
    private string GetSelectedExtension()
    {
        return (_formatComboBox.SelectedItem as string) ?? "mp4";
    }

    /// <summary>
    /// Enables or disables controls while conversion is running.
    /// </summary>
    private void SetBusy(bool isBusy, string? status = null)
    {
        _inputTextBox.Enabled = !isBusy;
        _outputTextBox.Enabled = !isBusy;
        _formatComboBox.Enabled = !isBusy;
        _browseInputButton.Enabled = !isBusy;
        _browseOutputButton.Enabled = !isBusy;
        _convertButton.Enabled = !isBusy;
        _progressBar.Style = isBusy ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;

        if (!isBusy && _progressBar.Value != 100)
        {
            _progressBar.Value = 0;
        }

        if (status is not null)
        {
            _statusLabel.Text = status;
        }
    }

    private string T(string key, params object[] args)
    {
        return AppText.T(key, args);
    }
}
