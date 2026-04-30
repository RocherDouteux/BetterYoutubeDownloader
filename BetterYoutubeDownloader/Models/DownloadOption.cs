using YoutubeExplode.Videos.Streams;

namespace BetterYoutubeDownloader.Models;

/// <summary>
/// Represents either a direct YouTube stream download or a merged video+audio download option.
/// </summary>
internal sealed record DownloadOption(
    string Quality,
    string OutputExtension,
    IStreamInfo? StreamInfo,
    IVideoStreamInfo? VideoStreamInfo,
    IStreamInfo? AudioStreamInfo)
{
    /// <summary>
    /// Gets whether this option must be produced by muxing separate video and audio streams.
    /// </summary>
    public bool IsMerged => VideoStreamInfo is not null && AudioStreamInfo is not null;

    /// <summary>
    /// Creates an option for a single directly downloadable stream.
    /// </summary>
    public static DownloadOption ForStream(string quality, IStreamInfo streamInfo)
    {
        return new DownloadOption(quality, streamInfo.Container.Name, streamInfo, null, null);
    }

    /// <summary>
    /// Creates an option for a high-quality video-only stream paired with an audio-only stream.
    /// </summary>
    public static DownloadOption ForMerged(string quality, IVideoStreamInfo videoStreamInfo, IStreamInfo audioStreamInfo)
    {
        return new DownloadOption(quality, "mkv", null, videoStreamInfo, audioStreamInfo);
    }
}
