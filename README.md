# Better YouTube Downloader

Better YouTube Downloader is a portable Windows Forms app created by RocherDouteux.

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

Development requires:

- Windows
- The .NET SDK that supports `net10.0-windows`
- `ffmpeg.exe` for merged downloads and conversion

The app looks for `ffmpeg.exe` in this order:

1. Beside `BetterYoutubeDownloader.exe`
2. On `PATH`

## Getting ffmpeg

For development, any normal ffmpeg install works as long as `ffmpeg` is available on `PATH`.

With Scoop:

```powershell
scoop install ffmpeg
```

With winget:

```powershell
winget install Gyan.FFmpeg
```

For a portable release, copy `ffmpeg.exe` next to `BetterYoutubeDownloader.exe` in the publish folder. The app will use that local copy automatically.

Example source path if ffmpeg was installed with Scoop:

```powershell
$env:USERPROFILE\scoop\apps\ffmpeg\current\bin\ffmpeg.exe
```

## Restore and Build

```powershell
dotnet build BetterYoutubeDownloader.sln
```

This restores NuGet packages and builds the Debug app.

## Run from Source

```powershell
dotnet run --project BetterYoutubeDownloader\BetterYoutubeDownloader.csproj
```

## Publish Portable Build

```powershell
dotnet publish BetterYoutubeDownloader\BetterYoutubeDownloader.csproj /p:PublishProfile=win-x64-portable
```

The portable output is written to:

```text
BetterYoutubeDownloader\bin\Release\net10.0-windows\win-x64\publish
```

The published `BetterYoutubeDownloader.exe` is self-contained, so it does not require the .NET runtime to be installed on the target machine.

## Add ffmpeg to the Portable Folder

After publishing, copy `ffmpeg.exe` into the publish folder:

```powershell
Copy-Item "$env:USERPROFILE\scoop\apps\ffmpeg\current\bin\ffmpeg.exe" `
  "BetterYoutubeDownloader\bin\Release\net10.0-windows\win-x64\publish\ffmpeg.exe" `
  -Force
```

Move the whole `publish` folder when sharing the app:

```text
publish/
  BetterYoutubeDownloader.exe
  ffmpeg.exe
```

`ffmpeg.exe` is not committed to the repository because it is a large third-party binary. It should be included only in packaged releases or copied into the publish folder locally.
