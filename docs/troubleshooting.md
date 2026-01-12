# Troubleshooting

## 起動時にウィンドウが消える

- `%AppData%\BulkVideoDownloader\logs\app.log` を確認してください。
- `settings.json` が壊れている場合は削除して再起動します。

## ダウンロード開始で応答が遅い

- URL数が多い場合は並列数を適切に設定してください。
- `yt-dlp` のオプションが長すぎる場合は整理してください。

## 進捗が表示されない

- 一部サイトは進捗を返さない場合があります。
- `yt-dlp` のログを確認してください。

## yt-dlp が見つからない

- ダウンロード開始時に `yt-dlp.exe` を自動取得します。
- 取得に失敗する場合はネットワーク接続と `%AppData%\BulkVideoDownloader\logs\app.log` を確認してください。
- 手動で `yt-dlp.exe` を実行フォルダに置くか PATH に追加しても構いません。
