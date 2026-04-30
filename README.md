# Better YouTube Downloader

Better YouTube Downloader is a portable Windows Forms app created for RocherDouteux.

It can inspect YouTube video and Shorts URLs, list available video/audio streams, download audio-only streams, download high-quality merged video with audio, and convert local video files between common formats.

## Features

- Search YouTube videos and Shorts URLs.
- List direct video+audio, video-only, audio-only, and merged video+audio options.
- Merge high-quality video-only streams with the best audio-only stream using ffmpeg.
- Convert local videos to `mp4`, `mkv`, `webm`, `mov`, or `avi`.
- Publish as a self-contained Windows x64 executable.

## Project Structure

- `Program.cs`: WinForms entry point.
- `MainForm.cs`: Main window and tab composition.
- `Controls/DownloaderTabControl.cs`: YouTube search, stream listing, and download workflow.
- `Controls/ConverterTabControl.cs`: Local video conversion workflow.
- `Controls/AboutTabControl.cs`: App ownership and description tab.
- `Models/DownloadOption.cs`: Download option model for direct and merged streams.
- `Services/FfmpegService.cs`: ffmpeg availability checks and process execution.
- `Utilities/`: Small helpers for filenames, URL normalization, and shared UI layout.
- `Assets/app-icon.ico`: Embedded Windows application icon.

## Requirements

Development requires the .NET SDK that supports `net10.0-windows`.

Runtime conversion and merged downloads require `ffmpeg.exe`. The portable publish folder includes a sidecar `ffmpeg.exe`; during development the app also falls back to `ffmpeg` from `PATH`.

## Build

```powershell
dotnet build BetterYoutubeDownloader.sln
```

## Publish Portable Build

```powershell
dotnet publish BetterYoutubeDownloader\BetterYoutubeDownloader.csproj /p:PublishProfile=win-x64-portable
```

The portable output is written to:

```text
BetterYoutubeDownloader\bin\Release\net10.0-windows\win-x64\publish
```

Move the whole `publish` folder when sharing the app so `ffmpeg.exe` stays beside `BetterYoutubeDownloader.exe`.
