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

                // クリア状況のチェック
                bool isCleared = PlayerPrefs.GetInt($"StageCleared_{stage.stageNumber}", 0) == 1;

                // ロック状況の判定：最初のステージは常にアンロック、それ以外は直前ステージがクリア済みならアンロック
                bool isUnlocked = (i == 0) || (PlayerPrefs.GetInt($"StageCleared_{_stages[i - 1].stageNumber}", 0) == 1);

                // テキスト（ステージ番号と名前）のセット
                var text = btnObj.transform.Find("Text")?.GetComponent<UnityEngine.UI.Text>();
                if (text != null)
                {
                    if (isUnlocked)
                    {
                        text.text = $"{stage.stageNumber} {stage.stageName}";
                    }
                    else
                    {
                        text.text = $"🔒 Stage {stage.stageNumber}";
                    }
                }

                // プレビュー画像のセット (PreviewImage)
                var previewImg = btnObj.transform.Find("PreviewImage")?.GetComponent<UnityEngine.UI.Image>();
                if (previewImg != null)
                {
                    previewImg.sprite = CreatePreviewSprite(stage, isCleared);
                    // ロックされている場合は半透明の暗い色にする
                    previewImg.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }

                var button = btnObj.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    button.interactable = isUnlocked;
                    button.onClick.AddListener(() => LoadStage(index));
                }
            }
        }

        // ゴール配置（goalLayout）からカラー or グレースケールのプレビュースプライトを生成する
        private Sprite CreatePreviewSprite(StageData stage, bool isCleared)
        {
            int rCount = stage.mainRows;
            int cCount = stage.mainCols;

            Texture2D tex = new Texture2D(cCount, rCount, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color[] pixels = new Color[rCount * cCount];
            // 透明で初期化
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            // goalLayout をピクセル座標に配置
            foreach (var cell in stage.goalLayout)
            {
                // Texture2Dのピクセルは下から上に向かってインデックスされるので、行インデックスを反転
                int y = rCount - 1 - cell.row;
                int x = cell.col;
                if (x >= 0 && x < cCount && y >= 0 && y < rCount)
                {
                    Color col = GemColorPalette.GetColor(cell.color);
                    if (!isCleared)
                    {
                        // グレースケール変換 (Luminance法)
                        float gray = 0.299f * col.r + 0.587f * col.g + 0.114f * col.b;
                        // 未クリアは少し暗い白黒シルエットにする
                        col = new Color(gray * 0.55f, gray * 0.55f, gray * 0.55f, 1f);
                    }
                    pixels[y * cCount + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, cCount, rCount), new Vector2(0.5f, 0.5f));
        }
    }
}
