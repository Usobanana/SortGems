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
        [SerializeField] private float _mainCellSpacing = 0f;

        [Header("Palette")]
        [SerializeField] private GridLayoutGroup _paletteLayout;
        [SerializeField] private float _paletteCellSpacing = 0f;

        [Header("Layout Settings")]
        [SerializeField] private float _maxMainCellSize = 45f;
        [SerializeField] private float _maxPaletteCellSize = 75f;

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

        private float _currentCellSize = 41.5f;
        private float _currentPaletteCellSize = 75f;

        public void BuildGrid(StageData stage)
        {
            if (_gridManager == null) return;

            // 1. 画面サイズ（幅・高さ）の動的取得
            float canvasWidth = 1080f;
            float canvasHeight = 1920f;
            var parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                var canvasRt = parentCanvas.GetComponent<RectTransform>();
                if (canvasRt != null)
                {
                    canvasWidth = canvasRt.rect.width;
                    canvasHeight = canvasRt.rect.height;
                }
            }

            float paddingVal = 16f; // コンテナ内余白

            // メイングリッドのセルサイズ計算 (左右マージン40fとパディングを考慮)
            float availableWidth = canvasWidth - 40f - (paddingVal * 2f);
            float calculatedSize = availableWidth / stage.mainCols;
            float roundedSize = Mathf.Floor(calculatedSize);
            _currentCellSize = Mathf.Min(roundedSize, _maxMainCellSize);

            // パレットのセルサイズ計算 (左右マージン40fとパディングを考慮)
            float availablePalWidth = canvasWidth - 40f - (paddingVal * 2f);
            float calculatedPaletteSize = availablePalWidth / stage.paletteCols;
            float roundedPaletteSize = Mathf.Floor(calculatedPaletteSize);
            _currentPaletteCellSize = Mathf.Min(roundedPaletteSize, _maxPaletteCellSize);

            // 2. コンテナ全体の正確な幅・高さを計算
            float mainW = stage.mainCols * _currentCellSize + (stage.mainCols - 1) * _mainCellSpacing + (paddingVal * 2f);
            float mainH = stage.mainRows * _currentCellSize + (stage.mainRows - 1) * _mainCellSpacing + (paddingVal * 2f);

            float palW = stage.paletteCols * _currentPaletteCellSize + (stage.paletteCols - 1) * _paletteCellSpacing + (paddingVal * 2f);
            float palH = stage.paletteRows * _currentPaletteCellSize + (stage.paletteRows - 1) * _paletteCellSpacing + (paddingVal * 2f);

            // 3. 有効な縦領域内での動的センタリング配置 (重なり防止)
            float headerH = 160f; // ヘッダー領域の高さ
            float buttonH = 140f; // 下部ボタン領域の高さ
            float availableH = canvasHeight - headerH - buttonH;

            float gap = 32f; // メインとパレットの隙間
            float totalContentH = mainH + gap + palH;

            float margin = (availableH - totalContentH) / 2f;
            margin = Mathf.Max(margin, 20f); // 最低20pxの余白を確保

            float mainTopY = -headerH - margin;
            float palTopY = mainTopY - mainH - gap;

            // メイングリッドコンテナのRectTransform調整
            var mainRt = _mainLayout.GetComponent<RectTransform>();
            mainRt.anchorMin = new Vector2(0.5f, 1f);
            mainRt.anchorMax = new Vector2(0.5f, 1f);
            mainRt.pivot = new Vector2(0.5f, 1f);
            mainRt.anchoredPosition = new Vector2(0f, mainTopY);
            mainRt.sizeDelta = new Vector2(mainW, mainH);

            // パレットコンテナのRectTransform調整
            var palRt = _paletteLayout.GetComponent<RectTransform>();
            palRt.anchorMin = new Vector2(0.5f, 1f);
            palRt.anchorMax = new Vector2(0.5f, 1f);
            palRt.pivot = new Vector2(0.5f, 1f);
            palRt.anchoredPosition = new Vector2(0f, palTopY);
            palRt.sizeDelta = new Vector2(palW, palH);

            // 4. コンテナ背景（区切り枠）の設定（シンプルな矩形に変更）
            var mainImg = _mainLayout.GetComponent<Image>();
            if (mainImg == null) mainImg = _mainLayout.gameObject.AddComponent<Image>();
            mainImg.sprite = null; // スプライトなし（シンプルな矩形）
            mainImg.color = new Color(0.08f, 0.08f, 0.1f, 0.35f); // 半透明ダークグレー

            var palImg = _paletteLayout.GetComponent<Image>();
            if (palImg == null) palImg = _paletteLayout.gameObject.AddComponent<Image>();
            palImg.sprite = null; // スプライトなし（シンプルな矩形）
            palImg.color = new Color(0.08f, 0.08f, 0.1f, 0.55f); // やや濃い半透明ダークグレー

            // 5. レイアウトパディングの適用
            int pad = Mathf.RoundToInt(paddingVal);
            _mainLayout.padding = new RectOffset(pad, pad, pad, pad);
            _paletteLayout.padding = new RectOffset(pad, pad, pad, pad);

            // 6. 各エリアのセルを構築
            BuildArea(_mainLayout,    stage.mainRows,    stage.mainCols,
                      _currentCellSize, _mainCellSpacing,
                      isPalette: false, out _mainViews);

            BuildArea(_paletteLayout, stage.paletteRows, stage.paletteCols,
                      _currentPaletteCellSize, _paletteCellSpacing,
                      isPalette: true,  out _paletteViews);

            RefreshAll();
        }

        private void AdjustContainerLayout(RectTransform containerRt, int cols, int rows, float cellSize, float spacing)
        {
            if (containerRt == null) return;

            // 上中央アンカー/ピボットに設定し、X=0 にして完全にセンタリング
            containerRt.anchorMin = new Vector2(0.5f, 1f);
            containerRt.anchorMax = new Vector2(0.5f, 1f);
            containerRt.pivot = new Vector2(0.5f, 1f);
            containerRt.anchoredPosition = new Vector2(0f, containerRt.anchoredPosition.y);

            // 正確な幅・高さを適用
            float w = cols * cellSize + (cols - 1) * spacing;
            float h = rows * cellSize + (rows - 1) * spacing;
            containerRt.sizeDelta = new Vector2(w, h);
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
                    view.OnTappedWithEvent += (v, eventData) =>
                    {
                        if (_isAnimating) return; // アニメーション中は操作をブロック
                        
                        // タップ位置に基づいて最も適切な優先セルに補正（吸い寄せ）
                        GemCellView correctedView = CorrectTapTarget(v, eventData);
                        
                        if (correctedView.IsPalette) _gridManager.OnPaletteCellTapped(correctedView.Row, correctedView.Col);
                        else           _gridManager.OnMainCellTapped(correctedView.Row, correctedView.Col);
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
            {
                for (int c = 0; c < cols; c++)
                {
                    var cell = _gridManager.GetMainCell(r, c);
                    // Voidマス、または空きマスは演出から除外する
                    if (cell.isVoid || cell.IsEmpty) continue;

                    StartCoroutine(DelayedEffect((r + c) * 0.03f, _mainViews[r, c]));
                }
            }
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

                float startSize = step.fromIsPalette ? _currentPaletteCellSize : _currentCellSize;
                float endSize   = step.toIsPalette   ? _currentPaletteCellSize : _currentCellSize;

                var co = StartCoroutine(AnimateSingleGem(fromCell.transform.position, toCell.transform.position, color, startSize, endSize));
                activeTweens.Add(co);

                yield return new WaitForSeconds(_delayBetweenGems);
            }

            foreach (var co in activeTweens)
            {
                yield return co;
            }

            _isAnimating = false;
            RefreshAll();

            // 部分移動によって残った選択中グループがあれば、選択状態の表示を復元する
            if (_gridManager != null && _gridManager.SelectedGroup != null)
            {
                HandleGroupSelected(_gridManager.SelectedGroup);
            }
        }

        private IEnumerator AnimateSingleGem(Vector3 startWorldPos, Vector3 endWorldPos, GemColor color, float startSize, float endSize)
        {
            // プレハブをベースに複製することで、移動中のビジュアル不一致や比率の崩れを完全に防ぐ
            var dummyCell = Instantiate(_cellPrefab, transform);
            dummyCell.gameObject.name = "DummyGem";
            dummyCell.gameObject.SetActive(false); // 設定が完了するまで非表示

            // 不要な入力を防ぐためコンポーネントを無効化・削除
            var button = dummyCell.GetComponent<Button>();
            if (button != null) Destroy(button);
            var trigger = dummyCell.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger != null) Destroy(trigger);

            // GridLayoutGroupの自動整列対象から除外する
            var layoutElement = dummyCell.gameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            // 移動中は背景マスを非表示にする（ジェムのみが飛ぶ表現）
            var bgTrans = dummyCell.transform.Find("BackgroundImage");
            if (bgTrans != null) bgTrans.gameObject.SetActive(false);

            // サイズを初期サイズに設定
            var rectTrans = dummyCell.GetComponent<RectTransform>();
            rectTrans.sizeDelta = new Vector2(startSize, startSize);

            // ジェムの描画を設定
            dummyCell.SetGem(color, GemColor.None);

            dummyCell.transform.position = startWorldPos;
            dummyCell.gameObject.SetActive(true); // 表示開始

            float elapsed = 0f;
            while (elapsed < _moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _moveDuration);
                t = t * (2f - t); // EaseOutQuad
                
                // 位置の移動
                dummyCell.transform.position = Vector3.Lerp(startWorldPos, endWorldPos, t);
                
                // サイズの補間
                float currentSize = Mathf.Lerp(startSize, endSize, t);
                rectTrans.sizeDelta = new Vector2(currentSize, currentSize);
                
                yield return null;
            }

            dummyCell.transform.position = endWorldPos;

            // 目的地にハマった瞬間にSEとバイブレーション（実機のみ）をトリガー
            if (SoundManager.Instance != null) SoundManager.Instance.PlayPlace();
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
            Handheld.Vibrate();
#endif

            Destroy(dummyCell.gameObject);
        }

        /// <summary>
        /// タップ位置（スクリーン座標）に基づいて、操作を優先したいマス（目標色が一致する空きマスなど）へ補正する。
        /// </summary>
        private GemCellView CorrectTapTarget(GemCellView originalView, UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_gridManager == null) return originalView;

            // UGUIのイベントデータからタップ位置を取得、取得できない場合は補正しない
            if (eventData == null) return originalView;
            Vector2 tapPos = eventData.position;

            GemGroup selected = _gridManager.SelectedGroup;
            GemCellView bestCandidate = originalView;
            float bestDistance = float.MaxValue;

            // スクリーン上での基本セルサイズ（幅）を算出
            float screenCellSize = _currentCellSize;
            var rectTrans = originalView.GetComponent<RectTransform>();
            if (rectTrans != null)
            {
                Vector3[] corners = new Vector3[4];
                rectTrans.GetWorldCorners(corners);
                screenCellSize = Vector2.Distance(corners[0], corners[1]); // 左下から左上への距離 ＝ 高さ（幅と同じ）
            }

            // 吸い寄せのしきい値：セルサイズの1.3倍の距離
            float threshold = screenCellSize * 1.3f;

            // 1. メイングリッドの全セルから候補を探す
            if (_mainViews != null)
            {
                int rows = _mainViews.GetLength(0);
                int cols = _mainViews.GetLength(1);
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        var view = _mainViews[r, c];
                        if (view == null) continue;
                        var cell = _gridManager.GetMainCell(r, c);
                        if (cell.isVoid) continue;

                        bool isCandidate = false;
                        if (selected != null)
                        {
                            // ジェム選択中：移動先候補（現在選択中と同色の目標空マス）
                            isCandidate = (cell.IsEmpty && cell.goalColor == selected.color);
                        }
                        else
                        {
                            // ジェム未選択：選択可能ジェム（正解位置にないジェム）
                            isCandidate = (!cell.IsEmpty && !cell.IsCorrect);
                        }

                        if (isCandidate)
                        {
                            Vector2 cellScreenPos = RectTransformUtility.WorldToScreenPoint(null, view.transform.position);
                            float dist = Vector2.Distance(tapPos, cellScreenPos);
                            if (dist < threshold && dist < bestDistance)
                            {
                                bestDistance = dist;
                                bestCandidate = view;
                            }
                        }
                    }
                }
            }

            // 2. パレットグリッドから候補を探す（ジェム未選択時のみ、パレット上のジェムも選択候補にする）
            if (selected == null && _paletteViews != null)
            {
                int rows = _paletteViews.GetLength(0);
                int cols = _paletteViews.GetLength(1);
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        var view = _paletteViews[r, c];
                        if (view == null) continue;
                        var cell = _gridManager.GetPaletteCell(r, c);

                        // パレット上のジェム（空でない）
                        bool isCandidate = !cell.IsEmpty;

                        if (isCandidate)
                        {
                            Vector2 cellScreenPos = RectTransformUtility.WorldToScreenPoint(null, view.transform.position);
                            float dist = Vector2.Distance(tapPos, cellScreenPos);

                            // パレットはセルサイズが大きい場合があるので、パレットセルのサイズでしきい値を計算
                            float palScreenCellSize = screenCellSize;
                            var palRectTrans = view.GetComponent<RectTransform>();
                            if (palRectTrans != null)
                            {
                                Vector3[] corners = new Vector3[4];
                                palRectTrans.GetWorldCorners(corners);
                                palScreenCellSize = Vector2.Distance(corners[0], corners[1]);
                            }
                            float palThreshold = palScreenCellSize * 1.3f;

                            if (dist < palThreshold && dist < bestDistance)
                            {
                                bestDistance = dist;
                                bestCandidate = view;
                            }
                        }
                    }
                }
            }

            return bestCandidate;
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
