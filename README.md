# NewPBL


## 🔄 実際の開発・作業フロー
【フェーズ1】作業を始める前に
1 powerShellでプロジェクトフォルダに移動します('cd /UnityProjects/NewPBL'など)
2 **git pull origin main**をし、手元のmainを更新します
3 **git checkout -b (feature/anim)** ()の中身は作業内容に合わせて変えてください
4 Unityを開いて作業してください

【フェーズ2】作業を終えたら
0 (容量の大きいファイルを追加した場合)
Get-ChildItem -Recurse -File -Exclude .git | Where-Object { $_.Length -gt 100MB } | Select-Object FullName, @{Name="Size(MB)";Expression={[math]::round($_.Length/1MB, 2)}}
をpowerShellで実行して100MGを超えるファイルがないか確認してください
→ Libraryフォルダなどgitignoreに設定したファイルのみが出てきた場合は無視でいいです
→ gitignoreに設定していないファイルがある場合は連絡して

1 **git add .**をしてください
2 **git commit -m "アニメーションを追加"** (""の中身は作業内容に合わせて)をしてください
3 **git push origin (feature/anim)** ()の中身は3で作成したブランチの名前にしてください

【フェーズ3】mainブランチに合流させます
1 mainに合流してほしい場合は連絡して

フェーズ1に戻って作業してください
