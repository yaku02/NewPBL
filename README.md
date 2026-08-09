# NewPBL

## 🛠 開発ルール
`main` ブランチへの直接コミット・Pushは禁止です。  
作業（機能追加・バグ修正など）を行う際は、**必ず新しいブランチを作成（ブランチを切る）** して作業してください。

---

## 🌿 開発手順

作業を開始するときは、必ず最新の `main` からブランチを作成してください。

### 1. GitHub Desktop を使う場合

1. **ブランチの作成**
   - 上部メニューの **Current Branch** をクリックします。
   - **New Branch** ボタンをクリックし、ブランチ名（例: `feature/player-move` や `fix/ui-bug`）を入力して作成します。
2. **作業・コミット**
   - Unityで作業を行います。
   - GitHub Desktopに戻り、左下の **Summary** に変更内容（例: `プレイヤーの移動処理を追加`）を入力して **Commit to [ブランチ名]** を押します。
3. **GitHubへPush & プルリクエスト作成**
   - **Publish branch** をクリックしてGitHubへ送信します。
   - **Create Pull Request** をクリックするとブラウザが開くので、`main` 宛てにプルリクエストを作成します。

---

### 2. コマンドライン（PowerShell / Git Bash）を使う場合

1. **最新の main から新しいブランチを作成**
   ```bash
   # mainブランチを最新にする
   git checkout main
   git pull origin main

   # ブランチを作成して切り替える（ブランチ名は作業内容に合わせて変更）
   git checkout -b feature/your-feature-name
