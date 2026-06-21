# モバイル セーフエリア・ノッチ対応

## 概要

iOS/Androidのノッチ、ホームインジケーター、ステータスバー、角丸ディスプレイに対応するための設計方針。

## Screen.safeArea

Unity が提供する `Screen.safeArea` は、ノッチやシステムUIを除いた安全な描画領域を `Rect` で返す。

- **iOS**: 正確（Dynamic Island, ノッチ, ホームインジケーター全て考慮）
- **Android 9+**: `renderOutsideSafeArea` 設定に依存
- **WebGL**: セーフエリアAPIなし（ブラウザが管理するため不要）

## UI Toolkit での対応

### 問題点
UI Toolkit には **セーフエリアの組み込みサポートがない**。UXML/USS は静的定義のため、ランタイムの `Screen.safeArea` を宣言的に適用できない。

### 推奨実装: SafeAreaContainer

```csharp
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class SafeAreaContainer : VisualElement
{
    public SafeAreaContainer()
    {
        RegisterCallback<AttachToPanelEvent>(_ => ApplySafeArea());
        // 画面回転時にも再計算
        RegisterCallback<GeometryChangedEvent>(_ => ApplySafeArea());
    }

    private void ApplySafeArea()
    {
        var safeArea = Screen.safeArea;
        var screenW = Screen.width;
        var screenH = Screen.height;

        // safeAreaをUI Toolkitの座標系（左上原点）に変換
        float left = safeArea.x;
        float right = screenW - (safeArea.x + safeArea.width);
        float top = screenH - (safeArea.y + safeArea.height);
        float bottom = safeArea.y;

        style.paddingLeft = left;
        style.paddingRight = right;
        style.paddingTop = top;
        style.paddingBottom = bottom;
    }
}
```

### 適用方法

ScreenManager の各画面で root に SafeAreaContainer を挟む:

```xml
<ui:VisualElement class="screen">
    <SafeAreaContainer style="flex-grow: 1;">
        <!-- 既存のUI要素 -->
    </SafeAreaContainer>
</ui:VisualElement>
```

## PanelSettings

- セーフエリア関連のプロパティはなし
- `PlayerSettings > Android > Render outside safe area` で制御（OFF推奨）

## 対応すべき領域

| 領域 | 影響する要素 | 対応方法 |
|------|-------------|---------|
| 上部ノッチ/Dynamic Island | HUDトップバー、ステージ名 | top padding |
| 下部ホームインジケーター | Undo/Hint/Resetボタン、広告バナー | bottom padding |
| 左右ノッチ（横画面時） | カルーセルカード | left/right padding |
| 角丸ディスプレイ | 四隅のボタン | セーフエリアで自動対応 |

## テスト方法

1. **Device Simulator** (Unity内蔵): Window > General > Device Simulator
   - iPhone 15 Pro, Pixel 7 等のプリセットでノッチ/パンチホールをシミュレート
2. **実機テスト**: iOS/Androidビルドで最終確認

## WebGL での考慮事項

- `Screen.safeArea` は全画面を返す（ノッチなし）
- ブラウザのアドレスバー・ツールバーはCSS viewport が管理
- 追加対応不要（現状のまま動作）

## 実装優先度

1. **Phase 1（現在不要）**: WebGLのみなら対応不要
2. **Phase 2（モバイルビルド時）**: SafeAreaContainer を実装し、HUDトップ/ボトムに適用
3. **Phase 3（リリース前）**: Device Simulator + 実機で全画面テスト

## 参考

- Unity公式: https://docs.unity3d.com/ScriptReference/Screen-safeArea.html
- コミュニティ実装: `artstorm/ui-toolkit-safe-area` (GitHub)
