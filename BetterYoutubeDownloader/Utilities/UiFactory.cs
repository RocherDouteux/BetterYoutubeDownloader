namespace BetterYoutubeDownloader.Utilities;

/// <summary>
/// Creates small reusable WinForms layout fragments.
/// </summary>
internal static class UiFactory
{
    /// <summary>
    /// Builds a standard label/textbox/browse-button row used by file picker forms.
    /// </summary>
    public static Control CreateFilePickerRow(string labelText, TextBox textBox, Button button)
    {
        return CreateFilePickerRow(new Label { Text = labelText }, textBox, button);
    }

    /// <summary>
    /// Builds a standard label/textbox/browse-button row using an existing label instance.
    /// </summary>
    public static Control CreateFilePickerRow(Label label, TextBox textBox, Button button)
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 3,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 8),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        label.AutoSize = true;
        label.Margin = new Padding(0, 7, 8, 0);
        label.MaximumSize = new Size(120, 0);
        panel.Controls.Add(label, 0, 0);

        textBox.Dock = DockStyle.Fill;
        textBox.Margin = new Padding(0, 0, 8, 0);
        panel.Controls.Add(textBox, 1, 0);

        button.Text = "Browse";
        button.AutoSize = true;
        panel.Controls.Add(button, 2, 0);

        return panel;
    }
}
