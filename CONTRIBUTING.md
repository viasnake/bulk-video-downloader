# Contributing

## 開発環境
- Windows 10/11 推奨
- .NET 10 SDK
- mise

### セットアップ
```bash
mise install
```

### ビルド
```bash
mise exec -- dotnet build BulkVideoDownloader.sln
```

## 変更方針
- UIの変更はAvaloniaのMVVM構成に従ってください。
- ネットワークやプロセス制御は `Services` に集約してください。
- 変更前後でクラッシュしないことを確認してください。

## ブランチ運用
- 変更はfeatureブランチで作業し、PRを作成してください。
- PRには実行したテストコマンドを記載してください。

## コーディング規約
- `INotifyPropertyChanged` を使ったUI更新はUIスレッドで行ってください。
- 例外を握りつぶす場合はログに理由を残してください。

## テスト
- 最低限 `dotnet build` を通してください。
