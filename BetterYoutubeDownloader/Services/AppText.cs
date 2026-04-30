namespace BetterYoutubeDownloader.Services;

/// <summary>
/// Provides fixed English UI strings for the application.
/// </summary>
internal static class AppText
{
    private static readonly Dictionary<string, string> Strings = new()
    {
        ["AppTitle"] = "Better YouTube Downloader",
        ["DownloaderTab"] = "Downloader",
        ["ConverterTab"] = "Converter",
        ["AboutTab"] = "About",
        ["UrlPlaceholder"] = "Paste a YouTube video or Shorts URL",
        ["Search"] = "Search",
        ["Type"] = "Type",
        ["Quality"] = "Quality",
        ["Container"] = "Container",
        ["Size"] = "Size",
        ["Details"] = "Details",
        ["DownloadSelected"] = "Download Selected",
        ["DownloaderReady"] = "Enter a URL, then search for available video and audio streams.",
        ["EnterUrlFirst"] = "Enter a YouTube URL first.",
        ["SearchingStreams"] = "Searching for streams...",
        ["NoStreams"] = "No downloadable streams were found for this URL.",
        ["FoundStreams"] = "Found {0} streams. Select one and download it.",
        ["SearchFailed"] = "Search failed",
        ["VideoAudioMerged"] = "Merged video + audio",
        ["VideoAudio"] = "Video + audio",
        ["VideoOnly"] = "Video only",
        ["AudioOnly"] = "Audio only",
        ["SaveDownloadTitle"] = "Choose where to save the download",
        ["Downloading"] = "Downloading...",
        ["DownloadingVideoAudio"] = "Downloading video and audio...",
        ["DownloadingVideo"] = "Downloading video",
        ["DownloadingAudio"] = "Downloading audio",
        ["MergingVideoAudio"] = "Merging video and audio...",
        ["SavedTo"] = "Saved to {0}",
        ["DownloadFailed"] = "Download failed",
        ["MissingMergedStreams"] = "This option does not have both video and audio streams.",
        ["FfmpegMissing"] = "ffmpeg was not found. Install ffmpeg and make sure it is available on PATH.",
        ["InputFile"] = "Input file",
        ["OutputFile"] = "Output file",
        ["Browse"] = "Browse",
        ["InputPlaceholder"] = "Choose a video file to convert",
        ["OutputPlaceholder"] = "Choose where the converted file should be saved",
        ["OutputFormat"] = "Output format",
        ["Convert"] = "Convert",
        ["ConverterReady"] = "Choose a video file and output format.",
        ["ChooseVideoFile"] = "Choose a video file to convert",
        ["ChooseConvertedFile"] = "Choose where to save the converted file",
        ["VideoFiles"] = "Video files",
        ["AllFiles"] = "All files",
        ["ChooseExistingInput"] = "Choose an existing input video file.",
        ["ChooseDifferentOutput"] = "Choose a different output file path.",
        ["Converting"] = "Converting...",
        ["ConversionFailed"] = "Conversion failed",
        ["AboutAuthor"] = "Created by RocherDouteux",
        ["AboutDescription"] = "A small portable Windows tool for finding YouTube video and audio streams, downloading high-quality merged video with sound, saving audio-only streams, and converting local video files between common formats.",
        ["AboutPowered"] = "Powered by YoutubeExplode and ffmpeg.",
    };

    /// <summary>
    /// Returns a UI string by key.
    /// </summary>
    public static string T(string key, params object[] args)
    {
        var value = Strings.TryGetValue(key, out var text) ? text : key;
        return args.Length == 0 ? value : string.Format(value, args);
    }
}
