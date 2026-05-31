// GemGroup.cs — 同色ジェムグループ（メイン・パレット両エリア対応）
using System.Collections.Generic;
using UnityEngine;

namespace SortGems.Core
{
    /// <summary>
    /// 同じ色のジェムグループ。タップ選択・グループ単位で移動する単位。
    /// セルはメイングリッドまたはパレットのどちらかに存在する。
    /// </summary>
    public class GemGroup
    {
        public int groupId;
        public GemColor color;

        /// <summary>グループを構成するセルリスト（位置 + どちらのグリッドか）</summary>
        public List<CellEntry> cells = new();

        public bool IsCompleted { get; private set; }

        public GemGroup(int groupId, GemColor color)
        {
            this.groupId = groupId;
            this.color   = color;
        }

        public void AddCell(Vector2Int pos, bool isPalette)
            => cells.Add(new CellEntry(pos, isPalette));

        public void SetCompleted(bool value) => IsCompleted = value;

        /// <summary>グループ内のセル情報</summary>
        public struct CellEntry
        {
            public Vector2Int pos;
            public bool isPalette;

            public CellEntry(Vector2Int pos, bool isPalette)
            {
                this.pos       = pos;
                this.isPalette = isPalette;
            }
        }
    }
}
