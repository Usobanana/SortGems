// GemColorPalette.cs — GemColor → Unity Color の変換テーブル
using UnityEngine;

namespace SortGems.Core
{
    public static class GemColorPalette
    {
        // ジェム本体色（ひし形◆に使用）: 鮮やかで視認性の高い色
        private static readonly Color[] _gemColors = new Color[]
        {
            Color.clear,                            // None
            new Color(1.00f, 0.20f, 0.20f),        // Red    — 鮮やかな赤
            new Color(0.20f, 0.50f, 1.00f),        // Blue   — コバルトブルー
            new Color(0.15f, 0.85f, 0.25f),        // Green  — エメラルドグリーン
            new Color(1.00f, 0.88f, 0.00f),        // Yellow — 純黄色
            new Color(0.70f, 0.20f, 1.00f),        // Purple — バイオレット
            new Color(1.00f, 0.50f, 0.00f),        // Orange — 濃オレンジ
            new Color(0.00f, 0.90f, 0.90f),        // Cyan   — シアン
            new Color(1.00f, 0.30f, 0.80f),        // Pink   — マゼンタピンク
        };

        // ゴールカラー（背景の薄い表示用）: 同系色の淡い版
        private static readonly Color[] _goalColors = new Color[]
        {
            Color.clear,
            new Color(1.00f, 0.20f, 0.20f, 0.22f),
            new Color(0.20f, 0.50f, 1.00f, 0.22f),
            new Color(0.15f, 0.85f, 0.25f, 0.22f),
            new Color(1.00f, 0.88f, 0.00f, 0.22f),
            new Color(0.70f, 0.20f, 1.00f, 0.22f),
            new Color(1.00f, 0.50f, 0.00f, 0.22f),
            new Color(0.00f, 0.90f, 0.90f, 0.22f),
            new Color(1.00f, 0.30f, 0.80f, 0.22f),
        };

        // 暗めのゴールカラー（空の目標マス用ベタ塗り）: 同系色を暗めのトーンにしたベタ塗り版
        private static readonly Color[] _darkGoalColors = new Color[]
        {
            Color.clear,
            new Color(0.35f, 0.10f, 0.10f, 1.00f), // Red (暗め)
            new Color(0.10f, 0.20f, 0.35f, 1.00f), // Blue (暗め)
            new Color(0.08f, 0.30f, 0.12f, 1.00f), // Green (暗め)
            new Color(0.35f, 0.30f, 0.00f, 1.00f), // Yellow (暗め)
            new Color(0.25f, 0.10f, 0.35f, 1.00f), // Purple (暗め)
            new Color(0.35f, 0.20f, 0.00f, 1.00f), // Orange (暗め)
            new Color(0.00f, 0.30f, 0.30f, 1.00f), // Cyan (暗め)
            new Color(0.35f, 0.12f, 0.28f, 1.00f), // Pink (暗め)
        };

        public static Color GetColor(GemColor gemColor)
        {
            int idx = (int)gemColor;
            if (idx < 0 || idx >= _gemColors.Length) return Color.white;
            return _gemColors[idx];
        }

        public static Color GetGoalColor(GemColor gemColor)
        {
            int idx = (int)gemColor;
            if (idx < 0 || idx >= _goalColors.Length) return Color.clear;
            return _goalColors[idx];
        }

        public static Color GetDarkGoalColor(GemColor gemColor)
        {
            int idx = (int)gemColor;
            if (idx < 0 || idx >= _darkGoalColors.Length) return Color.clear;
            return _darkGoalColors[idx];
        }

        private static Sprite _roundedRectSprite;
        public static Sprite RoundedRectSprite
        {
            get
            {
                if (_roundedRectSprite == null)
                {
                    Texture2D tex = CreateRoundedRectTexture(128, 128, 24); // 角丸半径24
                    _roundedRectSprite = Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
                }
                return _roundedRectSprite;
            }
        }

        /// <summary>
        /// 指定サイズと角丸半径で白色の角丸四角形テクスチャを動的に生成する。
        /// </summary>
        public static Texture2D CreateRoundedRectTexture(int width, int height, int radius)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] colors = new Color[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool isInside = true;
                    // 左下
                    if (x < radius && y < radius)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) > radius)
                            isInside = false;
                    }
                    // 右下
                    else if (x >= width - radius && y < radius)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(width - radius, radius)) > radius)
                            isInside = false;
                    }
                    // 左上
                    else if (x < radius && y >= height - radius)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius)) > radius)
                            isInside = false;
                    }
                    // 右上
                    else if (x >= width - radius && y >= height - radius)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(width - radius, height - radius)) > radius)
                            isInside = false;
                    }

                    colors[y * width + x] = isInside ? Color.white : Color.clear;
                }
            }
            tex.SetPixels(colors);
            tex.Apply(true);
            return tex;
        }
        private static Sprite _bevelShadowSprite;
        public static Sprite BevelShadowSprite
        {
            get
            {
                if (_bevelShadowSprite == null)
                {
                    Texture2D tex = CreateBevelShadowTexture(128, 128, 24); // 角丸半径24
                    _bevelShadowSprite = Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
                }
                return _bevelShadowSprite;
            }
        }

        /// <summary>
        /// 宝石のカットのように上下左右を対角線で4等分し、左上光（左上が白、右下が黒）の半透明陰影を、角丸四角形の形状で生成する。
        /// </summary>
        public static Texture2D CreateBevelShadowTexture(int width, int height, int radius)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] colors = new Color[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 1. まず角丸四角形の内外判定を行う
                    bool isInside = true;
                    // 左下
                    if (x < radius && y < radius)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) > radius)
                            isInside = false;
                    }
                    // 右下
                    else if (x >= width - radius && y < radius)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(width - radius, radius)) > radius)
                            isInside = false;
                    }
                    // 左上
                    else if (x < radius && y >= height - radius)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius)) > radius)
                            isInside = false;
                    }
                    // 右上
                    else if (x >= width - radius && y >= height - radius)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(width - radius, height - radius)) > radius)
                            isInside = false;
                    }

                    if (!isInside)
                    {
                        colors[y * width + x] = Color.clear;
                        continue;
                    }

                    // 2. 内側の場合は対角線による4つの三角形領域の判定を行い、陰影カラーを決定
                    bool isUpper = y >= x && y >= (height - 1 - x);
                    bool isLower = y < x && y < (height - 1 - x);
                    bool isLeft = y >= x && y < (height - 1 - x);
                    bool isRight = y < x && y >= (height - 1 - x);

                    Color c = Color.clear;
                    if (isLeft)
                    {
                        // 左：最も明るい (白半透明)
                        c = new Color(1f, 1f, 1f, 0.22f);
                    }
                    else if (isUpper)
                    {
                        // 上：少し明るい (白半透明)
                        c = new Color(1f, 1f, 1f, 0.12f);
                    }
                    else if (isRight)
                    {
                        // 右：少し暗い (黒半透明)
                        c = new Color(0f, 0f, 0f, 0.12f);
                    }
                    else if (isLower)
                    {
                        // 下：最も暗い (黒半透明)
                        c = new Color(0f, 0f, 0f, 0.28f);
                    }

                    colors[y * width + x] = c;
                }
            }
            tex.SetPixels(colors);
            tex.Apply(true);
            return tex;
        }
    }
}
