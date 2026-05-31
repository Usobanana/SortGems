# Unity シーン構築手順（GameScene）

> UnityエディタでGameScene.unityを手動で組み立てるための手順書。
> コードは全て `Assets/Scripts/` に揃っています。

---

## 前提：LeanTween のインポート

`GemCellView.cs` / `GridView.cs` / `GameUI.cs` が LeanTween を使用しています。  
**Asset Store または Package Manager から LeanTween を先にインポートしてください。**

- Asset Store: https://assetstore.unity.com/packages/tools/animation/leantween-3595
- または `com.dentsudoes.leantween` を Package Manager で追加

---

## シーン構成（Hierarchy）

```
GameScene
├── [Managers]                      (空のGameObject)
│   ├── GameManager                 → GameManager.cs を Add Component
│   ├── GridManager                 → GridManager.cs を Add Component
│   └── AdManager                   → AdManager.cs を Add Component
│
├── [Canvas] (Screen Space - Overlay)
│   ├── Header
│   │   ├── StageNameText           (TextMeshProUGUI)
│   │   └── TimerArea
│   │       ├── TimerSlider         (Slider)
│   │       └── TimerText           (TextMeshProUGUI)
│   │
│   ├── GridContainer               → GridView.cs を Add Component
│   │   └── GridLayout              (GridLayoutGroup)
│   │       └── (GemCell Prefabs が動的生成される)
│   │
│   ├── ButtonBar
│   │   ├── UndoButton              (Button)
│   │   ├── HintButton              (Button)
│   │   └── ResetButton             (Button)
│   │
│   ├── ClearedPanel                (Panel、初期非表示)
│   │   ├── ClearedText             (TextMeshProUGUI: "STAGE CLEAR!")
│   │   ├── NextStageButton         (Button)
│   │   └── ReplayButton            (Button)
│   │
│   └── FailedPanel                 (Panel、初期非表示)
│       ├── FailedText              (TextMeshProUGUI: "TIME'S UP!")
│       ├── AddTimeButton           (Button: "Watch Ad +1:00")
│       └── ReplayButton            (Button: "Retry")
│
└── GameBootstrap                   → GameBootstrap.cs を Add Component
```

---

## GemCell Prefab の作成

1. `Assets/Prefabs/` に新規 Prefab を作成 → 名前: `GemCell`
2. 構成:
   ```
   GemCell (GameObject)
   ├── BackgroundImage    (Image コンポーネント)
   ├── GemImage           (Image コンポーネント)
   └── CompletedMark      (Image or ParticleSystem、初期非表示)
   ```
3. `GemCell` ルートに `GemCellView.cs` を Add Component
4. Inspector で各参照をアサイン
5. PointerClick 用に `EventTrigger` を追加:
   - PointerClick → GemCellView.OnPointerClick()

---

## テスト用 StageData 作成

1. `Assets/ScriptableObjects/Stages/` で右クリック
2. `Create > SortGems > Stage Data` を選択 → `Stage_001` を作成
3. Inspector で設定:

```
Stage Number : 1
Stage Name   : "Tutorial 1"
Rows         : 4
Cols         : 4
Time Limit   : 300 (5分)

Initial Cells:  (例：4色、各4マス、計16マス)
  row:0 col:0  color:Red
  row:0 col:1  color:Blue
  row:0 col:2  color:Red
  row:0 col:3  color:Green
  row:1 col:0  color:Blue
  row:1 col:1  color:Green
  row:1 col:2  color:Yellow
  row:1 col:3  color:Yellow
  row:2 col:0  color:Green
  row:2 col:1  color:Yellow
  row:2 col:2  color:Blue
  row:2 col:3  color:Red
  row:3 col:0  color:Yellow
  row:3 col:1  color:Red
  row:3 col:2  color:Green
  row:3 col:3  color:Blue
  ※ 空きマスは設定しない（None = 空き）

Goal Cells: (4色が各行に集まる正解例)
  row:0 col:0〜3  color:Red   × 4
  row:1 col:0〜3  color:Blue  × 4
  row:2 col:0〜3  color:Green × 4
  row:3 col:0〜3  color:Yellow× 4
```

4. `GameBootstrap` の Inspector → `Test Stage` にドラッグ&ドロップ

---

## 参照のアサイン

### GameManager
- `Grid Manager` → GridManager GameObject をドラッグ

### GridView (GridContainer に追加)
- `Grid Manager` → GridManager
- `Grid Layout` → GridContainer/GridLayout
- `Cell Prefab` → Assets/Prefabs/GemCell

### GameUI (Canvas に追加)
- 各フィールドに対応する UI オブジェクトをアサイン

### GameBootstrap
- `Game Manager` → GameManager
- `Grid View` → GridContainer
- `Game UI` → Canvas
- `Test Stage` → Stage_001

---

## 動作確認チェックリスト

- [ ] Playモードで4×4グリッドが表示される
- [ ] タイマーが5:00からカウントダウンされる
- [ ] マスをタップするとグループが選択（ハイライト）される
- [ ] 空きマスをタップするとグループが移動する
- [ ] Undoボタンで1手前に戻る
- [ ] Resetボタンで初期状態に戻る
- [ ] 全グループ正位置→クリアパネルが表示される
- [ ] タイムオーバー→失敗パネルが表示される
- [ ] 失敗パネルの「+時間」ボタンでタイマー再開される
