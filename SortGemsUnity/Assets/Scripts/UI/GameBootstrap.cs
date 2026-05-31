// GameBootstrap.cs — シーン起動時にステージを初期化するエントリーポイント
using UnityEngine;
using SortGems.Core;
using SortGems.UI;

namespace SortGems.UI
{
    /// <summary>
    /// GameScene の起動順制御。
    /// GameManager → GridManager → GridView → GameUI の順に初期化する。
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private GridView _gridView;
        [SerializeField] private GameUI _gameUI;

        [Header("Test Stage（開発中のみ）")]
        [SerializeField] private StageData _testStage;

        private void Start()
        {
            if (_testStage == null)
            {
                Debug.LogError("[GameBootstrap] testStage が設定されていません。InspectorでStageDataを指定してください。");
                return;
            }

            LoadStage(_testStage);
        }

        public void LoadStage(StageData stage)
        {
            // 1. ゲームマネージャ起動（ロジック初期化 + タイマー開始）
            _gameManager.StartStage(stage);

            // 2. グリッドビュー構築（視覚的なグリッドを生成）
            _gridView.BuildGrid(stage);

            // 3. UI にステージ名を渡す
            _gameUI.SetStageName(stage.stageName);
        }
    }
}
