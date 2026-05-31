// GemCell.cs — グリッド上の1マスのデータ
using UnityEngine;

namespace SortGems.Core
{
    /// <summary>
    /// グリッド上の1マス。
    /// - メイングリッド：goalColor に目標色が設定されている（ピクセルアートの1ドット）
    /// - パレット：goalColor = None（一時退避用、目標色なし）
    /// </summary>
    [System.Serializable]
    public class GemCell
    {
        public int row;
        public int col;
        public GemColor color;      // 現在のジェム色（None = 空き）
        public GemColor goalColor;  // 目標色（None = パレット or 無関係マス）
        public int groupId;         // 同色グループID（-1 = 空き）
        public bool isLocked;       // ロックマス（将来拡張用）
        public bool isVoid;         // 空白マス（ピクセルアートの形状以外でジェムが置けない場所）

        public bool IsEmpty    => !isVoid && color == GemColor.None;
        public bool HasGoal    => goalColor != GemColor.None;
        /// <summary>正しい色のジェムが置かれているか（クリア判定用）</summary>
        public bool IsCorrect  => HasGoal && color == goalColor;

        public GemCell(int row, int col,
                       GemColor color = GemColor.None,
                       GemColor goalColor = GemColor.None,
                       int groupId = -1)
        {
            this.row      = row;
            this.col      = col;
            this.color    = color;
            this.goalColor = goalColor;
            this.groupId  = groupId;
            this.isLocked = false;
            this.isVoid   = false;
        }

        public GemCell Clone() =>
            new GemCell(row, col, color, goalColor, groupId) { isLocked = isLocked, isVoid = isVoid };
    }
}
