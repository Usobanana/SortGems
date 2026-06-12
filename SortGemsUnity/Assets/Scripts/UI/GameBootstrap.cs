// GameBootstrap.cs — シーン起動時にステージを初期化するエントリーポイント
using UnityEngine;
using System.Collections.Generic;
using SortGems.Core;
using SortGems.UI;
using SortGems.Ads;

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
        [SerializeField] private GemCellView _cellPrefab;

        [Header("Carousel Selection")]
        [SerializeField] private UnityEngine.UI.Button _leftButton;
        [SerializeField] private UnityEngine.UI.Button _rightButton;
        [SerializeField] private UnityEngine.UI.ScrollRect _scrollRect;
        [SerializeField] private UnityEngine.UI.Text _activeStageText;

        private int _currentStageIndex = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ShowTitle();
            gameObject.AddComponent<DebugMenu>();
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

            if (AdManager.Instance != null)
            {
                AdManager.Instance.HideBanner();
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

            BuildStageSelectUI();

            if (AdManager.Instance != null)
            {
                AdManager.Instance.ShowBanner();
            }
        }

        public void ShowGamePlay()
        {
            if (_titlePanel != null) _titlePanel.SetActive(false);
            if (_stageSelectPanel != null) _stageSelectPanel.SetActive(false);
            if (_gamePlayPanel != null) _gamePlayPanel.SetActive(true);

            if (AdManager.Instance != null)
            {
                AdManager.Instance.ShowBanner();
            }
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
                if (AdManager.Instance != null)
                {
                    AdManager.Instance.ShowInterstitialWithProbability(() =>
                    {
                        LoadStage(nextIndex);
                    });
                }
                else
                {
                    LoadStage(nextIndex);
                }
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

            // 既存のオブジェクトをクリア（テンプレート以外）
            foreach (Transform child in _stageButtonContent)
            {
                if (child.gameObject != _stageButtonPrefab)
                {
                    Destroy(child.gameObject);
                }
            }

            _stageButtonPrefab.SetActive(false); // テンプレート自体は非表示

            // 次に挑戦すべきステージ（未クリアの最初のステージ）を特定
            int nextStageIndex = 0;
            for (int i = 0; i < _stages.Count; i++)
            {
                bool isCleared = PlayerPrefs.GetInt($"StageCleared_{_stages[i].stageNumber}", 0) == 1;
                if (!isCleared)
                {
                    nextStageIndex = i;
                    break;
                }
                nextStageIndex = i; // すべてクリア済みの場合は最後のステージ
            }

            // 5つ先までスライド可能にする
            int maxVisibleIndex = Mathf.Min(nextStageIndex + 5, _stages.Count - 1);
            int visibleCount = _stages.Count > 0 ? maxVisibleIndex + 1 : 0;

            Debug.Log($"[SortGems] BuildStageSelectUI: TotalStages={_stages.Count}, NextStageIndex={nextStageIndex}, VisibleCount={visibleCount}");

            for (int i = 0; i < visibleCount; i++)
            {
                int index = i;
                var stage = _stages[i];
                var cardObj = Instantiate(_stageButtonPrefab, _stageButtonContent);
                var cardRt = cardObj.GetComponent<RectTransform>();
                if (cardRt != null)
                {
                    cardRt.anchoredPosition3D = Vector3.zero;
                    cardRt.localScale = Vector3.one;
                }
                else
                {
                    cardObj.transform.localPosition = Vector3.zero;
                    cardObj.transform.localScale = Vector3.one;
                }
                cardObj.name = $"Stage_{stage.stageNumber}";
                cardObj.SetActive(true);

                // クリア状況のチェック
                bool isCleared = PlayerPrefs.GetInt($"StageCleared_{stage.stageNumber}", 0) == 1;

                // ロック状況の判定：最初のステージは常にアンロック、それ以外は直前ステージがクリア済みならアンロック
                bool isUnlocked = (i == 0) || (PlayerPrefs.GetInt($"StageCleared_{_stages[i - 1].stageNumber}", 0) == 1);

                // プレビューグリッドのセット (PreviewGrid)
                var previewGridTrans = cardObj.transform.Find("PreviewGrid");
                if (previewGridTrans != null)
                {
                    var gridLayout = previewGridTrans.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                    var gridRt = previewGridTrans.GetComponent<RectTransform>();
                    
                    if (gridLayout != null && gridRt != null && _cellPrefab != null)
                    {
                        // 既存のセルをクリア
                        foreach (Transform child in previewGridTrans)
                        {
                            Destroy(child.gameObject);
                        }
                        
                        int rCount = stage.mainRows;
                        int cCount = stage.mainCols;
                        
                        // パズル画面と同じセルサイズと間隔
                        float cellSize = 41.5f; 
                        float cellSpacing = 0f;
                        
                        gridLayout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
                        gridLayout.constraintCount = cCount;
                        gridLayout.cellSize = new Vector2(cellSize, cellSize);
                        gridLayout.spacing = new Vector2(cellSpacing, cellSpacing);
                        
                        // グリッドサイズを動的に設定
                        gridRt.sizeDelta = new Vector2(cCount * cellSize, rCount * cellSize);
                        
                        // ロックされている場合は半透明にする。また、スワイプ操作をカード背後に通すため blocksRaycasts を false にする
                        var cg = previewGridTrans.GetComponent<CanvasGroup>();
                        if (cg == null) cg = previewGridTrans.gameObject.AddComponent<CanvasGroup>();
                        cg.alpha = isUnlocked ? 1f : 0.4f;
                        cg.blocksRaycasts = false;
                        
                        // 二次元配列を作成して goalLayout を展開
                        GemColor[,] gridColors = new GemColor[rCount, cCount];
                        for (int r = 0; r < rCount; r++)
                        {
                            for (int c = 0; c < cCount; c++)
                            {
                                gridColors[r, c] = GemColor.None;
                            }
                        }
                        
                        foreach (var cell in stage.goalLayout)
                        {
                            if (cell.row >= 0 && cell.row < rCount && cell.col >= 0 && cell.col < cCount)
                            {
                                gridColors[cell.row, cell.col] = cell.color;
                            }
                        }
                        
                        // セルを生成して配置
                        for (int r = 0; r < rCount; r++)
                        {
                            for (int c = 0; c < cCount; c++)
                            {
                                var cellView = Instantiate(_cellPrefab, previewGridTrans);
                                cellView.Setup(r, c, isPalette: false);
                                
                                GemColor color = gridColors[r, c];
                                cellView.SetGem(color, color, isVoid: (color == GemColor.None), grayscale: !isCleared);
                            }
                        }
                    }
                }

                // STARTボタンの制御 (次に挑戦すべきステージのみ表示)
                var startBtnObj = cardObj.transform.Find("StartButton")?.gameObject;
                var startButton = startBtnObj?.GetComponent<UnityEngine.UI.Button>();
                if (startButton != null)
                {
                    bool isNextStage = (i == nextStageIndex);
                    startBtnObj.SetActive(isNextStage);
                    if (isNextStage)
                    {
                        startButton.onClick.RemoveAllListeners();
                        startButton.onClick.AddListener(() => LoadStage(index));
                    }
                }
            }

            // カルーセルの初期化
            if (_scrollRect != null)
            {
                var carousel = _scrollRect.GetComponent<StageCarousel>();
                if (carousel == null)
                {
                    carousel = _scrollRect.gameObject.AddComponent<StageCarousel>();
                }
                carousel.Initialize(visibleCount, nextStageIndex, _leftButton, _rightButton);

                if (_activeStageText != null)
                {
                    // 初期選択状態のテキスト設定
                    if (nextStageIndex >= 0 && nextStageIndex < _stages.Count)
                    {
                        var initStage = _stages[nextStageIndex];
                        _activeStageText.text = $"ステージ {initStage.stageNumber}：{initStage.stageName}";
                    }

                    // ページ切り替えイベントで更新
                    carousel.OnPageChanged = (pageIndex) =>
                    {
                        if (pageIndex >= 0 && pageIndex < _stages.Count)
                        {
                            var currentStage = _stages[pageIndex];
                            _activeStageText.text = $"ステージ {currentStage.stageNumber}：{currentStage.stageName}";
                        }
                    };
                }
            }
        }
    }
}
