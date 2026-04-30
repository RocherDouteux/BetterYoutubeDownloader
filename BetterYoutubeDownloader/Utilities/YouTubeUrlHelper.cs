namespace BetterYoutubeDownloader.Utilities;

/// <summary>
/// Provides YouTube URL normalization helpers.
/// </summary>
internal static class YouTubeUrlHelper
{
    /// <summary>
    /// Converts supported URL shapes, including Shorts and youtu.be links, into input accepted by YoutubeExplode.
    /// </summary>
    public static string NormalizeVideoInput(string input)
    {
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            return input;
        }

        var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var shortsIndex = Array.FindIndex(pathParts, part => part.Equals("shorts", StringComparison.OrdinalIgnoreCase));
        if (shortsIndex >= 0 && shortsIndex + 1 < pathParts.Length)
        {
            return pathParts[shortsIndex + 1];
        }

        if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase) && pathParts.Length > 0)
        {
            return pathParts[0];
        }

        return input;
    }
}
