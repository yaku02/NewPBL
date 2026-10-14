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



## ⚓ Unity 側での開発方法

# 🔍 EvidenceObject の使い方ガイド

`EvidenceObject` は、ゲーム内の**証拠品の取得・表示・合成・条件判定**を一括管理するコンポーネントです。

---

## 📌 概要・できること
1. **インタラクトによる入手**：プレイヤーが近づいてキーを押すことで入手可能
2. **入手条件の設定**：特定のアイテムを事前に所持・使用している場合のみ取得可能
3. **UI表示（詳細・アイコン）**：手に入れたアイテムの画像（Sprite）や説明文を保持
4. **証拠品どうしの合成**：特定の組み合わせで新しい証拠品へ変化
5. **入手失敗時の演出**：条件を満たしていない場合に失敗音や Canvas を表示

---

## 🛠 インスペクター設定項目の一覧

GameObject に `EvidenceObject` をアタッチした際の設定手順です。

### 1. 証拠品情報（基本設定）
* **Description**：内部的な説明文やデバッグテキスト（AIに送る文章でもいいかも）
* **UIdescription**：詳細モーダル（UI）に表示されるプレイヤー向けの説明文
* **Evidence Icon**：インベントリUIや詳細画面で表示する画像（PNG等の `Sprite` アセット）

### 2. 入手失敗時
* **Failed Canvas**：条件を満たさずにインタラクトした際（または範囲内にいる際）に表示するUI Canvas
* **Failed Sound**：失敗時に再生する効果音（未設定の場合は `SEManager` のデフォルトエラー音が再生されます）

### 3. ⚠️ 参照（Reference）の必須・任意チェックリスト

インスペクターで設定する各種コンポーネントや GameObject の参照一覧です。

#### 🔴 必須（設定しないとエラー・正常動作しないもの）
| 設定項目 | 分類 | 理由・動作 |
| :--- | :--- | :--- |
| **Player Camera** | Transform | プレイヤーとの距離計算に使用します。未設定だとエラーになります。<br>*(※ ただし `Is Combine Result` が `true` の合成用アイテムは距離計算を行わないため不要です)* |

> 💡 **`Input Command` について**  
> スクリプト開始時（`Start()`）に `FindFirstObjectByType<InputCommand>()` でシーン内から自動検索されるため、インスペクターで未設定でも自動取得されます。（シーン内にアタッチされたオブジェクトが存在しない場合はエラーになります）

---

#### 🟡 任意（空欄 / None でも動作するもの）
設定しなくてもエラーは出ず、該当機能や演出がスキップ（または自動処理）されます。

| 設定項目 | 分類 | 設定しない場合の挙動 |
| :--- | :--- | :--- |
| **Evidence Icon** | Sprite | 詳細UIを開いた際に画像が表示されなくなります（枠ごと非表示）。 |
| **Interact Canvas** | GameObject | 接近時の「[E] 調べる」などの案内UIが表示されなくなります。 |
| **Result Canvas** | GameObject | 入手成功時の獲得ダイアログUIが表示されなくなります。 |
| **Failed Canvas** | GameObject | 条件未達成で調べた際のエラー用UIが表示されなくなります。 |
| **Failed Sound** | AudioClip | 失敗時の効果音が `SEManager` のデフォルトエラー音（`PlayErrorSound`）に自動で差し替わります。 |
| **Target Particle** | ParticleSystem | 証拠品の上に浮かぶ光やエフェクトが表示されなくなります。 |
| **Required Evidences** | List | 入手条件（必要なアイテム）がない「誰でも即拾えるアイテム」になります。 |
| **Combine Ingredient / Result**| EvidenceObject | 他の証拠品との「合成機能」を行わないアイテムになります。 |

### 4. 入手条件設定
* **Required Evidences** (List)：この証拠品を入手するために**必要な他の証拠品**
  * 指定した全ての証拠品を**「所持していること」**かつ**「モーダル画面から『使用』されたこと」**が入手条件になります。

### 5. 合成設定（クラフト・組み合わせ）
* **Is Combine Result** (bool)：
  * **`false`（通常アイテム）**：マップ上に配置され、プレイヤーが移動してインタラクトします。
  * **`true`（合成専用アイテム）**：マップ上に配置せず、合成成功時のみ出現させたいアイテムにチェックを入れます（距離判定を行わなくなります）。
* **Combine Ingredient**：このアイテムと組み合わせる**相手の証拠品**
* **Combine Result**：組み合わせに成功した際に手に入る**新しい証拠品**

---

## 💡 パターン別・作成例

### パターン A：マップ上でそのまま拾える通常アイテム
1. 空の GameObject に `EvidenceObject` をアタッチする。
2. `UIdescription` と `Evidence Icon` を設定。
3. `Interact Canvas` と `Result Canvas` を割り当てる。
4. `Required Evidences` や合成項目は **空欄** のままにする。

### パターン B：鍵（アイテムA）を使わないと手に入らない証拠品（アイテムB）
1. アイテムB（`EvidenceObject`）の `Required Evidences` に **アイテムA** を追加する。
2. プレイヤーがアイテムAを所持し、UIの詳細画面から「使用」ボタンを押すと、アイテムBのインタラクトロックが解除されて入手可能になります。

### パターン C：2つの証拠品を合成して新しい証拠品を作る
例：`ちぎれた手紙A` + `ちぎれた手紙B` ＝ `完成した手紙`

1. **`完成した手紙`** を作成：
   * `Is Combine Result` に **チェックを入れる (`true`)**
2. **`ちぎれた手紙A`**（またはBのどちらか片方）に設定：
   * `Combine Ingredient` ➔ **`ちぎれた手紙B`**
   * `Combine Result` ➔ **`完成した手紙`**
3. インベントリUIで2つを選択して「組み合わせる」を実行すると、元のアセット2つが削除され `完成した手紙` が生成されます。

---

## ⚙️ 他のスクリプトとの連携・仕組み

* **`EvidenceManager`**：
  * 所持している証拠品の一覧（`CollectedEvidences`）を保持します。
  * `Interact()` 成功時に `EvidenceManager.Instance.AddEvidence(this)` で自動追加されます。
  * 合成成功時は、素材となったアイテムをリストから削除し、結果のアイテムを追加します。

* **`NyonUI`**（演出アニメーション）：
  * `Result Canvas` や `Failed Canvas` に `NyonUI` コンポーネントがついている場合、表示時に自動で `.Show()` アニメーションが呼び出されます。

## 🛠 よく使う Git コマンド集

作業中に困ったときや、状態を確認したいときに使う便利なコマンド一覧です。

### 📊 状態確認・履歴
* **現在の状態を確認する**（変更されたファイルやブランチの確認）
  git status
  
コミット履歴を確認する（1行でコンパクトに表示）
git log --oneline

🌿 ブランチ操作
ローカルブランチの一覧を確認する
git branch

既存のブランチに切り替える
git checkout <ブランチ名>

ブランチを新規作成して切り替える
git checkout -b <ブランチ名>

ブランチを削除する（対象のブランチから抜けて行ってください）
git branch -d <ブランチ名>
