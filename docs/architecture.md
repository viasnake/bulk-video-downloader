# Architecture

## Overview

- Avalonia UI + MVVM 構成
- `yt-dlp.exe` を外部プロセスとして呼び出し
- 設定は `AppData` に JSON 保存

## Components

### Views

- `MainWindow.axaml`: 1画面 UI

### ViewModels

- `MainWindowViewModel`: UI 状態、コマンド、ログ制御
- `DownloadItemViewModel`: URL 単位の状態/進捗

### Services

- `DownloadService`: `yt-dlp` 実行と進捗解析
- `DownloadQueue`: 並列数制御と実行管理
- `SettingsService`: 設定の読み書き
- `UrlExpander`: URL 範囲展開

## Threading

- UI 更新は `UiDispatcher.Post` 経由で行う
- ログ更新は一定間隔でバッチ反映

## Data Flow

1. URL 入力 → `DownloadItemViewModel` を生成
2. `DownloadQueue` が逐次/並列で `DownloadService` を実行
3. 進捗ログを解析して UI を更新
