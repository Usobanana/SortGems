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
    }
}
