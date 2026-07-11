using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using SortGems.Core;
using SortGems.Ads;

namespace SortGems.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        [Header("Screen UXML")]
        [SerializeField] private VisualTreeAsset _titleScreen;
        [SerializeField] private VisualTreeAsset _stageSelectScreen;
        [SerializeField] private VisualTreeAsset _gamePlayHUD;

        [Header("References")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private GridView _gridView;
        [SerializeField] private GridManager _gridManager;

        [Header("Stages")]
        [SerializeField] private List<StageData> _stages = new();

        [Header("uGUI Grid (kept for puzzle rendering)")]
        [SerializeField] private GameObject _uguiGamePlayPanel;

        [Header("Preview")]
        [SerializeField] private GemCellView _cellPrefab;

        [Header("Settings")]
        [SerializeField] private float _addTimeSeconds = 60f;

        private UIDocument _uiDocument;
        private VisualElement _root;

        private int _currentStageIndex;
        private int _carouselPage;

        private enum Screen { Title, StageSelect, GamePlay }
        private Screen _currentScreen;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            _root = _uiDocument.rootVisualElement;

            if (_gameManager != null)
            {
                _gameManager.OnGameCleared.AddListener(ShowClearedPanel);
                _gameManager.OnGameFailed.AddListener(ShowFailedPanel);
                _gameManager.OnTimerUpdated.AddListener(UpdateTimer);
            }

            Debug.Log($"[ScreenManager] Start() called. root={_root != null}, titleScreen={_titleScreen != null}");
            ShowTitle();
            gameObject.AddComponent<DebugMenu>();
        }

        // --- Screen transitions ---

        public void ShowTitle()
        {
            _currentScreen = Screen.Title;
            _root.Clear();
            _titleScreen.CloneTree(_root);

            _root.Q<Button>("btn-play").clicked += ShowStageSelect;

            if (_uguiGamePlayPanel != null) _uguiGamePlayPanel.SetActive(false);

            PauseIfPlaying();
            AdManager.Instance?.HideBanner();
        }

        public void ShowStageSelect()
        {
            _currentScreen = Screen.StageSelect;
            _root.Clear();
            _stageSelectScreen.CloneTree(_root);

            _root.Q<Button>("btn-back").clicked += ShowTitle;

            BuildCarousel();

            if (_uguiGamePlayPanel != null) _uguiGamePlayPanel.SetActive(false);

            PauseIfPlaying();
            AdManager.Instance?.ShowBanner();
        }

        public void ShowGamePlay()
        {
            _currentScreen = Screen.GamePlay;
            _root.Clear();
            _gamePlayHUD.CloneTree(_root);

            // headerH/timerH の「正」は GridView 側にあるので、タイマー予約帯の位置を
            // ここで注入する（GamePlayHUD.uss の top:120px はCanvasが見つからない場合等のフォールバック）。
            if (_gridView != null)
            {
                var timerGroup = _root.Q("timer-group");
                if (timerGroup != null) timerGroup.style.top = _gridView.HeaderH;
            }

            _root.Q<Button>("btn-back").clicked += ShowStageSelect;
            _root.Q<Button>("btn-undo").clicked += () => _gameManager?.Undo();
            _root.Q<Button>("btn-hint").clicked += OnHintClicked;
            _root.Q<Button>("btn-reset").clicked += () =>
            {
                _gameManager?.ResetStage();
                HidePanel("failed-panel");
            };

            _root.Q<Button>("btn-next").clicked += OnNextStageClicked;
            _root.Q<Button>("btn-replay-cleared").clicked += OnReplayClicked;
            _root.Q<Button>("btn-back-cleared").clicked += () => { HidePanel("cleared-panel"); ShowStageSelect(); };
            _root.Q<Button>("btn-replay-failed").clicked += OnReplayClicked;
            _root.Q<Button>("btn-back-failed").clicked += () => { HidePanel("failed-panel"); ShowStageSelect(); };
            _root.Q<Button>("btn-addtime").clicked += OnAddTimeClicked;

            _root.Q<Button>("btn-unlock-item").clicked += OnUnlockByItem;
            _root.Q<Button>("btn-unlock-ad").clicked += OnUnlockByAd;
            _root.Q<Button>("btn-unlock-cancel").clicked += OnUnlockCancel;

            if (_uguiGamePlayPanel != null) _uguiGamePlayPanel.SetActive(true);

            if (_gridView != null)
                _gridView.OnUnlockButtonClicked += ShowUnlockDialog;

            AdManager.Instance?.ShowBanner();
        }

        // --- Stage loading ---

        public void LoadStage(int index)
        {
            if (index < 0 || index >= _stages.Count) return;
            _currentStageIndex = index;
            var stage = _stages[index];

            ShowGamePlay();
            _gameManager?.StartStage(stage);
            _gridView?.BuildGrid(stage);

            var label = _root.Q<Label>("stage-name");
            if (label != null) label.text = stage.stageName;
        }

        public void LoadNextStage()
        {
            int next = _currentStageIndex + 1;
            if (next < _stages.Count)
            {
                if (AdManager.Instance != null)
                    AdManager.Instance.ShowInterstitialWithProbability(() => LoadStage(next));
                else
                    LoadStage(next);
            }
            else
            {
                ShowStageSelect();
            }
        }

        // --- Carousel (virtualized: 3 cards in DOM, swipe-then-recycle) ---

        private int _totalVisible;
        private float _cardWidth = 832f;
        private List<int> _visibleStageIndices = new();
        private VisualElement[] _cardPool;
        private const int PoolSize = 3;
        private Dictionary<int, Texture2D> _previewCache = new();
        private bool _isAnimating;

        private void BuildCarousel()
        {
            _visibleStageIndices.Clear();
            foreach (var tex in _previewCache.Values)
                if (tex != null) Destroy(tex);
            _previewCache.Clear();

            int nextStageIndex = FindNextStageIndex();
            int futureLimit = Mathf.Min(_stages.Count - 1, nextStageIndex + 5);

            for (int i = 0; i <= futureLimit; i++)
                _visibleStageIndices.Add(i);

            _totalVisible = _visibleStageIndices.Count;
            _carouselPage = _visibleStageIndices.IndexOf(nextStageIndex);
            if (_carouselPage < 0) _carouselPage = _totalVisible - 1;

            var content = _root.Q("carousel-content");
            content.Clear();

            _cardPool = new VisualElement[PoolSize];
            for (int p = 0; p < PoolSize; p++)
            {
                var card = new VisualElement();
                card.AddToClassList("stage-card");

                var preview = new VisualElement();
                preview.AddToClassList("stage-card-preview");
                card.Add(preview);

                var label = new Label();
                label.AddToClassList("stage-card-label");
                card.Add(label);

                var statusLabel = new Label();
                statusLabel.AddToClassList("stage-card-status");
                card.Add(statusLabel);

                content.Add(card);
                _cardPool[p] = card;
            }

            var vp = _root.Q("carousel-viewport");
            vp.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                float vpW = evt.newRect.width;
                float pad = Mathf.Max(0, (vpW - 800f) / 2f);
                content.style.paddingLeft = pad;
                content.style.paddingRight = pad;
                UpdatePooledCards();
                SnapToPoolCenter(content, false);
            });

            var btnPrev = _root.Q<Button>("btn-prev");
            var btnNextNav = _root.Q<Button>("btn-next");
            var btnStart = _root.Q<Button>("btn-start");

            var viewport = _root.Q("carousel-viewport");
            float dragStartTranslateX = 0f;
            float swipeStartX = 0f;
            float lastPointerX = 0f;
            float velocity = 0f;
            bool isDragging = false;
            long lastMoveTime = 0;

            viewport.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (_isAnimating) return;
                swipeStartX = evt.position.x;
                lastPointerX = evt.position.x;
                velocity = 0f;
                isDragging = true;
                lastMoveTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                dragStartTranslateX = GetCurrentTranslateX(content);
                content.style.transitionDuration = new List<TimeValue> { new TimeValue(0) };
                viewport.CapturePointer(evt.pointerId);
            });
            viewport.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!isDragging) return;
                float delta = evt.position.x - swipeStartX;
                long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long dt = Mathf.Max(1, (int)(now - lastMoveTime));
                velocity = (evt.position.x - lastPointerX) / (dt / 1000f);
                lastPointerX = evt.position.x;
                lastMoveTime = now;
                content.style.translate = new Translate(dragStartTranslateX + delta, 0);
            });
            viewport.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!isDragging) return;
                isDragging = false;
                viewport.ReleasePointer(evt.pointerId);

                float currentX = GetCurrentTranslateX(content);
                float centerX = -(PoolSize / 2) * _cardWidth;
                float dragDist = currentX - centerX;
                int pageDelta = 0;
                if (Mathf.Abs(velocity) > 400f)
                    pageDelta = velocity < 0 ? 1 : -1;
                else if (Mathf.Abs(dragDist) > _cardWidth * 0.3f)
                    pageDelta = dragDist < 0 ? 1 : -1;

                int newPage = Mathf.Clamp(_carouselPage + pageDelta, 0, _totalVisible - 1);
                NavigateToPage(newPage, content);
            });

            btnPrev.clicked += () => { if (!_isAnimating && _carouselPage > 0) NavigateToPage(_carouselPage - 1, content); };
            btnNextNav.clicked += () => { if (!_isAnimating && _carouselPage < _totalVisible - 1) NavigateToPage(_carouselPage + 1, content); };
            btnStart.clicked += () =>
            {
                if (_isAnimating) return;
                int stageIdx = _visibleStageIndices[_carouselPage];
                LoadStage(stageIdx);
            };

            UpdatePooledCards();
            UpdateCarouselInfo();
        }

        private void NavigateToPage(int newPage, VisualElement content)
        {
            int delta = newPage - _carouselPage;
            if (delta == 0)
            {
                SnapToPoolCenter(content, true);
                return;
            }

            _isAnimating = true;
            int targetSlot = (PoolSize / 2) + delta;
            float targetX = -targetSlot * _cardWidth;
            content.style.transitionDuration = new List<TimeValue> { new TimeValue(250, TimeUnit.Millisecond) };
            content.style.translate = new Translate(targetX, 0);

            _carouselPage = newPage;

            content.schedule.Execute(() =>
            {
                content.style.transitionDuration = new List<TimeValue> { new TimeValue(0) };
                UpdatePooledCards();
                SnapToPoolCenter(content, false);
                UpdateCarouselInfo();
                _isAnimating = false;
            }).ExecuteLater(270);
        }

        private void UpdatePooledCards()
        {
            int center = PoolSize / 2;
            for (int p = 0; p < PoolSize; p++)
            {
                int pageOffset = p - center;
                int dataPage = _carouselPage + pageOffset;
                var card = _cardPool[p];

                if (dataPage < 0 || dataPage >= _totalVisible)
                {
                    card.style.visibility = Visibility.Hidden;
                    continue;
                }

                card.style.visibility = Visibility.Visible;
                int stageIdx = _visibleStageIndices[dataPage];
                var stage = _stages[stageIdx];
                bool isCleared = PlayerPrefs.GetInt($"StageCleared_{stage.stageNumber}", 0) == 1;
                bool isUnlocked = (stageIdx == 0) || PlayerPrefs.GetInt($"StageCleared_{_stages[stageIdx - 1].stageNumber}", 0) == 1;

                card.Q<Label>(className: "stage-card-label").text = $"Stage {stage.stageNumber}: {stage.stageName}";
                card.Q<Label>(className: "stage-card-status").text = isCleared ? "CLEARED" : isUnlocked ? "" : "LOCKED";

                card.EnableInClassList("stage-card-locked", !isUnlocked);
                card.EnableInClassList("stage-card-active", p == center);

                var preview = card.Q(className: "stage-card-preview");
                if (preview != null)
                {
                    var tex = GetOrCreatePreview(stageIdx, stage, isCleared);
                    if (tex != null)
                        preview.style.backgroundImage = new StyleBackground(tex);
                }
            }
        }

        private Texture2D GetOrCreatePreview(int stageIdx, StageData stage, bool isCleared)
        {
            int cacheKey = stageIdx * 2 + (isCleared ? 1 : 0);
            if (_previewCache.TryGetValue(cacheKey, out var cached))
                return cached;

            if (stage.goalLayout == null || stage.goalLayout.Count == 0)
            {
                _previewCache[cacheKey] = stage.pixelArtTexture;
                return stage.pixelArtTexture;
            }

            int rows = stage.mainRows;
            int cols = stage.mainCols;
            int cellPx = 24;
            int gemInset = 3;
            int texW = cols * cellPx;
            int texH = rows * cellPx;

            var goalMap = new GemColor[rows, cols];
            foreach (var def in stage.goalLayout)
                if (def.row >= 0 && def.row < rows && def.col >= 0 && def.col < cols)
                    goalMap[def.row, def.col] = def.color;

            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[texW * texH];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    GemColor gc = goalMap[r, c];
                    if (gc == GemColor.None) continue;

                    Color baseCol = GemColorPalette.GetColor(gc);
                    Color bgCol = GemColorPalette.GetDarkGoalColor(gc);
                    if (!isCleared)
                    {
                        baseCol = ToGray(baseCol);
                        bgCol = ToGray(bgCol);
                    }

                    int px = c * cellPx;
                    int py = (rows - 1 - r) * cellPx;

                    for (int y = 0; y < cellPx; y++)
                    {
                        for (int x = 0; x < cellPx; x++)
                        {
                            int idx = (py + y) * texW + (px + x);
                            bool isGem = x >= gemInset && x < cellPx - gemInset
                                      && y >= gemInset && y < cellPx - gemInset;
                            if (isGem)
                            {
                                Color col = baseCol;
                                int gx = x - gemInset;
                                int gy = y - gemInset;
                                int gemSize = cellPx - gemInset * 2;
                                if (gy < 2)
                                    col = Color.Lerp(baseCol, Color.white, 0.25f);
                                else if (gy >= gemSize - 2)
                                    col = Color.Lerp(baseCol, Color.black, 0.2f);
                                if (gx < 2)
                                    col = Color.Lerp(col, Color.white, 0.15f);
                                else if (gx >= gemSize - 2)
                                    col = Color.Lerp(col, Color.black, 0.1f);
                                pixels[idx] = col;
                            }
                            else
                            {
                                pixels[idx] = bgCol;
                            }
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _previewCache[cacheKey] = tex;
            return tex;
        }

        private static Color ToGray(Color c)
        {
            float g = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            g *= 0.55f;
            return new Color(g, g, g, c.a);
        }

        private void SnapToPoolCenter(VisualElement content, bool animate)
        {
            float targetX = -(PoolSize / 2) * _cardWidth;
            int ms = animate ? 250 : 0;
            content.style.transitionDuration = new List<TimeValue> { new TimeValue(ms, TimeUnit.Millisecond) };
            content.style.translate = new Translate(targetX, 0);
        }

        private float GetCurrentTranslateX(VisualElement el)
        {
            var t = el.resolvedStyle.translate;
            return t.x;
        }

        private void UpdateCarouselInfo()
        {
            int stageIdx = _visibleStageIndices[_carouselPage];
            var textLabel = _root.Q<Label>("active-stage-text");
            if (textLabel != null && stageIdx >= 0 && stageIdx < _stages.Count)
            {
                var stage = _stages[stageIdx];
                textLabel.text = $"Stage {stage.stageNumber}: {stage.stageName}";
            }

            var btnPrev = _root.Q<Button>("btn-prev");
            var btnNextNav = _root.Q<Button>("btn-next");
            if (btnPrev != null) btnPrev.style.display = _carouselPage > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (btnNextNav != null) btnNextNav.style.display = _carouselPage < _totalVisible - 1 ? DisplayStyle.Flex : DisplayStyle.None;

            bool isUnlocked = (stageIdx == 0) || PlayerPrefs.GetInt($"StageCleared_{_stages[stageIdx - 1].stageNumber}", 0) == 1;
            var btnStart = _root.Q<Button>("btn-start");
            if (btnStart != null) btnStart.style.display = isUnlocked ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private int FindNextStageIndex()
        {
            for (int i = 0; i < _stages.Count; i++)
            {
                if (PlayerPrefs.GetInt($"StageCleared_{_stages[i].stageNumber}", 0) == 0)
                    return i;
            }
            return _stages.Count - 1;
        }

        // --- Timer ---

        private void UpdateTimer(float remaining)
        {
            if (_currentScreen != Screen.GamePlay) return;

            float ratio = _gameManager.TimeRatio;

            var fill = _root.Q("timer-bar-fill");
            if (fill != null)
            {
                fill.style.width = Length.Percent(ratio * 100f);
                Color color;
                if (ratio > 0.5f)
                    color = new Color(0.2f, 0.85f, 0.3f);
                else if (ratio > 0.25f)
                    color = Color.Lerp(new Color(0.95f, 0.85f, 0.1f), new Color(0.2f, 0.85f, 0.3f), (ratio - 0.25f) / 0.25f);
                else
                    color = Color.Lerp(new Color(0.95f, 0.2f, 0.2f), new Color(0.95f, 0.85f, 0.1f), ratio / 0.25f);
                fill.style.backgroundColor = color;
            }

            var text = _root.Q<Label>("timer-text");
            if (text != null)
            {
                int min = Mathf.FloorToInt(remaining / 60f);
                int sec = Mathf.FloorToInt(remaining % 60f);
                text.text = $"{min:00}:{sec:00}";
            }
        }

        // --- Panels ---

        private void ShowClearedPanel()
        {
            if (_currentScreen != Screen.GamePlay) return;
            var panel = _root.Q("cleared-panel");
            if (panel != null) panel.style.display = DisplayStyle.Flex;
        }

        private void ShowFailedPanel()
        {
            if (_currentScreen != Screen.GamePlay) return;
            var panel = _root.Q("failed-panel");
            if (panel != null)
            {
                panel.style.display = DisplayStyle.Flex;
                var label = _root.Q<Button>("btn-addtime");
                if (label != null) label.text = $"+{_addTimeSeconds / 60f:0}:00 Watch Ad";
            }
        }

        private void HidePanel(string name)
        {
            var panel = _root.Q(name);
            if (panel != null) panel.style.display = DisplayStyle.None;
        }

        // --- Button handlers ---

        private void OnHintClicked()
        {
            if (_gridView != null && _gridManager != null)
                _gridView.ShowHint(_gridManager.SelectedGroup);
        }

        private void OnAddTimeClicked()
        {
            _gameManager?.AddTime(_addTimeSeconds);
            HidePanel("failed-panel");
        }

        private void OnReplayClicked()
        {
            _gameManager?.ResetStage();
            HidePanel("cleared-panel");
            HidePanel("failed-panel");
        }

        private void ShowUnlockDialog()
        {
            if (_gridManager != null && _gridManager.IsPaletteRow2Unlocked) return;

            _gameManager?.PauseGame();
            var panel = _root?.Q("unlock-dialog");
            if (panel != null) panel.style.display = DisplayStyle.Flex;
        }

        private void HideUnlockDialog()
        {
            HidePanel("unlock-dialog");
            _gameManager?.ResumeGame();
        }

        private void OnUnlockByItem()
        {
            // TODO: アイテム在庫チェック＆消費処理
            _gridManager?.UnlockPaletteRow2();
            HideUnlockDialog();
        }

        private void OnUnlockByAd()
        {
            if (AdManager.Instance != null)
            {
                AdManager.Instance.ShowInterstitialWithProbability(() =>
                {
                    _gridManager?.UnlockPaletteRow2();
                    HideUnlockDialog();
                });
            }
            else
            {
                _gridManager?.UnlockPaletteRow2();
                HideUnlockDialog();
            }
        }

        private void OnUnlockCancel()
        {
            HideUnlockDialog();
        }

        private void OnNextStageClicked()
        {
            HidePanel("cleared-panel");
            LoadNextStage();
        }

        private void PauseIfPlaying()
        {
            if (_gameManager != null && _gameManager.State == GameManager.GameState.Playing)
                _gameManager.PauseGame();
        }

        public void SetStageName(string name)
        {
            var label = _root?.Q<Label>("stage-name");
            if (label != null) label.text = name;
        }
    }
}
