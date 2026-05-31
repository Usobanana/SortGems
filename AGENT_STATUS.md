# エージェント申し送りファイル（AGENT_STATUS.md）

> **このファイルはエージェント間の引き継ぎ・現在状況共有のためのものです。**  
> ClaudeCode / Antigravity / Codex が作業を引き継ぐ際は必ずここを読み、更新してください。

---

## 現在のフェーズ

**Phase 1 — コアゲームプレイ実装中 ＋ WebGL/GitHub Pages 公開対応**  
最終更新：2026-05-31  
更新者：ClaudeCode

---

## プロジェクト構成

```
C:/GitHub/SortGems/
├── SortGemsUnity/          ← Unity プロジェクト（メイン開発）
│   ├── Assets/
│   │   └── Scenes/         ← Unityシーン置き場
│   └── ...
├── sort-gems-godot/        ← Godot プロジェクト（サブ／参考用）
├── SPEC.md                 ← ゲーム仕様書（確定版）★必読
└── AGENT_STATUS.md         ← このファイル（状況共有）
```

---

## 直近の完了作業

| 日付 | 担当 | 内容 |
|------|------|------|
| 2026-05-30 | ClaudeCode | SPEC.md 仕様書を確定（v1.0） |
| 2026-05-30 | ClaudeCode | AGENT_STATUS.md 申し送りファイル作成 |
| 2026-05-30 | ClaudeCode | Unity フォルダ構成作成（Scripts/Core, UI, Ads, Prefabs, Art 等） |
| 2026-05-30 | ClaudeCode | TASK-001〜005 Core スクリ実装完了（下記参照） |
| 2026-05-30 | ClaudeCode | TASK-006〜009 View/UI/Ads スクリ実装完了（下記参照） |
| 2026-05-30 | Antigravity | TASK-010 Editor自動構築スクリプト実装（CreateGameScene.cs・TestMenu.cs）、GameScene.unity・GemCell.prefab・Stage_001.asset 生成 |
| 2026-05-30 | ClaudeCode | Stage_001の空きマスバグ修正（16/16→12/16ジェム、空き4マス確保） |
| 2026-05-30 | ClaudeCode | 実際のGems Flowを確認し2エリア構成に仕様確定 → コアクラスを全面改修 |
| 2026-05-30 | Antigravity | CreateGameScene.cs を新しい2エリア構成（メイン4x4, パレット2x4）および新StageData構造に合わせて修正 |
| 2026-05-30 | Antigravity | GameBootstrap.cs の初期化順序の誤りによる NullReferenceException バグを修正 |
| 2026-05-30 | ClaudeCode | グループ選択を「同色全選択」→「8方向Flood Fill連結成分」に変更（GridManager全面改修）|
| 2026-05-30 | ClaudeCode | CreateGameScene.cs を24×24グリッド・モバイル縦レイアウト(1080×1920)に修正 |
| 2026-05-30 | ClaudeCode | ジェム形状を◆ひし形に（GemImage 45°回転・0.62スケール）、GemColorPaletteを鮮やかな色に改善 |
| 2026-05-30 | ClaudeCode | 空きマス選択バグ修正（HandleTap早期return + FloodFill安全チェック）|
| 2026-05-30 | Antigravity | ジェム選択時にパレットの全空きマスが不必要にハイライトされるビジュアルバグを修正 |
| 2026-05-30 | Antigravity | 8方向Flood Fill選択に伴い、移動先空きマスの判定を従来の縦横直線から「8方向BFS連結成分」に改修（斜め一直線等の移動に対応） |
| 2026-05-30 | Antigravity | 選択グループが置けないサイズ不足の空きマスをハイライト対象から除外する機能を追加（置けない場所が光る矛盾を解決） |
| 2026-05-30 | Antigravity | 一致マスの選択除外＆ひし形非表示・正方形ベタ塗り（ピクセル同化）を徹底、Undo/Reset実行時に描画が更新されないバグを修正 |
| 2026-05-30 | Antigravity | 部分移動の緩和（空きマス不足でも置ける分だけ移動）、パレットの自動ソート（左詰め）、ジェムが1つずつ飛ぶ移動アニメーション演出を追加 |
| 2026-05-30 | Antigravity | 目標色に応じた部分移動制限（はみ出し防止）、同色の目標マスのみの白枠線フェード明滅ハイライト演出を追加 |
| 2026-05-31 | Antigravity | 白枠線明滅ハイライトを通常選択から非表示化し「ヒント」ボタンに統合、移動アニメーション開始時の座標設定順序を修正しチラつきバグを解消 |
| 2026-05-31 | Antigravity | AnimateSingleGem の初期非アクティブ化（SetActive(false)）による移動時チラつきバグの完全修正、およびシーン作成時のパッケージ保存エラー・GridManager.Reset の NullReferenceException 回避 |
| 2026-05-31 | Antigravity | デバッグ用一時停止機能の実装（ESCキーによるタイマー・操作・アニメーションの一時停止トグル） |
| 2026-05-31 | Antigravity | AnimateSingleGem に LayoutElement(ignoreLayout=true) を追加し、GridLayoutGroup による自動整列に起因するジェム移動アニメーションのチラつき（カクつき）を完全解消 |
| 2026-05-31 | Antigravity | WebGLビルド時の AdMob 設定例外エラー対策（GoogleMobileAdsSettings.asset の自動ファイル生成処理を追加し、AppID空白エラーおよびパッケージフォルダ保存エラーを解消） |
| 2026-05-31 | Antigravity | 表示・操作調整およびビジュアル刷新タスク完了（セルサイズの動的計算・完全センタリング、ジェムの3D斜めカット陰影、配置マスの暗転廃止と原色固定、はめ込み用くぼみ（SocketImage）の実装、パレット自動ソート、Undoロジックと部分配置の選択継続の修正） |

---

## 実装済みファイル一覧

```
Assets/Scripts/Core/
├── GemColor.cs         ✅ ジェムの色列挙型
├── GemColorPalette.cs  ✅ GemColor → Unity Color 変換テーブル
├── GemCell.cs          ✅ グリッド1マスのデータ
├── GemGroup.cs         ✅ 同色グループ（座標リスト・完了フラグ）
├── StageData.cs        ✅ ステージ定義 ScriptableObject
├── GridManager.cs      ✅ グリッド管理・移動ロジック・Undo/Reset・クリア判定
├── GemCellView.cs      ✅ 1マスの表示制御（選択/ハイライト/アニメーション）
└── GridView.cs         ✅ GridManagerを購読してビュー全体を更新

Assets/Scripts/UI/
├── GameUI.cs           ✅ タイマー/ボタン/クリア・失敗パネル制御
└── GameBootstrap.cs    ✅ シーン起動エントリーポイント（Stage→GridView→GameManager→UI）

Assets/Scripts/Ads/
└── AdManager.cs        ✅ Google AdMob スタブ実装（SDK未導入でもビルド可能）

ドキュメント:
├── SPEC.md             ✅ 仕様書確定版
├── AGENT_STATUS.md     ✅ このファイル
└── SCENE_SETUP.md      ✅ Unityエディタでのシーン手動構築手順書
```

---

## 現在の状況・次にやること

### ✅ 完了
- TASK-001: フォルダ構成
- TASK-002: グリッドシステム（GemCell, GemGroup, GridManager, StageData）
- TASK-003: タップ操作・グループ移動ロジック
- TASK-004: クリア判定・タイマー（GameManager）
- TASK-005: Undo / Reset
- TASK-006: GridView.cs + GemCellView.cs + GemColorPalette.cs
- TASK-007: SCENE_SETUP.md にステージデータ作成手順を記載（Unityエディタ作業）
- TASK-008: GameUI.cs（タイマー・ボタン・クリア/失敗パネル）+ GameBootstrap.cs
- TASK-009: AdManager.cs（AdMobスタブ、SDK未導入でもビルド可）

### 🟡 次に着手すること（優先順）

#### ⚠️ 直近の未確認事項（次のエージェントが最初に確認すること）
- `Tools > SortGems > Create Game Scene` を再実行してシーンを再生成する
- Playして以下を確認：
  - [ ] ジェムが◆ひし形で表示されるか
  - [ ] 空きマスをタップしてもグループ選択されないか
  - [ ] ジェムタップで連結成分のみ選択されるか（斜め隣接含む）
  - [ ] グループ移動でメイン↔パレット間が行き来できるか
  - [ ] 目標色とジェム色が一致したセルが◆ひし形から■正方形ベタ塗りになり、タップしても選択から除外されるか
  - [ ] Undo/Reset を実行した際に画面の表示が正しく更新されるか
  - [ ] 選択グループのサイズより空きマスが少ない場合でも、置ける分だけ部分移動できるか
  - [ ] ジェムが移動元から移動先に順番に（パラパラと）飛んでいくアニメーションが表示されるか
  - [ ] パレット内のジェムが常に隙間なく左詰めで自動整列されるか
  - [ ] ジェムを配置する際、タップした空マスの目標色と一致する連結マスにのみジェムが置かれる（他エリアへはみ出さない）か
  - [ ] ジェム選択時に、同色の目標色を持つメイングリッドの空きマスが光らなくなったこと（ヒントに分離されたこと）を確認したか
  - [ ] ジェム未選択時に「Hint」を押すと、すべての目標空きマスが白い枠線でふわふわ明滅するか
  - [ ] ジェム選択時に「Hint」を押すと、同色の目標空きマスが白い枠線でふわふわ明滅するか
  - [x] ジェムが移動する際に一瞬画面中央左にチラつく現象が解消されたか

#### ⚠️ TASK-010: Unityエディタ手動作業（CreateGameScene の再実行）
**CreateGameScene.cs の修正は完了しました。以下の手順でシーンを再構築してください：**
1. Unity Editor上部メニューから `Tools > SortGems > Create Game Scene` を実行する。
2. これにより、以下の3つが自動生成/更新されます。
   - `Assets/Scenes/GameScene.unity`
   - `Assets/Prefabs/GemCell.prefab`
   - `Assets/ScriptableObjects/Stages/Stage_001.asset`（メイン4x4、パレット2x4構成）
3. 再構築されたシーンをPlayし、メイングリッドとパレットの2エリア間でジェムが正しく移動できること、およびクリア判定が動くことを手動検証してください。

#### 🆕 TASK-011: WebGL ビルド → GitHub Pages 公開（iPhone でブラウザプレイ対応）

**目的：** iPhone の Safari / Chrome からそのままプレイできるようにする  
**方法：** Unity WebGL ビルド → `docs/` フォルダに出力 → GitHub Pages で公開

**手順：**

1. **Unity WebGL ビルド設定を確認・変更**
   - `File > Build Settings` でプラットフォームを **WebGL** に切り替え（Switch Platform）
   - `Player Settings` で以下を設定：
     - `Resolution and Presentation > Default Canvas Width/Height` → 1080 × 1920（縦向き）
     - `Compression Format` → **Disabled**（GitHub Pages は `.gz`/`.br` をそのまま配信できないため）
     - `Publishing Settings > Decompression Fallback` → ON
   - `Player Settings > Other Settings > Color Space` → Linear 推奨

2. **ビルド実行**
   - Build フォルダの出力先を `C:/GitHub/SortGems/docs` に指定（GitHub Pages のデフォルトパス）
   - `Build（Buildのみ）` を実行（Build and Run は不要）

3. **GitHub Pages 設定**
   - `SortGems` リポジトリの GitHub Pages を `main` ブランチの `/docs` フォルダに設定
   - `Settings > Pages > Source` → `Deploy from a branch` → `main` / `docs`

4. **iOS Safari 向け注意点**
   - `index.html` 内で `<meta name="viewport" content="width=device-width, initial-scale=1.0">` が正しく設定されているか確認（Unity WebGL テンプレートに含まれているはず）
   - iOS Safari はフルスクリーン不可（WKWebView 制限）なので、`Screen.fullScreen = false` でクラッシュ回避
   - `GemCellView.cs` や入力処理が `Input.GetMouseButton` ベースなら WebGL/モバイルでも動作するが、`Input.touchCount` を使っている箇所は `Input.GetMouseButtonDown(0)` と統合されているか確認

5. **ビルド後の検証**
   - `docs/index.html` をブラウザで開き、PC Chrome でまず動作確認
   - その後 iPhone Safari でアクセスし、タップ操作・レイアウトを確認

**注意：**
- AdMob（`AdManager.cs`）は WebGL 非対応。スタブのまま（`#if UNITY_ANDROID || UNITY_IOS` ガードをかけておくと安全）
- WebGL では `Application.OpenURL` / `SystemInfo` 等の一部 API 挙動が異なる場合あり

#### TASK-012（旧011）: AdMob SDK インポートと有効化
- https://github.com/googleads/googleads-mobile-unity/releases から最新版をDL
- Unity Package Manager でインポート
- `AdManager.cs` の `// ` コメントアウトを解除して有効化
- テスト広告IDで動作確認

#### TASK-013（旧012）: ステージデータを増やす（20本以上）
- TASK-010 で動作確認できたら、難易度別にステージデータを量産
- Easy: Stage_001〜007、Normal: Stage_008〜015、Hard: Stage_016〜

---

## 重要な仕様決定事項（要確認）

| 項目 | 決定内容 |
|------|----------|
| ジェム移動単位 | **同色グループまとめて移動**（1個ずつではない） |
| グリッド形状 | **矩形のみ**（N×M） |
| 時間制限 | **あり**（Gems Flow 準拠、ステージごとにタイマー設定） |
| ビジュアル | メニュー/背景に **Synty 3D ファンタジー素材** 使用予定 |
| 広告 | **Google AdMob**（リワード＋インタースティシャル＋バナー） |
| IAP | **Unity IAP**（広告除去・ヒントパック・タイムエクステンダー） |

---

## 引き継ぎ時の注意点

1. **作業前に必ず `SPEC.md` を読むこと**
2. **作業完了後、このファイルの「直近の完了作業」テーブルを更新すること**
3. **迷ったこと・決めたことは「未確定事項」か「重要な仕様決定事項」に追記すること**
4. **コミットメッセージは `[TASK-XXX] 内容` の形式で書くこと**
5. Unity の作業は `SortGemsUnity/` 以下で行う

---

## 確定した追加仕様

| 項目 | 決定内容 |
|------|----------|
| タイムオーバー時の挙動 | **途中状態を保持したまま**タイマーが止まる。リワード広告視聴 or アイテム消費で +時間して続行可能。「最初からやり直し」ではない。|
| プロジェクト形式 | Unity **2D プロジェクト**のまま進める |
| 背景3D追加方法 | Phase 4 で **デュアルカメラ方式** で追加（2D実装を変更不要）。3DカメラのDepth=-1、2DカメラのClearFlags=Depth Only。 |

---

## ブロッカー・問題

| 問題 | 状況 |
|------|------|
| mcp-unity 接続 | ClaudeCode（FleetView・CLI両方）では接続不可。**Antigravity が MCP 接続できるので Antigravity に委譲** |

---

## コミュニケーションメモ

- ClaudeCode がトークン切れで引き継ぐ場合：Antigravity or Codex が `AGENT_STATUS.md` を読んで作業再開
- 判断が必要な場合：ユーザー（ryohey1914@gmail.com）に確認を取ること
- 大きな設計変更は `SPEC.md` に反映した上で作業すること
