namespace BetterYoutubeDownloader.Utilities;

/// <summary>
/// Provides filesystem-safe filename helpers.
/// </summary>
internal static class FileNameHelper
{
    /// <summary>
    /// Removes invalid filename characters from a suggested download filename.
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }
}
