# Bulk Video Downloader GUI 設計書（ドラフト）

## 目的
- Windows向けのGUIアプリとして、複数URLの動画ダウンロードを簡単に操作可能にする。
- `yt-dlp.exe` を活用し、対応プロバイダの広さを維持する。

## 主要要件
- 1画面構成で操作が完結する。
- URLの入力/編集とファイル読み込みの両方に対応。
- ダウンロードの開始/停止が可能。
- 保存先、追加オプション、並列数を指定できる。
- 進捗バー表示（URL単位）。
- URLごとの状態表示（待機/実行中/完了/エラー）。
- 設定（保存先・並列数・オプション）を保存/復元する。

## UI構成（1画面）
### 上部
- URL入力テキストエリア
- 「ファイル読込」ボタン
- 「リストに追加」ボタン（テキストエリア内容をキュー化）

### 中央
- URL一覧テーブル
  - 列: URL / 状態 / 進捗(%) / 出力ファイル / エラー概要

### 右側 or 下部
- 保存先フォルダ選択
- 追加オプション入力（テキスト）
- 並列数（数値入力）
- 開始 / 停止 ボタン

### 下部
- ログ表示（簡易デバッグ用途）

## データモデル（MVVM）
### DownloadItem
- Url
- Status
- Progress
- OutputFile
- ErrorMessage
- LogTail

### DownloadQueue
- ObservableCollection<DownloadItem>

### Settings
- OutputDirectory
- AdditionalOptions
- Parallelism

## 実行制御
- `yt-dlp.exe` をURL単位で起動する。
- `ProcessStartInfo` で標準出力/標準エラーを取得する。
- `--progress-template` で進捗解析可能なフォーマットを出力させる。
- 停止は該当プロセスをKillする（安全停止は将来拡張）。

## 並列実行設計
- `SemaphoreSlim` で同時実行数を制御する。
- 逐次実行を基本とし、並列数指定で拡張する。
- UI側は常にキュー順で状態更新する。

## ログ・進捗
- URL単位の進捗バー（%）と状態更新。
- 下部ログには最新出力行を追記（全量保持はしない）。
- エラー時はstderrの一部を `ErrorMessage` に反映。

## 設定保存
- JSON形式でローカル保存する。
- 起動時に読み込み、未設定の場合はデフォルト値を使用する。

## 配布/ビルド
- .NET Publish（SingleFile, win-x64）。
- `yt-dlp.exe` を実行フォルダに同梱（更新はユーザ任意）。

---

# 実行計画
1. 既存リポジトリの構成確認と新規プロジェクト方針決定
2. Avaloniaプロジェクト初期化（MVVM構成）
3. UIレイアウト作成（1画面・一覧・設定・ログ）
4. `DownloadItem`/`Settings` モデル実装
5. `yt-dlp` 実行ラッパー実装（進捗解析＋停止）
6. 並列キュー制御（SemaphoreSlim）
7. URL読み込み機能（テキスト/ファイル）
8. 設定保存/復元（JSON等）
9. 動作確認と改善点整理
