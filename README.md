# Bulk Video Downloader

Windows向けのGUIアプリとして、複数URLの動画ダウンロードを一括操作するためのツールです。GUIはAvalonia UIで構築し、`yt-dlp` を利用してさまざまなプロバイダに対応します。

## Features
- URL入力/編集とファイル読み込み
- URLごとの状態/進捗表示
- 保存先・追加オプション・並列数指定
- ダウンロード開始/停止
- 設定の自動保存

## Requirements
- Windows 10/11
- `yt-dlp.exe` (実行フォルダ同梱、またはPATHに追加)
- .NET 8 SDK (miseで管理)

## Setup (mise)
```bash
mise install
```

## Build
```bash
mise exec -- dotnet build BulkVideoDownloader.sln
```

## Run
```bash
mise exec -- dotnet run --project BulkVideoDownloader/BulkVideoDownloader.csproj
```

## Configuration
- 設定ファイル: `%AppData%\BulkVideoDownloader\settings.json`
- ログファイル: `%AppData%\BulkVideoDownloader\logs\app.log`

## Documentation
- 使い方: `docs/usage.md`
- アーキテクチャ: `docs/architecture.md`
- トラブルシュート: `docs/troubleshooting.md`

## Notes
- Linux/macOS上でGUIを起動するにはディスプレイ環境が必要です。
- `yt-dlp` のオプションは「追加オプション」欄にそのまま指定します。
