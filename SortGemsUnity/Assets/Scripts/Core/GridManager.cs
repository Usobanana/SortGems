// GridManager.cs — メイングリッド＋パレットの2エリア管理
// グループ選択: タップ位置から8方向連結成分(Flood Fill)で動的に決定
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SortGems.Core
{
    [System.Serializable]
    public struct MoveStep
    {
        public Vector2Int fromPos;
        public bool fromIsPalette;
        public Vector2Int toPos;
        public bool toIsPalette;
    }

    /// <summary>
    /// ゲームの核心クラス。
    ///
    /// グループ選択ルール:
    ///   タップしたマスから 上下左右＋斜め(8方向) に同色が隣接していれば
    ///   同じグループとして扱う（Flood Fill）。
    ///   同色でも繋がっていなければ別グループ。
    ///
    /// 2エリア構成:
    ///   メイングリッド … 各マスに goalColor。全マス IsCorrect でクリア。
    ///   パレット       … goalColor なし。一時退避用。クリア時は全空き。
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        // ---- 公開イベント ----
        public System.Action<GemGroup> OnGroupSelected;   // null = 選択解除
        public System.Action<List<MoveStep>, GemColor> OnGroupMoved;
        public System.Action           OnStageCleared;
        public System.Action<GemColor> OnColorCompleted;  // ある色が全て正位置に揃った

        // ---- グリッド状態 ----
        private GemCell[,] _main;
        private GemCell[,] _palette;
        private GemCell[,] _initMain;
        private GemCell[,] _initPalette;

        private GemGroup _selectedGroup;
        private Stack<(GemCell[,] main, GemCell[,] palette, GemGroup selectedGroup)> _undoStack = new();

        // 8方向オフセット（上下左右＋斜め）
        private static readonly Vector2Int[] Dirs8 = {
            new(-1,-1), new(-1,0), new(-1,1),
            new( 0,-1),            new( 0,1),
            new( 1,-1), new( 1,0), new( 1,1),
        };

        public int MainRows    { get; private set; }
        public int MainCols    { get; private set; }
        public int PaletteRows { get; private set; }
        public int PaletteCols { get; private set; }
        public GemGroup SelectedGroup => _selectedGroup;

        // ---- 初期化 ----

        public void Initialize(StageData stage)
        {
            MainRows    = stage.mainRows;
            MainCols    = stage.mainCols;
            PaletteRows = stage.paletteRows;
            PaletteCols = stage.paletteCols;

            _main    = new GemCell[MainRows, MainCols];
            _palette = new GemCell[PaletteRows, PaletteCols];
            _undoStack.Clear();
            _selectedGroup = null;

            // メイングリッド初期化
            for (int r = 0; r < MainRows; r++)
                for (int c = 0; c < MainCols; c++)
                    _main[r, c] = new GemCell(r, c);

            foreach (var def in stage.goalLayout)
                if (InMainBounds(def.row, def.col))
                    _main[def.row, def.col].goalColor = def.color;

            foreach (var def in stage.initialMainCells)
                if (InMainBounds(def.row, def.col))
                    _main[def.row, def.col].color = def.color;

            // パレット初期化
            for (int r = 0; r < PaletteRows; r++)
                for (int c = 0; c < PaletteCols; c++)
                    _palette[r, c] = new GemCell(r, c);

            foreach (var def in stage.initialPaletteCells)
                if (InPaletteBounds(def.row, def.col))
                    _palette[def.row, def.col].color = def.color;

            // メイングリッドの中で、目標色も初期色もないマスを Void (空白) に設定
            for (int r = 0; r < MainRows; r++)
            {
                for (int c = 0; c < MainCols; c++)
                {
                    var cell = _main[r, c];
                    if (cell.goalColor == GemColor.None && cell.color == GemColor.None)
                    {
                        cell.isVoid = true;
                    }
                }
            }

            _initMain    = CloneGrid(_main);
            _initPalette = CloneGrid(_palette);
        }

        // ---- タップ操作 ----

        public void OnMainCellTapped(int row, int col)
            => HandleTap(row, col, isPalette: false);

        public void OnPaletteCellTapped(int row, int col)
            => HandleTap(row, col, isPalette: true);

        private void HandleTap(int row, int col, bool isPalette)
        {
            // ゲームの状態が Playing でない場合は操作をブロック（一時停止時などの入力制限）
            if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing) return;

            var cell = GetCell(row, col, isPalette);
            if (cell.isVoid) return; // 空白マスはタップを無視

            if (_selectedGroup == null)
            {
                // 選択フェーズ: ジェムがある場合のみ Flood Fill（空きマスは無視）
                if (cell.IsEmpty || cell.isLocked) return;

                var group = FloodFill(row, col, isPalette);
                if (group == null || group.cells.Count == 0) return;

                _selectedGroup = group;
                OnGroupSelected?.Invoke(_selectedGroup);
                if (SoundManager.Instance != null) SoundManager.Instance.PlaySelect(); // 選択SE再生
            }
            else
            {
                if (cell.IsEmpty)
                {
                    // 移動先タップ
                    TryMoveGroup(_selectedGroup, row, col, isPalette);
                }
                else
                {
                    // 選択中グループのセルをタップ → 選択解除
                    bool isSameGroup = _selectedGroup.cells
                        .Any(e => e.pos == new Vector2Int(row, col) && e.isPalette == isPalette);

                    if (isSameGroup)
                    {
                        _selectedGroup = null;
                        OnGroupSelected?.Invoke(null);
                    }
                    else
                    {
                        // 別のジェムをタップ → そのジェムのグループに切り替え
                        var group = FloodFill(row, col, isPalette);
                        if (group != null && group.cells.Count > 0)
                        {
                            _selectedGroup = group;
                            OnGroupSelected?.Invoke(_selectedGroup);
                            if (SoundManager.Instance != null) SoundManager.Instance.PlaySelect(); // 選択SE再生
                        }
                    }
                }
            }
        }

        // ---- Flood Fill（8方向連結成分） ----

        /// <summary>
        /// (row, col) を起点に 8方向の同色連結成分を返す。
        /// 同色でも繋がっていないセルは含まない。
        /// グリッドをまたぐ（メイン←→パレット）連結は行わない。
        /// </summary>
        private GemGroup FloodFill(int startRow, int startCol, bool isPalette)
        {
            GemCell[,] grid = isPalette ? _palette : _main;
            GemColor targetColor = grid[startRow, startCol].color;

            // 空きマス、または既に正解配置されているマスはFloodFillしない（除外する）
            if (targetColor == GemColor.None) return null;
            if (!isPalette && grid[startRow, startCol].IsCorrect) return null;

            var group = new GemGroup((int)targetColor, targetColor);

            if (isPalette)
            {
                // パレットの場合は、同じ色のすべてのジェムを1次元的に連続しているものとしてすべて選択する
                for (int r = 0; r < PaletteRows; r++)
                {
                    for (int c = 0; c < PaletteCols; c++)
                    {
                        if (_palette[r, c].color == targetColor)
                        {
                            group.AddCell(new Vector2Int(r, c), true);
                        }
                    }
                }
                return group;
            }

            // メイングリッドの場合は8方向 Flood Fill
            var visited = new HashSet<Vector2Int>();
            var queue   = new Queue<Vector2Int>();

            var start = new Vector2Int(startRow, startCol);
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var pos = queue.Dequeue();
                group.AddCell(pos, isPalette);

                foreach (var dir in Dirs8)
                {
                    var next = pos + dir;
                    if (visited.Contains(next)) continue;

                    bool inBounds = InMainBounds(next.x, next.y);
                    if (!inBounds) continue;

                    var neighbor = _main[next.x, next.y];
                    // 同色であり、かつ正解位置にまだ入っていないマスのみ連結対象とする
                    if (neighbor.color == targetColor && !neighbor.IsCorrect)
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }

            return group;
        }

        // ---- 移動ロジック ----

        private bool TryMoveGroup(GemGroup group, int tapRow, int tapCol, bool targetIsPalette)
        {
            var pivot = new Vector2Int(tapRow, tapCol);

            // 部分移動を許可するため、見つかった分の空きマス（最大グループサイズ個）を取得
            var targets = FindContiguousEmpty(pivot, group.cells.Count, targetIsPalette, group);

            if (targets == null || targets.Count == 0)
            {
                _selectedGroup = null;
                OnGroupSelected?.Invoke(null);
                return false;
            }

            int moveCount = targets.Count;

            // Undo に積む（移動前のメイン、パレット状態、および移動前の選択グループ全体）
            _undoStack.Push((CloneGrid(_main), CloneGrid(_palette), group));

            // 移動ステップ情報の構築（アニメーション用）
            var steps = new List<MoveStep>();
            for (int i = 0; i < moveCount; i++)
            {
                steps.Add(new MoveStep
                {
                    fromPos = group.cells[i].pos,
                    fromIsPalette = group.cells[i].isPalette,
                    toPos = targets[i],
                    toIsPalette = targetIsPalette
                });
            }

            // 元位置を空に（移動する個数分だけ）
            for (int i = 0; i < moveCount; i++)
            {
                var entry = group.cells[i];
                var g = entry.isPalette ? _palette : _main;
                g[entry.pos.x, entry.pos.y].color   = GemColor.None;
                g[entry.pos.x, entry.pos.y].groupId = -1;
            }

            // 新位置に配置
            var dstGrid = targetIsPalette ? _palette : _main;
            foreach (var pos in targets)
            {
                dstGrid[pos.x, pos.y].color   = group.color;
                dstGrid[pos.x, pos.y].groupId = group.groupId;
            }

            // パレットの自動整列（左詰め）
            PackPalette();

            // 部分配置後の選択継続チェック
            if (moveCount < group.cells.Count)
            {
                // 残りのジェムがある場合は選択状態を維持（新たなグループを再構成）
                var remainingGroup = new GemGroup(group.groupId, group.color);
                
                // メイングリッドに残っているジェム（ソケット移動しないもの）を追加
                for (int i = moveCount; i < group.cells.Count; i++)
                {
                    var entry = group.cells[i];
                    if (!entry.isPalette)
                    {
                        remainingGroup.AddCell(entry.pos, false);
                    }
                }

                // パレットに残っているジェムは、PackPalette()によって座標が左詰めに動いているため、
                // PackPalette()後のパレット内から同じgroupId/colorのセルを見つけ出して登録する
                int remainingPaletteCount = 0;
                for (int i = moveCount; i < group.cells.Count; i++)
                {
                    if (group.cells[i].isPalette) remainingPaletteCount++;
                }

                if (remainingPaletteCount > 0)
                {
                    int foundCount = 0;
                    for (int r = 0; r < PaletteRows; r++)
                    {
                        for (int c = 0; c < PaletteCols; c++)
                        {
                            if (_palette[r, c].color == group.color)
                            {
                                remainingGroup.AddCell(new Vector2Int(r, c), true);
                                foundCount++;
                                if (foundCount >= remainingPaletteCount)
                                    break;
                            }
                        }
                        if (foundCount >= remainingPaletteCount)
                            break;
                    }
                }

                _selectedGroup = remainingGroup;
                OnGroupSelected?.Invoke(_selectedGroup);
            }
            else
            {
                _selectedGroup = null;
                OnGroupSelected?.Invoke(null);
            }

            OnGroupMoved?.Invoke(steps, group.color);

            // クリア判定
            if (!targetIsPalette) CheckColorCompletion(group.color);
            CheckStageCleared();
            return true;
        }

        private void PackPalette()
        {
            var gems = new List<(GemColor color, int groupId)>();
            for (int r = 0; r < PaletteRows; r++)
            {
                for (int c = 0; c < PaletteCols; c++)
                {
                    if (!_palette[r, c].IsEmpty)
                    {
                        gems.Add((_palette[r, c].color, _palette[r, c].groupId));
                    }
                }
            }

            // パレット内のジェムを GemColor (enumの定義順) で自動ソート
            gems = gems.OrderBy(g => (int)g.color).ToList();

            for (int r = 0; r < PaletteRows; r++)
            {
                for (int c = 0; c < PaletteCols; c++)
                {
                    _palette[r, c].color = GemColor.None;
                    _palette[r, c].groupId = -1;
                }
            }

            int index = 0;
            for (int r = 0; r < PaletteRows; r++)
            {
                for (int c = 0; c < PaletteCols; c++)
                {
                    if (index >= gems.Count) return;
                    _palette[r, c].color = gems[index].color;
                    _palette[r, c].groupId = gems[index].groupId;
                    index++;
                }
            }
        }

        private List<Vector2Int> FindContiguousEmpty(
            Vector2Int pivot, int size, bool targetIsPalette, GemGroup group)
        {
            var grid = targetIsPalette ? _palette : _main;
            int rows = grid.GetLength(0), cols = grid.GetLength(1);

            bool srcSameGrid = group.cells.Count > 0
                               && group.cells[0].isPalette == targetIsPalette;
            var srcSet = srcSameGrid
                ? new HashSet<Vector2Int>(group.cells.Select(e => e.pos))
                : new HashSet<Vector2Int>();

            // タップしたセルの目標色
            GemColor pivotGoalColor = grid[pivot.x, pivot.y].goalColor;
            bool isFromPalette = group.cells.Count > 0 && group.cells[0].isPalette;

            bool IsAvailable(Vector2Int p)
            {
                if (p.x < 0 || p.x >= rows || p.y < 0 || p.y >= cols) return false;

                bool isFree = grid[p.x, p.y].IsEmpty || srcSet.Contains(p);
                if (!isFree) return false;

                // パレットからメインに戻す際のみ、同色の目標マスにのみ配置できるように制限
                if (!targetIsPalette && isFromPalette)
                {
                    if (grid[p.x, p.y].goalColor != group.color)
                    {
                        return false;
                    }
                }

                // メイングリッドでタップ位置に目標色がある場合、同じ目標色のマスのみに連結を制限する
                if (!targetIsPalette && pivotGoalColor != GemColor.None)
                {
                    return grid[p.x, p.y].goalColor == pivotGoalColor;
                }

                return true;
            }

            if (!IsAvailable(pivot)) return null;

            // 8方向BFSで連結している空きマスを探索して確保
            var result = new List<Vector2Int> { pivot };
            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int> { pivot };
            queue.Enqueue(pivot);

            int[] dx = { -1, -1, -1,  0, 0,  1, 1, 1 };
            int[] dy = { -1,  0,  1, -1, 1, -1, 0, 1 };

            while (queue.Count > 0 && result.Count < size)
            {
                var curr = queue.Dequeue();

                for (int i = 0; i < 8; i++)
                {
                    var next = new Vector2Int(curr.x + dx[i], curr.y + dy[i]);
                    if (IsAvailable(next) && !visited.Contains(next))
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                        result.Add(next);

                        if (result.Count == size)
                            break;
                    }
                }
            }

            return result.Count > 0 ? result : null;
        }

        // ---- クリア判定 ----

        /// <summary>指定した色のジェムが全てメイングリッドの正位置に揃ったか確認</summary>
        private void CheckColorCompletion(GemColor color)
        {
            // メイングリッド全体を走査して、この色に関わるセルを確認
            bool allCorrect = true;
            bool anyGoal    = false;

            for (int r = 0; r < MainRows; r++)
            {
                for (int c = 0; c < MainCols; c++)
                {
                    var cell = _main[r, c];
                    if (cell.goalColor != color) continue;
                    anyGoal = true;
                    if (!cell.IsCorrect) { allCorrect = false; break; }
                }
                if (!allCorrect) break;
            }

            if (anyGoal && allCorrect)
                OnColorCompleted?.Invoke(color);
        }

        private void CheckStageCleared()
        {
            // メイン: goalColor のある全マスが IsCorrect
            for (int r = 0; r < MainRows; r++)
                for (int c = 0; c < MainCols; c++)
                    if (_main[r, c].HasGoal && !_main[r, c].IsCorrect) return;

            // パレット: 全空き
            for (int r = 0; r < PaletteRows; r++)
                for (int c = 0; c < PaletteCols; c++)
                    if (!_palette[r, c].IsEmpty) return;

            OnStageCleared?.Invoke();
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) return;
            GemGroup prevGroup;
            (_main, _palette, prevGroup) = _undoStack.Pop();
            
            // 選択状態を移動前のグループに戻す
            _selectedGroup = prevGroup;
            OnGroupSelected?.Invoke(_selectedGroup);
            
            OnGroupMoved?.Invoke(null, GemColor.None);
        }

        public void Reset()
        {
            if (_initMain == null || _initPalette == null) return;
            _main    = CloneGrid(_initMain);
            _palette = CloneGrid(_initPalette);
            _undoStack.Clear();
            _selectedGroup = null;
            OnGroupMoved?.Invoke(null, GemColor.None);
        }

        // ---- データアクセス ----

        public GemCell GetMainCell(int row, int col)    => _main[row, col];
        public GemCell GetPaletteCell(int row, int col) => _palette[row, col];

        private GemCell GetCell(int row, int col, bool isPalette)
            => isPalette ? _palette[row, col] : _main[row, col];

        // ---- ユーティリティ ----

        private bool InMainBounds(int r, int c)
            => r >= 0 && r < MainRows && c >= 0 && c < MainCols;
        private bool InPaletteBounds(int r, int c)
            => r >= 0 && r < PaletteRows && c >= 0 && c < PaletteCols;

        private GemCell[,] CloneGrid(GemCell[,] src)
        {
            int rows = src.GetLength(0), cols = src.GetLength(1);
            var dst = new GemCell[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    dst[r, c] = src[r, c].Clone();
            return dst;
        }

        /// <summary>指定した空きマスから繋がっている連続空きマスの総数を返す（8方向BFS）</summary>
        public int GetContiguousEmptyCount(int startRow, int startCol, bool isPalette, GemGroup group)
        {
            var grid = isPalette ? _palette : _main;
            int rows = grid.GetLength(0), cols = grid.GetLength(1);

            bool srcSameGrid = group != null && group.cells.Count > 0
                               && group.cells[0].isPalette == isPalette;
            var srcSet = srcSameGrid
                ? new HashSet<Vector2Int>(group.cells.Select(e => e.pos))
                : new HashSet<Vector2Int>();

            bool IsAvailable(Vector2Int p)
            {
                if (p.x < 0 || p.x >= rows || p.y < 0 || p.y >= cols) return false;
                return grid[p.x, p.y].IsEmpty || srcSet.Contains(p);
            }

            var start = new Vector2Int(startRow, startCol);
            if (!IsAvailable(start)) return 0;

            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int> { start };
            queue.Enqueue(start);

            int[] dx = { -1, -1, -1,  0, 0,  1, 1, 1 };
            int[] dy = { -1,  0,  1, -1, 1, -1, 0, 1 };

            while (queue.Count > 0)
            {
                var curr = queue.Dequeue();
                for (int i = 0; i < 8; i++)
                {
                    var next = new Vector2Int(curr.x + dx[i], curr.y + dy[i]);
                    if (IsAvailable(next) && !visited.Contains(next))
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }

            return visited.Count;
        }
    }
}
