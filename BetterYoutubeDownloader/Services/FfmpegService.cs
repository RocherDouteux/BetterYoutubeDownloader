using System.Collections.ObjectModel;
using System.Diagnostics;

namespace BetterYoutubeDownloader.Services;

/// <summary>
/// Wraps ffmpeg process execution for availability checks, muxing, and conversion.
/// </summary>
internal static class FfmpegService
{
    /// <summary>
    /// Checks whether ffmpeg can be launched either from the portable app folder or from PATH.
    /// </summary>
    public static bool IsAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = ResolveExecutablePath(),
                ArgumentList = { "-version" },
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            });

            if (process is null)
            {
                return false;
            }

            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Combines a separate video stream and audio stream into one output file without re-encoding.
    /// </summary>
    public static async Task MuxAsync(string videoPath, string audioPath, string outputFilePath)
    {
        var processStartInfo = CreateBaseStartInfo();
        processStartInfo.ArgumentList.Add("-y");
        processStartInfo.ArgumentList.Add("-i");
        processStartInfo.ArgumentList.Add(videoPath);
        processStartInfo.ArgumentList.Add("-i");
        processStartInfo.ArgumentList.Add(audioPath);
        processStartInfo.ArgumentList.Add("-c");
        processStartInfo.ArgumentList.Add("copy");
        processStartInfo.ArgumentList.Add(outputFilePath);

        await RunAsync(processStartInfo);
    }

    /// <summary>
    /// Converts a local media file to the selected output container using codec settings appropriate for that container.
    /// </summary>
    public static async Task ConvertAsync(string inputFilePath, string outputFilePath, string extension)
    {
        var processStartInfo = CreateBaseStartInfo();
        processStartInfo.ArgumentList.Add("-y");
        processStartInfo.ArgumentList.Add("-i");
        processStartInfo.ArgumentList.Add(inputFilePath);
        AddConversionArguments(processStartInfo.ArgumentList, extension);
        processStartInfo.ArgumentList.Add(outputFilePath);

        await RunAsync(processStartInfo);
    }

    /// <summary>
    /// Creates the shared ffmpeg process configuration.
    /// </summary>
    private static ProcessStartInfo CreateBaseStartInfo()
    {
        return new ProcessStartInfo
        {
            FileName = ResolveExecutablePath(),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
    }

    /// <summary>
    /// Prefers an ffmpeg.exe deployed beside the app, falling back to PATH for development machines.
    /// </summary>
    private static string ResolveExecutablePath()
    {
        var localFfmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        return File.Exists(localFfmpegPath) ? localFfmpegPath : "ffmpeg";
    }

    /// <summary>
    /// Runs ffmpeg and throws an exception containing stderr/stdout when the process fails.
    /// </summary>
    private static async Task RunAsync(ProcessStartInfo processStartInfo)
    {
        using var process = Process.Start(processStartInfo)
            ?? throw new InvalidOperationException("Failed to start ffmpeg.");

        var standardError = await process.StandardError.ReadToEndAsync();
        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var output = string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError;
            throw new InvalidOperationException($"ffmpeg failed with exit code {process.ExitCode}.{Environment.NewLine}{output}");
        }
    }

    /// <summary>
    /// Adds codec arguments for the selected output format.
    /// </summary>
    private static void AddConversionArguments(Collection<string> arguments, string extension)
    {
        switch (extension)
        {
            case "mp4":
                arguments.Add("-c:v");
                arguments.Add("libx264");
                arguments.Add("-preset");
                arguments.Add("medium");
                arguments.Add("-crf");
                arguments.Add("20");
                arguments.Add("-c:a");
                arguments.Add("aac");
                arguments.Add("-b:a");
                arguments.Add("192k");
                arguments.Add("-movflags");
                arguments.Add("+faststart");
                break;
            case "mkv":
                arguments.Add("-c");
                arguments.Add("copy");
                break;
            case "webm":
                arguments.Add("-c:v");
                arguments.Add("libvpx-vp9");
                arguments.Add("-crf");
                arguments.Add("32");
                arguments.Add("-b:v");
                arguments.Add("0");
                arguments.Add("-c:a");
                arguments.Add("libopus");
                arguments.Add("-b:a");
                arguments.Add("128k");
                break;
            case "mov":
                arguments.Add("-c:v");
                arguments.Add("libx264");
                arguments.Add("-preset");
                arguments.Add("medium");
                arguments.Add("-crf");
                arguments.Add("20");
                arguments.Add("-c:a");
                arguments.Add("aac");
                arguments.Add("-b:a");
                arguments.Add("192k");
                break;
            case "avi":
                arguments.Add("-c:v");
                arguments.Add("mpeg4");
                arguments.Add("-q:v");
                arguments.Add("4");
                arguments.Add("-c:a");
                arguments.Add("mp3");
                arguments.Add("-b:a");
                arguments.Add("192k");
                break;
            default:
                throw new InvalidOperationException($"Unsupported output format: {extension}");
        }
    }
}
