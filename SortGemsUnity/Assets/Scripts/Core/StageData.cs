// StageData.cs — ステージデータ（ScriptableObject）
using System.Collections.Generic;
using UnityEngine;

namespace SortGems.Core
{
    /// <summary>
    /// ステージの定義データ。
    ///
    /// 構造：
    ///   メイングリッド（mainRows × mainCols）
    ///     - 各マスに goalColor（= ピクセルアートの目標色）を持つ
    ///     - 初期配置は initialMainCells でバラバラに定義
    ///   パレット（paletteRows × paletteCols）
    ///     - 一時退避用。goalColor なし。初期は空。
    /// </summary>
    [CreateAssetMenu(fileName = "Stage_001", menuName = "SortGems/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("基本情報")]
        public int stageNumber;
        public string stageName;

        [Header("時間制限（秒）")]
        public float timeLimitSeconds = 300f;

        [Header("メイングリッド設定")]
        public int mainRows = 8;
        public int mainCols = 8;

        [Header("パレット設定（一時退避エリア）")]
        public int paletteRows = 4;
        public int paletteCols = 8;

        [Header("ゴール配置（= ピクセルアートの完成図）")]
        [Tooltip("メイングリッドの各マスの目標色。None のマスはクリア判定に含まない")]
        public List<CellColorDef> goalLayout = new();

        [Header("ジェム初期配置（メイングリッド）")]
        [Tooltip("ゲーム開始時のジェム配置。goalLayout と同じジェム数だが色が混在している")]
        public List<CellColorDef> initialMainCells = new();

        [Header("ジェム初期配置（パレット）")]
        [Tooltip("通常は空。特殊ステージ用")]
        public List<CellColorDef> initialPaletteCells = new();

        [Header("ピクセルアート")]
        [Tooltip("完成絵のテクスチャ（goalLayout の代わりにテクスチャから自動生成も可）")]
        public Texture2D pixelArtTexture;

        [System.Serializable]
        public class CellColorDef
        {
            public int row;
            public int col;
            public GemColor color;
        }
    }
}
