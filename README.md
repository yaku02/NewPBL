# 🚀 NewPBL 開発ガイド

プロジェクトの基本的な作業フローです。作業前後の手順と Git の運用ルールをまとめています。

---

## 🔄 開発・作業フロー

### 【フェーズ 1】作業を開始する前

1. **PowerShell** でプロジェクトフォルダに移動します。
   ```powershell
   cd /UnityProjects/NewPBL
最新のソースコードを取得して main ブランチを更新します。

git pull origin main
作業用ブランチを作成して切り替えます。

git checkout -b feature/anim
💡 feature/anim の部分は作業内容に合わせて変更してください（例: feature/player-movement, fix/ui-bug など）。

Unity を起動して作業を開始します。

【フェーズ 2】作業を終了するとき
0. 巨大ファイルのチェック（容量確認）
容量の大きなファイルを追加した場合は、PowerShell で以下を実行して 100MB を超えるファイルがないか確認 してください。

Get-ChildItem -Recurse -File -Exclude .git | Where-Object { $_.Length -gt 100MB } | Select-Object FullName, @{Name="Size(MB)";Expression={[math]::round($_.Length/1MB, 2)}}
Library フォルダなど .gitignore 設定済みのファイルのみ検出された場合

👉 問題ありません。そのまま進めてください。

.gitignore に含まれていないファイルが検出された場合

👉 git add せずに、チームメンバーへ連絡してください。

1〜3. Git への記録と送信
変更内容をステージングします。

git add .
コミットします。

git commit -m "アニメーションを追加"
💡 "" の中は作業内容が伝わるメッセージに変更してください。

作成したブランチをリモートにプッシュします。

git push origin feature/anim
💡 feature/anim 部分はフェーズ 1 で作成したブランチ名に合わせて変更してください。

【フェーズ 3】main ブランチへの合流
リモートへプッシュ完了後、main ブランチに合流させるため 担当者へ連絡 してください。

合流連絡が終わったら、必要に応じて【フェーズ 1】に戻って次の作業を進めます。
