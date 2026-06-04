// GameBootstrap.cs — シーン起動時にステージを初期化するエントリーポイント
using UnityEngine;
using System.Collections.Generic;
using SortGems.Core;
using SortGems.UI;

namespace SortGems.UI
{
    /// <summary>
    /// GameScene の起動順制御。
    /// Title → StageSelect → GamePlay の画面遷移とステージ管理を行う。
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        [Header("Screens")]
        [SerializeField] private GameObject _titlePanel;
        [SerializeField] private GameObject _stageSelectPanel;
        [SerializeField] private GameObject _gamePlayPanel;

        [Header("References")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private GridView _gridView;
        [SerializeField] private GameUI _gameUI;

        [Header("Stages")]
        [SerializeField] private List<StageData> _stages = new();
        [SerializeField] private Transform _stageButtonContent;
        [SerializeField] private GameObject _stageButtonPrefab;

        private int _currentStageIndex = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ShowTitle();
            BuildStageSelectUI();
        }

        public void ShowTitle()
        {
            if (_titlePanel != null) _titlePanel.SetActive(true);
            if (_stageSelectPanel != null) _stageSelectPanel.SetActive(false);
            if (_gamePlayPanel != null) _gamePlayPanel.SetActive(false);

            if (_gameManager != null && _gameManager.State == GameManager.GameState.Playing)
            {
                _gameManager.PauseGame();
            }
        }

        public void ShowStageSelect()
        {
            if (_titlePanel != null) _titlePanel.SetActive(false);
            if (_stageSelectPanel != null) _stageSelectPanel.SetActive(true);
            if (_gamePlayPanel != null) _gamePlayPanel.SetActive(false);

            if (_gameManager != null && _gameManager.State == GameManager.GameState.Playing)
            {
                _gameManager.PauseGame();
            }
        }

        public void ShowGamePlay()
        {
            if (_titlePanel != null) _titlePanel.SetActive(false);
            if (_stageSelectPanel != null) _stageSelectPanel.SetActive(false);
            if (_gamePlayPanel != null) _gamePlayPanel.SetActive(true);
        }

        public void LoadStage(int index)
        {
            if (index < 0 || index >= _stages.Count) return;
            _currentStageIndex = index;
            var stage = _stages[index];

            ShowGamePlay();

            if (_gameManager != null)
            {
                _gameManager.StartStage(stage);
            }

            if (_gridView != null)
            {
                _gridView.BuildGrid(stage);
            }

            if (_gameUI != null)
            {
                _gameUI.SetStageName(stage.stageName);
            }
        }

        public void LoadNextStage()
        {
            int nextIndex = _currentStageIndex + 1;
            if (nextIndex < _stages.Count)
            {
                LoadStage(nextIndex);
            }
            else
            {
                // 全ステージクリア時はステージ選択に戻る
                ShowStageSelect();
            }
        }

        private void BuildStageSelectUI()
        {
            if (_stageButtonContent == null || _stageButtonPrefab == null) return;

            // 既存のボタンをクリア（テンプレート以外）
            foreach (Transform child in _stageButtonContent)
            {
                if (child.gameObject != _stageButtonPrefab)
                {
                    Destroy(child.gameObject);
                }
            }

            _stageButtonPrefab.SetActive(false); // テンプレート自体は非表示

            for (int i = 0; i < _stages.Count; i++)
            {
                int index = i;
                var stage = _stages[i];
                var btnObj = Instantiate(_stageButtonPrefab, _stageButtonContent);
                btnObj.name = $"Stage_{stage.stageNumber}";
                btnObj.SetActive(true);

                var text = btnObj.GetComponentInChildren<UnityEngine.UI.Text>();
                if (text != null)
                {
                    text.text = $"{stage.stageNumber}\n{stage.stageName}";
                }

                var button = btnObj.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => LoadStage(index));
                }
            }
        }
    }
}
