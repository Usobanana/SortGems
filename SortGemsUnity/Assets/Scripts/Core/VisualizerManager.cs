using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SortGems.Core
{
    /// <summary>
    /// 全画面で常駐（DontDestroyOnLoad）し、複数のビジュアライザーバーを横一列に自動配置して管理するシングルトンマネージャー。
    /// </summary>
    public class VisualizerManager : MonoBehaviour
    {
        public static VisualizerManager Instance { get; private set; }

        [Header("Prefab and Parent")]
        [Tooltip("各バーのベースとなるプレハブ（空の場合は実行時にUI Imageを自動生成します）")]
        [SerializeField] private GameObject _barPrefab;

        [Tooltip("バーを配置する親コンテナ（RectTransform）")]
        [SerializeField] private RectTransform _container;

        [Header("Settings")]
        [Tooltip("バーの数")]
        [SerializeField] private int _barCount = 32;

        [Tooltip("バー同士の間隔（ピクセル）")]
        [SerializeField] private float _spacing = 3f;

        [Tooltip("周波数に対する伸縮の感度")]
        [SerializeField] private float _sensitivity = 2000f;

        [Tooltip("元のサイズに戻るスムーズさ（Lerpスピード）")]
        [SerializeField] private float _lerpSpeed = 10f;

        [Header("Visual Settings")]
        [Tooltip("バーの色と透明度（うっすら背景に馴染むようにアルファを低めに設定）")]
        [SerializeField] private Color _barColor = new Color(1f, 1f, 1f, 0.4f);

        [Tooltip("バーの最小の高さ（音がない時の高さ）")]
        [SerializeField] private float _minHeight = 10f;

        [Tooltip("バーの最大の高さ")]
        [SerializeField] private float _maxHeight = 180f;

        private List<GameObject> _bars = new();
        private List<VisualizerBar> _barScripts = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // シーン切り替え時もこのオブジェクト（ルートオブジェクト）を保持する
            DontDestroyOnLoad(gameObject);
            if (transform.parent != null)
            {
                DontDestroyOnLoad(transform.root.gameObject);
            }
        }

        private void Start()
        {
            GenerateBars();
        }

        /// <summary>
        /// 画面サイズや設定値に基づいて、バーを横一列に等間隔で自動生成して配置する。
        /// </summary>
        public void GenerateBars()
        {
            // 既存のバーを破棄
            foreach (var bar in _bars)
            {
                if (bar != null) Destroy(bar);
            }
            _bars.Clear();
            _barScripts.Clear();

            if (_container == null)
            {
                Debug.LogError("[VisualizerManager] _container (RectTransform) が設定されていません。");
                return;
            }

            // HorizontalLayoutGroupを追加または取得して設定
            var layout = _container.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = _container.gameObject.AddComponent<HorizontalLayoutGroup>();

            layout.spacing = _spacing;
            layout.childAlignment = TextAnchor.LowerLeft; // 下端揃えにして上に向かって伸びるようにする
            layout.childControlWidth = true;
            layout.childControlHeight = false; // 高さはスクリプト（VisualizerBar）で制御するため
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 0);

            for (int i = 0; i < _barCount; i++)
            {
                GameObject barObj;
                if (_barPrefab != null)
                {
                    barObj = Instantiate(_barPrefab, _container);
                }
                else
                {
                    // プレハブが未指定の場合は、自動で Image をアタッチしたUIオブジェクトを生成
                    barObj = new GameObject($"Bar_{i}");
                    barObj.transform.SetParent(_container, false);
                    
                    var img = barObj.AddComponent<Image>();
                    img.color = _barColor;
                    
                    // UIイベントを遮らないようにレイキャストターゲットはOFFにする
                    img.raycastTarget = false; 
                }

                barObj.name = $"Bar_{i}";
                _bars.Add(barObj);

                var rect = barObj.GetComponent<RectTransform>();
                if (rect == null) rect = barObj.AddComponent<RectTransform>();

                // HorizontalLayoutGroupが幅と配置を自動制御します。
                // ピボットを (0.5f, 0f) にすることで、高さが変更されたときに上方向に伸びるようになります。
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, _minHeight);

                // 個々のバーを制御するスクリプトをアタッチ
                var barScript = barObj.GetComponent<VisualizerBar>();
                if (barScript == null) barScript = barObj.AddComponent<VisualizerBar>();

                // 周波数帯（インデックス）の割り当て:
                // 左側（低インデックス）が低音、右側（高インデックス）が高音
                int frequencyBin = Mathf.RoundToInt(Mathf.Lerp(1, 100, (float)i / _barCount));

                barScript.Setup(frequencyBin, _sensitivity, _lerpSpeed, _minHeight, _maxHeight);
                _barScripts.Add(barScript);
            }
        }
    }
}
