// GameUI.cs — ゲームプレイ画面のUI制御（TextMeshPro・LeanTween不要版）
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SortGems.Core;

namespace SortGems.UI
{
    public class GameUI : MonoBehaviour
    {
        [Header("Timer")]
        [SerializeField] private Slider _timerSlider;
        [SerializeField] private Text _timerText;       // UnityEngine.UI.Text（TMP不要）
        [SerializeField] private Image _timerFill;

        [Header("Stage Info")]
        [SerializeField] private Text _stageNameText;

        [Header("Action Buttons")]
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _hintButton;
        [SerializeField] private Button _resetButton;

        [Header("Cleared Panel")]
        [SerializeField] private GameObject _clearedPanel;
        [SerializeField] private Button _nextStageButton;
        [SerializeField] private Button _replayButton_Cleared;
        [SerializeField] private Button _clearedBackButton;

        [Header("Failed Panel")]
        [SerializeField] private GameObject _failedPanel;
        [SerializeField] private Button _addTimeButton;
        [SerializeField] private Button _replayButton_Failed;
        [SerializeField] private Button _failedBackButton;
        [SerializeField] private Text _addTimeLabel;

        [Header("Screen Control Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _stageSelectBackButton;
        [SerializeField] private Button _gamePlayBackButton;

        [Header("Settings")]
        [SerializeField] private float _addTimeSeconds = 60f;

        private static readonly Color _colorGreen  = new Color(0.2f, 0.85f, 0.3f);
        private static readonly Color _colorYellow = new Color(0.95f, 0.85f, 0.1f);
        private static readonly Color _colorRed    = new Color(0.95f, 0.2f, 0.2f);

        [Header("References")]
        [SerializeField] private GridView _gridView;
        [SerializeField] private GridManager _gridManager;

        private GameManager _gameManager;

        private void Start()
        {
            _gameManager = GameManager.Instance;

            _undoButton.onClick.AddListener(_gameManager.Undo);
            _resetButton.onClick.AddListener(() =>
            {
                _gameManager.ResetStage();
                _failedPanel.SetActive(false);
            });
            _hintButton.onClick.AddListener(OnHintClicked);
            _addTimeButton.onClick.AddListener(OnAddTimeClicked);
            _replayButton_Failed.onClick.AddListener(OnReplayClicked);
            _nextStageButton.onClick.AddListener(OnNextStageClicked);
            _replayButton_Cleared.onClick.AddListener(OnReplayClicked);

            if (_clearedBackButton != null)
            {
                _clearedBackButton.onClick.AddListener(() =>
                {
                    _clearedPanel.SetActive(false);
                    if (GameBootstrap.Instance != null) GameBootstrap.Instance.ShowStageSelect();
                });
            }
            if (_failedBackButton != null)
            {
                _failedBackButton.onClick.AddListener(() =>
                {
                    _failedPanel.SetActive(false);
                    if (GameBootstrap.Instance != null) GameBootstrap.Instance.ShowStageSelect();
                });
            }

            _gameManager.OnGameCleared.AddListener(ShowClearedPanel);
            _gameManager.OnGameFailed.AddListener(ShowFailedPanel);
            _gameManager.OnTimerUpdated.AddListener(UpdateTimer);

            _clearedPanel.SetActive(false);
            _failedPanel.SetActive(false);

            if (_playButton != null) _playButton.onClick.AddListener(() => GameBootstrap.Instance.ShowStageSelect());
            if (_stageSelectBackButton != null) _stageSelectBackButton.onClick.AddListener(() => GameBootstrap.Instance.ShowTitle());
            if (_gamePlayBackButton != null) _gamePlayBackButton.onClick.AddListener(() => GameBootstrap.Instance.ShowStageSelect());
        }

        // ---- タイマー表示 ----

        private void UpdateTimer(float remaining)
        {
            float ratio = _gameManager.TimeRatio;

            if (_timerSlider != null)
                _timerSlider.value = ratio;

            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            if (_timerText != null)
                _timerText.text = $"{minutes:00}:{seconds:00}";

            if (_timerFill != null)
            {
                if (ratio > 0.5f)
                    _timerFill.color = _colorGreen;
                else if (ratio > 0.25f)
                    _timerFill.color = Color.Lerp(_colorYellow, _colorGreen, (ratio - 0.25f) / 0.25f);
                else
                    _timerFill.color = Color.Lerp(_colorRed, _colorYellow, ratio / 0.25f);
            }
        }

        // ---- パネル表示 ----

        private void ShowClearedPanel()
        {
            _clearedPanel.SetActive(true);
            StartCoroutine(ScalePanelIn(_clearedPanel));
        }

        private void ShowFailedPanel()
        {
            _failedPanel.SetActive(true);
            StartCoroutine(ScalePanelIn(_failedPanel));
            if (_addTimeLabel != null)
                _addTimeLabel.text = $"+{_addTimeSeconds / 60f:0}:00 Watch Ad";
        }

        private IEnumerator ScalePanelIn(GameObject panel)
        {
            panel.transform.localScale = Vector3.zero;
            float duration = 0.25f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float s = 1.70158f;
                t -= 1f;
                float val = t * t * ((s + 1f) * t + s) + 1f;
                panel.transform.localScale = Vector3.one * val;
                yield return null;
            }
            panel.transform.localScale = Vector3.one;
        }

        // ---- ボタンハンドラ ----

        private void OnHintClicked()
        {
            if (_gridView != null && _gridManager != null)
            {
                _gridView.ShowHint(_gridManager.SelectedGroup);
            }
            Debug.Log("[GameUI] Hint requested and displayed.");
        }

        private void OnAddTimeClicked()
        {
            // TODO: AdManager.Instance.ShowRewardedAd(onComplete: () => _gameManager.AddTime(_addTimeSeconds));
            _gameManager.AddTime(_addTimeSeconds);
            _failedPanel.SetActive(false);
            Debug.Log($"[GameUI] AddTime {_addTimeSeconds}s");
        }

        private void OnReplayClicked()
        {
            _gameManager.ResetStage();
            _clearedPanel.SetActive(false);
            _failedPanel.SetActive(false);
        }

        private void OnNextStageClicked()
        {
            _clearedPanel.SetActive(false);
            if (GameBootstrap.Instance != null)
            {
                GameBootstrap.Instance.LoadNextStage();
            }
        }

        public void SetStageName(string name)
        {
            if (_stageNameText != null) _stageNameText.text = name;
        }
    }
}
