# Antigravity へ — 引き継ぎメモ

**作成者:** ClaudeCode  
**日付:** 2026-05-30

---

## 君に頼みたいこと

**mcp-unity を使って Unity の GameScene を組み立ててほしい。**  
コードは全部揃っている。シーンの構築（GameObject配置・参照アサイン）だけが残っている。

---

## プロジェクトの場所

```
Unity プロジェクト: C:/GitHub/SortGems/SortGemsUnity/
詳細手順書:        C:/GitHub/SortGems/SCENE_SETUP.md   ← 必読
仕様書:            C:/GitHub/SortGems/SPEC.md
全体状況:          C:/GitHub/SortGems/AGENT_STATUS.md
```

---

## 実装済みスクリプト（全部コンパイル通過済み）

```
Assets/Scripts/Core/
├── GemColor.cs         — 色の列挙型
├── GemColorPalette.cs  — 色→UnityColor変換
├── GemCell.cs          — グリッド1マスのデータ
├── GemGroup.cs         — 同色グループ
├── StageData.cs        — ステージ定義 ScriptableObject
├── GridManager.cs      — ロジック全般（移動・判定・Undo）
├── GemCellView.cs      — 1マスの表示・アニメ（Coroutine版、LeanTween不要）
└── GridView.cs         — ビュー全体制御

Assets/Scripts/UI/
├── GameUI.cs           — タイマー/ボタン/クリア・失敗パネル（UI.Text版、TMP不要）
└── GameBootstrap.cs    — 起動エントリーポイント

Assets/Scripts/Ads/
└── AdManager.cs        — AdMobスタブ（SDK未導入でもビルドOK）
```

---

## シーン構築でやること（SCENE_SETUP.md の要約）

### 1. GameScene.unity を作成
`File > New Scene` → `Assets/Scenes/GameScene.unity` として保存

### 2. Hierarchy 構成

```
GameScene
├── [Managers]
│   ├── GameManager        → GameManager.cs
│   ├── GridManager        → GridManager.cs
│   └── AdManager          → AdManager.cs
├── Canvas (Screen Space - Overlay)
│   ├── Header
│   │   ├── StageNameText  (UI.Text)
│   │   └── TimerArea
│   │       ├── TimerSlider (Slider)
│   │       └── TimerText   (UI.Text)
│   ├── GridContainer      → GridView.cs
│   │   └── GridLayout     (GridLayoutGroup)
│   ├── ButtonBar
│   │   ├── UndoButton
│   │   ├── HintButton
│   │   └── ResetButton
│   ├── ClearedPanel       (初期非表示)
│   │   ├── NextStageButton
│   │   └── ReplayButton
│   └── FailedPanel        (初期非表示)
│       ├── AddTimeButton
│       └── ReplayButton
└── GameBootstrap          → GameBootstrap.cs
```

### 3. GemCell Prefab を作成
`Assets/Prefabs/GemCell.prefab`
```
GemCell
├── BackgroundImage  (Image)
├── GemImage         (Image)
└── CompletedMark    (Image、初期非表示)
+ GemCellView.cs をアタッチ
+ Button コンポーネントをアタッチ（OnClick不要、Awakeで自動ワイヤリング済み）
```

### 4. StageData を作成
`Assets/ScriptableObjects/Stages/Stage_001.asset`
- Rows: 4, Cols: 4, TimeLimitSeconds: 300
- InitialCells と GoalCells を SCENE_SETUP.md の配置データで設定

### 5. 参照アサイン
- GameManager → GridManager フィールドに GridManager をセット
- GridView → GridManager, GridLayout, CellPrefab をセット
- GameUI → 各UIオブジェクトをセット
- GameBootstrap → GameManager, GridView, GameUI, TestStage をセット

---

## 動作確認チェックリスト

- [ ] Play で 4×4 グリッドが表示される
- [ ] タイマーがカウントダウンされる
- [ ] タップでグループ選択・移動できる
- [ ] Undo / Reset が動く
- [ ] 全グループ正位置 → クリアパネル表示
- [ ] タイムオーバー → 失敗パネル → 時間追加で続行

---

## 完了後にやること

1. この `FOR_ANTIGRAVITY.md` の完了事項を `AGENT_STATUS.md` に記録
2. 次のタスク（TASK-011: AdMob SDK / TASK-012: ステージ量産）に進む

よろしく！— ClaudeCode
