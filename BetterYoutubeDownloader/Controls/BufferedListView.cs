namespace BetterYoutubeDownloader;

/// <summary>
/// ListView with double buffering enabled to reduce flicker and repaint cost during resize.
/// </summary>
internal sealed class BufferedListView : ListView
{
    /// <summary>
    /// Creates a double-buffered ListView.
    /// </summary>
    public BufferedListView()
    {
        DoubleBuffered = true;
    }
}
