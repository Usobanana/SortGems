# SortGems 画面仕様書

現行実装（`main` ブランチ、直近プッシュ時点）のスクリーンショット・UXML・USS・C#コードから読み取れる「現状の仕様」を整理したベースラインドキュメント。今後のレイアウト再設計は、このドキュメントとワイヤーフレーム（`documents/wireframes/wireframes.html`）を出発点に議論する。

対象外: `DebugMenu.cs`（開発用オーバーレイ）、WebGLビルドのブラウザコンソールパネルはプレイヤー向け仕様に含めない。

---

## 1. 全体構成

SortGemsの画面はUnityの2レイヤーで構成されている。

| レイヤー | 技術 | 役割 |
|---|---|---|
| Chrome/HUD | UI Toolkit (`UIDocument` + `ScreenManager`) | タイトル、ステージ選択、ゲームプレイのヘッダー/フッター/ダイアログ |
| パズル本体 | uGUI (`Canvas` + `GridView` + `GemCellView`) | メイングリッドとパレットのジェム表示・タップ操作 |

`ScreenManager.cs` が画面遷移（Title / StageSelect / GamePlay の3状態）を管理し、`_root.Clear()` → 該当UXMLの `CloneTree()` で画面を丸ごと差し替える。パズルグリッド用の `Canvas`（`_uguiGamePlayPanel`）はゲームプレイ画面の時だけ `SetActive(true)`。

キャンバス基準解像度: **1080 × 1920（9:16 縦持ち）**。`GridView.BuildGrid()` 内でハードコードされている。

**【重要・不変条件】`[UIToolkit]`（`UIDocument`）の `Sort Order` は `[Canvas]`（uGUI, `sortingOrder: 0`）より必ず大きい値にすること。** 両者が同値の場合、Unityの描画順は保証されず、UI Toolkit側の半透明レイヤー（`.overlay-panel` のスクリムなど）だけがuGUI Canvasの下に描画され、不透明な子要素（ダイアログの `.panel` など）だけが上に見える、という不具合が発生する（2026-07-11に発見・修正）。現在 `UIDocument.m_SortingOrder = 10` を [GameScene.unity](../SortGemsUnity/Assets/Scenes/GameScene.unity) に設定済み。今後シーンを作り直す・複製する際は必ずこの値を維持すること（`CreateGameScene.cs` 側にも同様の明示設定を入れるのが望ましい・未実施）。

---

## 2. 画面遷移フロー

```
┌─────────┐  Game Start   ┌──────────────┐   Start    ┌─────────────┐
│  Title  │ ────────────▶ │ Stage Select │ ─────────▶ │  Game Play  │
└─────────┘ ◀──────────── └──────────────┘ ◀───────── └─────────────┘
                 Back                          Back / Cleared→Back

Game Play 内のオーバーレイ（背景を暗くして中央にパネル表示）:
  - Cleared パネル   : Next Stage / Replay / Back
  - Failed パネル    : +1:00 (Watch Ad) / Replay / Back
  - Unlock ダイアログ : Use Item / Watch Ad / Back（パレット2段目タップ時）

Next Stage → 次ステージがあれば Game Play へ再ロード、なければ Stage Select へ
```

---

## 3. デザイントークン（`Common.uss` 準拠）

### 3.1 カラー

| トークン | 値 | 用途 |
|---|---|---|
| `--color-bg` | `rgb(24,24,32)` | 画面全体の背景 |
| `--color-surface` | `rgba(40,40,56,0.92)` | パネル背景（ダイアログ等） |
| `--color-surface-high` | `rgba(50,50,70,0.95)` | ステージカード背景 |
| `--color-overlay` | `rgba(0,0,0,0.6)` | モーダルの背面スクリム |
| `--color-primary` | `rgb(150,90,230)` | プライマリボタン（紫） |
| `--color-secondary` | `rgba(255,255,255,0.10)` | セカンダリボタン |
| `--color-accent` | `rgb(255,200,60)` | クリア時タイトル、強調 |
| `--color-danger` | `rgb(240,80,80)` | 失敗時タイトル、危険操作 |
| `--color-success` | `rgb(50,215,75)` | タイマー満タン時 |
| `--color-text` / `--color-text-dim` | `rgb(240,240,240)` / `rgb(170,170,190)` | 主/副テキスト |

### 3.2 タイポグラフィ

フォント: `Caveat-Bold`（手書き風）を全画面共通で使用。

| トークン | サイズ |
|---|---|
| `--fs-display` | 72px（CLEARED! / TIME UP） |
| `--fs-title` | 48px |
| `--fs-h1` 〜 `--fs-h3` | 40 / 32 / 28px |
| `--fs-body-lg` / `--fs-body` | 24 / 20px |
| `--fs-btn-lg` / `--fs-btn-md` / `--fs-btn-sm` | 40 / 26 / 19px |
| `--fs-small` | 16px |

タイトルロゴのみ個別指定: `title-logo-main` 96px、`title-logo-sub` 30px（letter-spacing 8px）。

### 3.3 スペーシング／角丸

`--sp-xs..2xl`: 4 / 8 / 16 / 24 / 32 / 48px　　`--radius-sm..lg`: 8 / 12 / 16px、円形は50%。

### 3.4 ボタン

- `.btn`: 基本形。padding 16×36、min-height 58px、角丸12px、ホバーで拡大(1.03)+明色化、押下で縮小(0.97)+暗色化。
- `.btn-lg`: 大サイズ。padding 24×56、min-height 84px、min-width 280px。
- `.btn-cta`: Title の Game Start / StageSelect の Start 専用。`.btn-lg` に加え `width:560px; font-size:48px;` を画面下から絶対位置固定(`bottom:360px`)で配置。
- `.btn-primary-size`: `width:560px; font-size:48px; min-height:84px`（`.btn-cta` と同じ footprint、位置指定なし）。**ヘッダーに配置されるボタン（`.btn-icon` の戻る矢印等）を除く、ダイアログ内ボタンなど非ヘッダーの主要ボタンはこのサイズをルールとする**（2026-07-11決定）。現在は `cleared-panel` / `failed-panel` / `unlock-dialog` の全ボタン（Next Stage/Replay/Back 等）に適用済み。
- `.btn-secondary`: 半透明白背景（戻る、Undo/Hint/Reset、キャンセル系）。`.btn-primary-size` と併用してサイズのみ揃えることが多い。
- `.btn-icon`: 正方形に近い、戻る矢印(◀)専用。56×56px。サイズ統一ルールの対象外。
- `.btn-nav`: ステージ選択のカルーセル送り矢印。円形64×64px、画面端に絶対配置。

---

## 4. 画面別仕様

### 4.1 タイトル画面 (`TitleScreen.uxml`)

```
┌──────────────────────────┐
│   [背景: bg_title_lofi]   │
│                            │
│                            │
│        Lo-Fi Chill         │ ← title-logo-main 96px
│      Gems Sort Game        │ ← title-logo-sub 30px, letter-spacing 8px
│                            │
│                            │
│                            │
│      [ Game Start ]        │ ← btn-cta, bottom:360px固定, width:560px
│                            │
│           v1.0              │ ← version-label（画面下部中央、Application.version）
└──────────────────────────┘
```

- 要素はロゴ2行 + バージョン表記。中央揃え(`.screen`のalign-items/justify-content center)。
- `EdgeGradientOverlay` が上端・下端にアーチ状のグラデーション幕（黒フェード）を重ねる。
- CTAボタンは画面下から360px固定位置（画面高に対する相対値ではない = 縦長比率が変わると余白比率がズレる）。
- **【2026-07-11追加】バージョン表記。** `version-label`（`--fs-small` 16px、`--color-text-dim`）を画面最下部(`bottom:24px`)に中央揃えで配置。`ScreenManager.ShowTitle()` が `Application.version`（`PlayerSettings.bundleVersion`）を実行時にセットする。

### 4.2 ステージ選択画面 (`StageSelectScreen.uxml`)

```
┌──────────────────────────┐
│ [◀]                       │ ← stage-header、戻るボタンのみ
│   Stage N: 名前ラベル      │ ← active-stage-label, scrim付き, width:800px(stage-cardと同幅)
│  ◀   ┌────────────┐   ▶   │ ← btn-nav（円形送りボタン、前後ステージが無い時は非表示）
│      │  [preview] │       │ ← stage-card 800×700、カルーセル中央=アクティブ(scale 1.05)
│      │ Stage N: 名 │       │
│      │  [CLEARED]  │       │ ← 未クリア鍵無しは空文字、ロック中は"LOCKED"+opacity0.4
│      └────────────┘       │
│                            │
│        [ Start ]           │ ← btn-cta, 未アンロック時は非表示
└──────────────────────────┘
```

- カルーセルは**仮想化**（DOMプールは常に3枚、スワイプ/矢印ボタンで中身を差し替え）。カード幅832px固定。
- 各カードのプレビューはゴールレイアウトから動的生成した500×500のドットアートテクスチャ（`GetOrCreatePreview`）。未クリアはグレースケール、クリア済みはフルカラー。
- ロック判定: 直前ステージが `StageCleared_{N}` でない場合 `LOCKED`。
- **【2026-07-11変更】`active-stage-label` の位置。** 従来はカルーセルの下（Startボタンの上）に配置していたが、`stage-card` の直上・同幅(800px)に変更。これに伴い `carousel-wrapper` から `flex-grow:1` を外し、コンテンツ量に応じたサイズへ変更（末尾のスペーサーが余った縦スペースを吸収する構成に統一）。

### 4.3 ゲームプレイ画面

#### 4.3.1 HUDチロム (`GamePlayHUD.uxml`, UI Toolkit)

```
┌──────────────────────────┐
│ [◀] Stage 1                │ ← hud-top, 背景なし（透過）
│                            │
│      ▓▓▓▓▓▓▓▓░░░ 03:00     │ ← timer-group（headerH直下の予約帯、高さtimerH固定）
│                            │
│   (uGUIパズルグリッド)      │ ← Canvas側で動的配置
│                            │
│   (パレット2段 + Unlock)   │
│                            │
│ [ Undo ] [ Hint ] [ Reset ]│ ← hud-bottom, 背景なし（透過）
└──────────────────────────┘
```

- **【2026-07-11変更】`hud-top` / `hud-bottom` の背景帯を削除。** 従来は `background-color: var(--color-bar-bg)`（黒50%）の帯を敷いていたが、下地なしの透過表示に変更。ボタン自体は `.btn`/`.btn-secondary` の背景で視認性を確保する。

- **【決定】タイマー帯は headerH / bottomH と同じ「予約帯」方式にする。** 旧実装は `timer-group` を `top: 480px` の絶対値でハードコードしており、グリッドの実際の開始Y座標（ステージごとに可変）と無関係だったため、行数の多いステージでグリッドとタイマーが近接／重なる可能性があった。
  - 新方式: `GridView.cs` の `availH` 計算に `timerH`（タイマーバー分の高さ、目安72px）を追加し、`availH = canvasHeight - headerH - timerH - bottomH`、グリッド開始Y `mainTopY = -headerH - timerH - topMargin` とする。
  - `GamePlayHUD.uss` 側は `timer-group` の `top` をマジックナンバーではなく `headerH` 定数（120px）に変更し、常にヘッダー直下の固定スロットに収める。
  - これにより「グリッドの実際の位置を読み取ってタイマーを追従させる」という2システム間の同期を作らずに、構造的に重なりをなくせる（詳細は §6-1 参照）。
- HUD自体は `picking-mode="Ignore"` の透明背景で、実際のパズル操作はすべて下のuGUI Canvasが受ける。

#### 4.3.2 パズルグリッド + パレット（uGUI, `GridView.cs`）

`BuildGrid()` が実行時にレイアウトを計算する。定数（現状値）:

| 変数 | 値 |
|---|---|
| canvas基準サイズ | 1080 × 1920（※実際は親`Canvas`の実サイズを実行時に読む。Canvasが見つからない場合のみのフォールバック値） |
| sideMargin | 8px |
| padVal（内側余白） | 6px |
| headerH（上部除外領域） | 120px |
| timerH（タイマー予約帯・**新規**） | 72px（目安、要調整） |
| bottomH（下部除外領域） | 100px |
| gap（メイン⇔パレット間） | 16px |
| maxMainCellSize | ~~45px~~ → **【決定・実装待ち】90px** |
| maxPaletteCellSize | ~~75px~~ → **【決定・実装待ち】100px** |

計算順序:
1. 使用可能幅 `availW = 1080 - sideMargin*2 - padVal*2`
2. セルサイズ = `min(availW/cols, availH*0.8/rows, maxMainCellSize)` を切り捨て
3. パレットのセルサイズはメイングリッド幅に合わせて逆算（`mainW / paletteCols`、上限は上記 maxPaletteCellSize）
4. 残り縦余白を使ってメイン+ギャップ+パレットを縦方向センタリング

**【決定・実装待ち】マス/ジェムのサイズ引き上げ。** 実データ（`Stage_001.asset`: 13×13, パレット14列）で試算すると、幅からは約80.9px、高さからは約100pxまでセルサイズを出せるにもかかわらず、`maxMainCellSize=45px` の固定上限で潰されていた（パレットも連動して約42.6pxまで縮小）。モバイル実機でマス・ジェムが小さく見える主因はこの上限値であって、`availW/cols` や `availH*0.8/rows` 自体の計算式は問題ない。

- 上限を45→90px、パレット上限を75→100pxに引き上げる。列数が多い高密度ステージ（24列など）では `availW/cols` 側が先に効くため、上限を上げても悪影響はない（試算: 24列で幅制約は約43.8px、新上限90pxより十分小さいまま）。
- タップ判定は `GridView.CorrectTapTarget()` が既にセルサイズの1.3倍まで吸い寄せる仕組みを持つため、見た目のセルを大きくすることは誤タップ低減にも直接寄与する。
- 縦方向の `0.8` 係数（パレット分の余白見込み）は今回は変更しない。今後さらに詰めるなら、メイン+パレットの合計高さを直接解く方式への置き換えも検討余地として残す。

**【確認済み・対応不要】メイングリッドの列数（横マス数）の偶数/奇数対応。** `GridView.cs` のランタイム計算には偶数/奇数を区別する処理が無く、`GridLayoutGroup` は列数に関わらず自動整列、コンテナも中央アンカーで配置されるため、奇数列なら中央に1本の軸、偶数列なら中央が2列の境界になるだけで、いずれも正しく中央揃えされる。実際 `Stage_001.asset` は13×13（奇数）で特殊対応なく動作済み。

- **本当の火種はランタイム側ではなくステージ生成ツール側**（`StageArtAutofixWindow.cs` 等）。`documents/pixel-art-rules.md` に記録されている「12×12の元アートを13×13へ強制拡張する際、オフセット計算 `(13-12)/2=0` で左上詰めになり余白が偏る」というバグがこれにあたる。画面レイアウト設計としての追加対応は不要。

**【2026-07-11決定・実装済み】移動ルール: 選択中グループは同じ目標色のマスにしか移動できない。** 従来は「目標色（`goalColor`）が設定されていないメイングリッド上の空きマス（ピクセルアート形状内だが目標のない自由マス）」には、色を問わず自由に流し込める抜け穴があった。ハイライト表示（`GridView.HighlightEmpties`）は元々「選択色と同じ`goalColor`のマスのみ」を光らせていたため、見た目上のヒントと実際に許可される移動の間に不整合があった。`GridManager.FindContiguousEmpty()` の `IsAvailable()` を「メイングリッドへの移動は常に `goalColor == group.color` のマスのみ許可（自グループが現在占有しているマス=`srcSet`は対象外）」に統一し、ハイライトと実際の移動可否を一致させた。パレットへの退避（`goalColor`を持たない）は従来通り色フリーのまま。

セル1つの見た目（`GemCellView.cs`、前回プッシュ版）:
- 背景（`_backgroundImage`）＝フルサイズ。ジェムが目標色と一致していれば鮮やかな色、不一致なら目標色を薄く表示、空マスならグレー。
- ジェム本体（`_gemImage`）＝角丸スプライト、背景の72%スケールで中央配置。正解時は影(Shadow)を消してフラットに見せる。
- ソケット（`_socketImage`）＝空マスの時だけ表示するくぼみ演出。

パレット仕様:
- 既定 `paletteRows=4, paletteCols=8`（`StageData` デフォルト値。実際のステージごとの値は`Stage_XXX.asset`側で上書きされている）。
- 2段目はロック状態で開始する。
- **【決定・実装待ち】アンロックの起点を専用ボタンから「ロック行のセル自体」に変更する。** 旧実装は `GemCellView.SetLocked(true)` が `blocksRaycasts=false` にしてセルを完全に無反応化し、代わりに `UnlockRow2Button`（パレット内に絶対配置、幅=パレット幅60%×高さ=セルサイズ80%）だけがタップ対象だった。セルサイズが縮むステージではこの小さなボタンが押しにくくなる。
  - 新方式: ロック行のセルは `blocksRaycasts=true` のまま維持し、タップされたら（ジェム移動ではなく）`OnUnlockButtonClicked` を発火させる専用分岐を `GemCellView`/`GridManager` に追加する。行全体（8マス分）がタップ領域になるため、セル1つが小さくても実用上の当たり判定は十分に確保できる。
  - ロック表示: 施錠アイコン＋ハッチング柄は維持しつつ、行全体に「押せる帯」だとわかる薄いハイライト/枠線を追加する。
  - アンロック方法: アイテム消費（未実装、TODOコメントあり） or 広告視聴。

### 4.4 オーバーレイダイアログ（すべて `GamePlayHUD.uxml` 内、`display:none`初期）

3種類とも共通の `.overlay-panel`（画面全面スクリム、`--color-overlay` = 黒60%）+ `.panel`（中央カード、`--color-surface`）構造。ボタンはヘッダー系（戻る矢印等）を除き全て `.btn-primary-size`（§3.4参照、Game Start/Startと同サイズ）に統一。

| ダイアログ | タイトル | ボタン（上から、いずれも同サイズ） |
|---|---|---|
| `cleared-panel` | "CLEARED!"（accent色、72px） | Next Stage / Replay / Back |
| `failed-panel` | "TIME UP"（danger色、72px） | +1:00 Watch Ad / Replay / Back |
| `unlock-dialog` | "Unlock Row 2"（40px） | Use Item / Watch Ad / Back |

- **【2026-07-11修正】UXML上の宣言順を `timer-group` の後ろに変更。** UI Toolkit内では後に宣言された兄弟要素ほど手前に描画されるため、旧構成では `timer-group` がダイアログより後に宣言されており、ダイアログ表示中でもタイマーが最前面に出てしまう問題があった。ダイアログ3種を `timer-group` の後ろへ移動して解消。
- **【2026-07-11修正】全画面マスクが効かない不具合。** §1に記載の `UIDocument`/`Canvas` の `Sort Order` 同値問題により、`.overlay-panel` の半透明スクリムがuGUIパズルグリッドの下に描画され、ダイアログの不透明パネルだけが浮いて見える状態だった。`UIDocument.m_SortingOrder = 10` に設定して解消。

---

## 5. ゲーム状態（`GameManager.cs`）

`Idle → Playing ⇄ Paused → Cleared` または `Playing → Failed → (Replay)Playing`

- `Paused` はステージ選択に戻る/バックグラウンド遷移時などに使用（タイマー停止）。
- `Failed` から `AddTime()` で `Playing` に復帰可能（広告視聴による時間延長）。

---

## 6. 未確定・要検討事項（現状把握で見つかった論点）

これらは「バグ」ではなく、次の綿密なレイアウト設計で判断が必要な項目。

1. ~~**タイマー位置の絶対値固定**~~ → **解決方針決定済み（実装待ち）**。`top:480px` の絶対値をやめ、headerH/bottomHと同じ「予約帯」方式にする。
   - `GridView.cs`: `availH = canvasHeight - headerH - timerH - bottomH`、`mainTopY = -headerH - timerH - topMargin` とし、グリッドが常に予約帯の下から始まるようにする。
   - `GamePlayHUD.uss`: `timer-group` の `top` をマジックナンバーではなく `headerH` 定数に変更。
   - 却下案: BuildGrid()後にグリッドの実際の上端Y座標をScreenManagerが読み取ってタイマー位置に反映する方式。見た目の密着度は上がるが、UI Toolkit(HUD)とuGUI(グリッド)という独立した2システム間に新しい同期ポイントが増え、レイアウト変更のたびに壊れやすくなるため見送り。
   - 前提確認事項: UI ToolkitパネルのPanel Settingsとplay画面のCanvasScalerが同一の基準解像度（1080×1920）にスケーリングされているか要確認。ズレていると「px」の意味が両システムで一致しない。
2. ~~**HUDとCanvasの二重レイヤー構造**~~ → **解決方針決定済み（実装待ち）**。UI Toolkit（HUD）とuGUI（グリッド）が完全に独立して座標計算されている問題は、`GridView.cs` 側の定数（`headerH`/`timerH`/`bottomH`等）を単一の正とし、`ScreenManager.ShowGamePlay()` がHUDクローン直後に同じ値を対象VisualElementの `style.top` へ注入することで解消する。`GamePlayHUD.uss` 上の値は実行時に上書きされる前提の「フォールバック表示用」に格下げする。
3. ~~**パレット2段目ロックUI**~~ → **解決方針決定済み（実装待ち）**。詳細は §4.3.2「パレット仕様」参照。専用の小さなアンロックボタンを廃止し、ロック行のセル自体をタップ領域にする。
4. **キャンバス基準1080×1920固定 → 実質的には非問題、既存ドキュメントへの追記のみ**。`GridView.BuildGrid()` は実際には親`Canvas`の実サイズを実行時に読んでおり、1080×1920は取得失敗時のフォールバックに過ぎない（§4.3.2の表を参照、記述を修正済み）。対応: `documents/mobile-safe-area.md` に、uGUI側では `headerH`(120px)/`bottomH`(100px) が簡易的なセーフエリア代わりとして機能している旨を追記し、将来的に `Screen.safeArea` と連動させる拡張余地を明記する（未実施）。
5. **ステージカードの固定ピクセルサイズ**（800×700、カード幅832px固定）→ **対応不要と判断**。ゲーム全体が固定基準解像度＋CanvasScalerでスケールする設計を前提にしている以上、基準解像度内での固定pxは想定通りの設計であり、レスポンシブ化の必要はない。
6. **UI Toolkit（HUD）とuGUI（グリッド）のハイブリッド構成そのものの是非**（2026-07-11提起）。今回、両者の描画順（`Sort Order`）が同値だとダイアログのスクリムだけがグリッドの下に描かれる不具合が実際に発生した（§1・§4.4参照）。境界を跨ぐたびに手動でソート順を握り合わせる必要がある点は構成上のコストとして残る。
   - **現時点の判断: ハイブリッド構成を維持**。`Sort Order` の大小関係（UIDocument=10 > Canvas=0）を明示的な不変条件としてドキュメント化する対症療法に留める。
   - **理由**: パズルグリッドは `GridLayoutGroup` ベースで多数セルを動的生成する性能シビアな部分であり、UI Toolkit（Yogaレイアウト）へ丸ごと移行するのは書き直しリスクが大きい。一方でTitle/StageSelect/HUDは既にUI Toolkit化済みで、越境が発生するのはこのダイアログ表示のような限定的な箇所のみ。
   - **見直しトリガー**: 同種の越境バグ（描画順・入力ヒット判定・座標系のズレなど）が再発した場合、またはパズルグリッド側で大きな性能改修を行うタイミングで、グリッドのUI Toolkit移行を再検討する。

---

## 7. 関連ファイル

- UXML: `SortGemsUnity/Assets/UI/Screens/{TitleScreen,StageSelectScreen,GamePlayHUD}.uxml`
- USS: `SortGemsUnity/Assets/UI/Styles/{Common,TitleScreen,StageSelectScreen,GamePlayHUD}.uss`
- C#: `ScreenManager.cs`, `StageCarousel.cs`, `GridView.cs`, `GemCellView.cs`, `GameManager.cs`, `StageData.cs`
- ワイヤーフレーム: `documents/wireframes/wireframes.html`
