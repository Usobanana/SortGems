// GridView.cs — メイングリッド＋パレットの2エリアビュー管理
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SortGems.Core
{
    /// <summary>
    /// GridManager の状態を受けてビュー全体を更新する。
    /// メイングリッドとパレットそれぞれに GridLayoutGroup を持つ。
    /// </summary>
    public class GridView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager _gridManager;

        [Header("Main Grid")]
        [SerializeField] private GridLayoutGroup _mainLayout;
        [SerializeField] private float _mainCellSize  = 36f;
        [SerializeField] private float _mainCellSpacing = 2f;

        [Header("Palette")]
        [SerializeField] private GridLayoutGroup _paletteLayout;
        [SerializeField] private float _paletteCellSize  = 44f;
        [SerializeField] private float _paletteCellSpacing = 4f;

        [Header("Prefab")]
        [SerializeField] private GemCellView _cellPrefab;

        private GemCellView[,] _mainViews;
        private GemCellView[,] _paletteViews;

        [Header("Move Animation Settings")]
        [SerializeField] private float _moveDuration = 0.15f;
        [SerializeField] private float _delayBetweenGems = 0.04f;

        private bool _isAnimating = false;

        private void Awake()
        {
            if (_gridManager == null)
            {
                Debug.LogError("[GridView] _gridManager がアサインされていません。Tools > SortGems > Create Game Scene を実行してシーンを再構築してください。");
                return;
            }
            _gridManager.OnGroupSelected  += HandleGroupSelected;
            _gridManager.OnGroupMoved     += HandleGroupMoved;
            _gridManager.OnColorCompleted += HandleColorCompleted;
            _gridManager.OnStageCleared   += HandleStageCleared;
        }

        // ---- 構築 ----

        public void BuildGrid(StageData stage)
        {
            if (_gridManager == null) return;

            BuildArea(_mainLayout,    stage.mainRows,    stage.mainCols,
                      _mainCellSize, _mainCellSpacing,
                      isPalette: false, out _mainViews);

            BuildArea(_paletteLayout, stage.paletteRows, stage.paletteCols,
                      _paletteCellSize, _paletteCellSpacing,
                      isPalette: true,  out _paletteViews);

            RefreshAll();
        }

        private void BuildArea(GridLayoutGroup layout, int rows, int cols,
                               float cellSize, float spacing, bool isPalette,
                               out GemCellView[,] views)
        {
            foreach (Transform child in layout.transform)
                Destroy(child.gameObject);

            layout.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = cols;
            layout.cellSize        = new Vector2(cellSize, cellSize);
            layout.spacing         = new Vector2(spacing, spacing);

            views = new GemCellView[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var view = Instantiate(_cellPrefab, layout.transform);
                    view.Setup(r, c, isPalette);

                    int row = r, col = c;
                    view.OnTapped += _ =>
                    {
                        if (_isAnimating) return; // アニメーション中は操作をブロック
                        if (isPalette) _gridManager.OnPaletteCellTapped(row, col);
                        else           _gridManager.OnMainCellTapped(row, col);
                    };

                    views[r, c] = view;
                }
            }
        }

        // ---- イベントハンドラ ----

        private void HandleGroupSelected(GemGroup group)
        {
            ClearAllHighlights();
            if (group == null) return;

            foreach (var entry in group.cells)
            {
                var views = entry.isPalette ? _paletteViews : _mainViews;
                views[entry.pos.x, entry.pos.y].SetSelected(true);
            }

            // 空きマスのハイライトは通常選択時には表示しない（ヒントボタン連動にするためコメントアウト）
            // HighlightEmpties(_mainViews,    isPalette: false, group);
        }

        private void HandleGroupMoved(System.Collections.Generic.List<MoveStep> steps, GemColor color)
        {
            if (steps == null || steps.Count == 0)
            {
                // Undo/Reset 時などはアニメーションなしで即時更新
                RefreshAll();
                ClearAllHighlights();
            }
            else
            {
                StartCoroutine(PlayMoveAnimation(steps, color));
            }
        }

        private void HandleColorCompleted(GemColor color)
        {
            // その色の全メインセルに完成エフェクト
            int rows = _mainViews.GetLength(0), cols = _mainViews.GetLength(1);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var cell = _gridManager.GetMainCell(r, c);
                    if (cell.goalColor == color)
                        _mainViews[r, c].PlayCompletedEffect();
                }
        }

        private void HandleStageCleared()
        {
            int rows = _mainViews.GetLength(0), cols = _mainViews.GetLength(1);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    StartCoroutine(DelayedEffect((r + c) * 0.03f, _mainViews[r, c]));
        }

        // ---- ヘルパー ----

        private void RefreshAll()
        {
            RefreshArea(_mainViews,    isPalette: false);
            RefreshArea(_paletteViews, isPalette: true);
        }

        private void RefreshArea(GemCellView[,] views, bool isPalette)
        {
            if (views == null) return;
            int rows = views.GetLength(0), cols = views.GetLength(1);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var cell = isPalette
                        ? _gridManager.GetPaletteCell(r, c)
                        : _gridManager.GetMainCell(r, c);
                    views[r, c].SetGem(cell.color, cell.goalColor, cell.isVoid);
                }
        }

        private void ClearAllHighlights()
        {
            ClearHighlight(_mainViews);
            ClearHighlight(_paletteViews);
        }

        private void ClearHighlight(GemCellView[,] views)
        {
            if (views == null) return;
            int rows = views.GetLength(0), cols = views.GetLength(1);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    views[r, c].SetSelected(false);
                    views[r, c].SetHighlighted(false);
                }
        }

        private void HighlightEmpties(GemCellView[,] views, bool isPalette, GemGroup selectedGroup)
        {
            if (views == null || selectedGroup == null) return;
            int rows = views.GetLength(0), cols = views.GetLength(1);
            GemColor selectedColor = selectedGroup.color;

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var cell = isPalette
                        ? _gridManager.GetPaletteCell(r, c)
                        : _gridManager.GetMainCell(r, c);

                    // パレットは除外。メイングリッド上の、選択されたジェムと同じ目標色の空きマスのみハイライト
                    if (cell.IsEmpty && !isPalette && cell.goalColor == selectedColor)
                    {
                        // 部分移動を許可するため、連結する空きマスが1つ以上あればハイライト
                        int availableCount = _gridManager.GetContiguousEmptyCount(r, c, isPalette, selectedGroup);
                        if (availableCount > 0)
                        {
                            views[r, c].SetHighlighted(true);
                        }
                    }
                }
        }

        private IEnumerator DelayedEffect(float delay, GemCellView view)
        {
            yield return new WaitForSeconds(delay);
            view.PlayCompletedEffect();
        }

        private IEnumerator PlayMoveAnimation(System.Collections.Generic.List<MoveStep> steps, GemColor color)
        {
            _isAnimating = true;
            ClearAllHighlights();

            var activeTweens = new System.Collections.Generic.List<Coroutine>();
            foreach (var step in steps)
            {
                var fromViews = step.fromIsPalette ? _paletteViews : _mainViews;
                var toViews   = step.toIsPalette ? _paletteViews : _mainViews;

                var fromCell = fromViews[step.fromPos.x, step.fromPos.y];
                var toCell   = toViews[step.toPos.x, step.toPos.y];

                // 移動元の表示を一旦クリア（ダミーの飛行ジェムが見えるため）
                var origCell = step.fromIsPalette
                    ? _gridManager.GetPaletteCell(step.fromPos.x, step.fromPos.y)
                    : _gridManager.GetMainCell(step.fromPos.x, step.fromPos.y);
                fromCell.SetGem(GemColor.None, origCell.goalColor);

                var co = StartCoroutine(AnimateSingleGem(fromCell.transform.position, toCell.transform.position, color));
                activeTweens.Add(co);

                yield return new WaitForSeconds(_delayBetweenGems);
            }

            foreach (var co in activeTweens)
            {
                yield return co;
            }

            _isAnimating = false;
            RefreshAll();
        }

        private IEnumerator AnimateSingleGem(Vector3 startWorldPos, Vector3 endWorldPos, GemColor color)
        {
            var dummyGo = new GameObject("DummyGem");
            dummyGo.SetActive(false); // 生成直後に非アクティブ化して描画をブロック
            
            var rectTrans = dummyGo.AddComponent<RectTransform>();
            rectTrans.sizeDelta = new Vector2(_mainCellSize, _mainCellSize);

            // GridLayoutGroupの自動整列対象から除外する
            var layoutElement = dummyGo.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            var img = dummyGo.AddComponent<Image>();
            img.color = GemColorPalette.GetColor(color);

            dummyGo.transform.SetParent(transform, false);

            dummyGo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            dummyGo.transform.localScale = new Vector3(0.62f, 0.62f, 1f);

            dummyGo.transform.position = startWorldPos;
            dummyGo.SetActive(true); // 全設定完了後に表示開始

            float elapsed = 0f;
            while (elapsed < _moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _moveDuration);
                t = t * (2f - t); // EaseOutQuad
                dummyGo.transform.position = Vector3.Lerp(startWorldPos, endWorldPos, t);
                yield return null;
            }

            dummyGo.transform.position = endWorldPos;

            // 目的地にハマった瞬間にSEとバイブレーション（実機のみ）をトリガー
            if (SoundManager.Instance != null) SoundManager.Instance.PlayPlace();
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
            Handheld.Vibrate();
#endif

            Destroy(dummyGo);
        }

        /// <summary>
        /// ヒント表示: 条件に合致するマスを白い枠線でふわふわと明滅させる
        /// </summary>
        public void ShowHint(GemGroup selectedGroup)
        {
            ClearAllHighlights();

            int rows = _mainViews.GetLength(0), cols = _mainViews.GetLength(1);

            if (selectedGroup == null)
            {
                // ジェム未選択時: メイングリッド上のすべての空きマス（目標色がある場所）をハイライト
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        var cell = _gridManager.GetMainCell(r, c);
                        if (cell.IsEmpty && cell.HasGoal)
                        {
                            _mainViews[r, c].SetHighlighted(true);
                        }
                    }
                }
            }
            else
            {
                // ジェム選択時: 選択ジェムと同色のメイングリッド上の空きマスをハイライト
                GemColor selectedColor = selectedGroup.color;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        var cell = _gridManager.GetMainCell(r, c);
                        if (cell.IsEmpty && cell.goalColor == selectedColor)
                        {
                            // 部分移動を許可するため、連結する空きマスが1つ以上あればハイライト
                            int availableCount = _gridManager.GetContiguousEmptyCount(r, c, false, selectedGroup);
                            if (availableCount > 0)
                            {
                                _mainViews[r, c].SetHighlighted(true);
                            }
                        }
                    }
                }
            }
        }
    }
}
