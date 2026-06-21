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

            if (_uguiGamePlayPanel != null) _uguiGamePlayPanel.SetActive(true);

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

        // --- Carousel ---

        private int _visibleCount;
        private int _firstVisibleIndex;
        private float _cardWidth = 832f; // card(800) + margin(32)

        private void BuildCarousel()
        {
            int nextStageIndex = FindNextStageIndex();

            // 表示範囲: クリア済み5つ前 ～ 未クリア5つ先
            int startIdx = Mathf.Max(0, nextStageIndex - 5);
            int endIdx = Mathf.Min(_stages.Count - 1, nextStageIndex + 5);
            _firstVisibleIndex = startIdx;
            _visibleCount = endIdx - startIdx + 1;
            _carouselPage = nextStageIndex - startIdx;
            Debug.Log($"[Carousel] next={nextStageIndex} start={startIdx} end={endIdx} count={_visibleCount} page={_carouselPage}");

            var content = _root.Q("carousel-content");
            content.Clear();

            for (int i = startIdx; i <= endIdx; i++)
            {
                var stage = _stages[i];
                bool isCleared = PlayerPrefs.GetInt($"StageCleared_{stage.stageNumber}", 0) == 1;
                bool isUnlocked = (i == 0) || PlayerPrefs.GetInt($"StageCleared_{_stages[i - 1].stageNumber}", 0) == 1;

                var card = new VisualElement();
                card.AddToClassList("stage-card");
                if (!isUnlocked) card.AddToClassList("stage-card-locked");

                var label = new Label($"Stage {stage.stageNumber}");
                label.AddToClassList("stage-card-label");
                card.Add(label);

                var statusLabel = new Label(isCleared ? "CLEARED" : isUnlocked ? "" : "LOCKED");
                statusLabel.AddToClassList("stage-card-status");
                card.Add(statusLabel);

                content.Add(card);
            }

            // viewportサイズ確定後にスナップ
            var vp = _root.Q("carousel-viewport");
            vp.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                float vpW = evt.newRect.width;
                float pad = Mathf.Max(0, (vpW - 800f) / 2f);
                content.style.paddingLeft = pad;
                content.style.paddingRight = pad;
                SnapToPage(content, false);
            });

            var btnPrev = _root.Q<Button>("btn-prev");
            var btnNext = _root.Q<Button>("btn-next");
            var btnStart = _root.Q<Button>("btn-start");

            // スワイプ
            var viewport = _root.Q("carousel-viewport");
            float dragStartTranslateX = 0f;
            float swipeStartX = 0f;
            float lastPointerX = 0f;
            float velocity = 0f;
            bool isDragging = false;
            long lastMoveTime = 0;

            viewport.RegisterCallback<PointerDownEvent>(evt =>
            {
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
                int nearestPage = Mathf.RoundToInt(-currentX / _cardWidth);
                if (Mathf.Abs(velocity) > 400f)
                    nearestPage = velocity < 0 ? _carouselPage + 1 : _carouselPage - 1;
                _carouselPage = Mathf.Clamp(nearestPage, 0, _visibleCount - 1);
                SnapToPage(content, true);
                UpdateCarouselInfo();
            });

            btnPrev.clicked += () => { if (_carouselPage > 0) { _carouselPage--; SnapToPage(content, true); UpdateCarouselInfo(); } };
            btnNext.clicked += () => { if (_carouselPage < _visibleCount - 1) { _carouselPage++; SnapToPage(content, true); UpdateCarouselInfo(); } };
            btnStart.clicked += () => LoadStage(_firstVisibleIndex + _carouselPage);

            UpdateCarouselInfo();
        }

        private void SnapToPage(VisualElement content, bool animate)
        {
            float targetX = -_carouselPage * _cardWidth;
            int ms = animate ? 300 : 0;
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
            int stageIdx = _firstVisibleIndex + _carouselPage;
            var textLabel = _root.Q<Label>("active-stage-text");
            if (textLabel != null && stageIdx >= 0 && stageIdx < _stages.Count)
            {
                var stage = _stages[stageIdx];
                textLabel.text = $"Stage {stage.stageNumber}: {stage.stageName}";
            }

            var content = _root.Q("carousel-content");
            for (int i = 0; i < content.childCount; i++)
            {
                if (i == _carouselPage) content[i].AddToClassList("stage-card-active");
                else content[i].RemoveFromClassList("stage-card-active");
            }

            var btnPrev = _root.Q<Button>("btn-prev");
            var btnNext = _root.Q<Button>("btn-next");
            if (btnPrev != null) btnPrev.style.display = _carouselPage > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (btnNext != null) btnNext.style.display = _carouselPage < _visibleCount - 1 ? DisplayStyle.Flex : DisplayStyle.None;

            bool isUnlocked = (stageIdx == 0) || PlayerPrefs.GetInt($"StageCleared_{_stages[stageIdx - 1].stageNumber}", 0) == 1;
            var btnStart = _root.Q<Button>("btn-start");
            if (btnStart != null) btnStart.style.display = isUnlocked ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private int FindNextStageIndex()
        {
            int next = 0;
            for (int i = 0; i < _stages.Count; i++)
            {
                if (PlayerPrefs.GetInt($"StageCleared_{_stages[i].stageNumber}", 0) == 0)
                    return i;
                next = i;
            }
            return next;
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
