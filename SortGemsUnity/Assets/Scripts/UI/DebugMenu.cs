using UnityEngine;
using UnityEngine.UI;
using SortGems.Core;

namespace SortGems.UI
{
    /// <summary>
    /// デバッグ用のオーバーレイUI。
    /// 画面右上に「Debug」ボタンを配置し、クリックすると「Clear Stage」「Unlock All Stages」「Reset Progress」のメニューを開きます。
    /// </summary>
    public class DebugMenu : MonoBehaviour
    {
        private GameObject _debugButtonObj;
        private GameObject _debugPanelObj;

        private void Start()
        {
            CreateDebugUI();
        }

        private void CreateDebugUI()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[DebugMenu] Canvas not found in scene.");
                return;
            }

            // ---- 1. Debug ボタンの作成 ----
            _debugButtonObj = new GameObject("DebugButton", typeof(RectTransform), typeof(Image), typeof(Button));
            _debugButtonObj.transform.SetParent(canvas.transform, false);
            _debugButtonObj.transform.SetAsLastSibling();

            var btnRect = _debugButtonObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(1, 1);
            btnRect.anchoredPosition = new Vector2(-20, -20); // 右上から少し内側
            btnRect.sizeDelta = new Vector2(120, 60);

            var btnImg = _debugButtonObj.GetComponent<Image>();
            btnImg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            var button = _debugButtonObj.GetComponent<Button>();
            button.onClick.AddListener(ToggleDebugPanel);

            // テキストの追加
            GameObject btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnTextObj.transform.SetParent(_debugButtonObj.transform, false);
            var btnTextRect = btnTextObj.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;

            var btnTxt = btnTextObj.GetComponent<Text>();
            btnTxt.text = "Debug";
            btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.fontSize = 20;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = Color.white;

            // ---- 2. Debug パネルの作成 ----
            _debugPanelObj = new GameObject("DebugPanel", typeof(RectTransform), typeof(Image));
            _debugPanelObj.transform.SetParent(canvas.transform, false);
            _debugPanelObj.transform.SetAsLastSibling();

            var panelRect = _debugPanelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400, 300);

            var panelImg = _debugPanelObj.GetComponent<Image>();
            panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            _debugPanelObj.SetActive(false);

            // タイトル
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(_debugPanelObj.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(300, 40);

            var titleTxt = titleObj.GetComponent<Text>();
            titleTxt.text = "DEBUG MENU";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 24;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = Color.yellow;

            // ボタン配置用のコンテナ (VerticalLayoutGroup)
            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentObj.transform.SetParent(_debugPanelObj.transform, false);
            var contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.1f, 0.1f);
            contentRect.anchorMax = new Vector2(0.9f, 0.8f);
            contentRect.sizeDelta = Vector2.zero;

            var layout = contentObj.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            // 各種デバッグボタンの作成
            CreatePanelButton(contentObj.transform, "Clear Stage", OnClearStageClicked);
            CreatePanelButton(contentObj.transform, "Unlock All Stages", OnUnlockAllClicked);
            CreatePanelButton(contentObj.transform, "Reset Progress", OnResetClicked);
        }

        private void CreatePanelButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 50);

            var img = btnObj.GetComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var button = btnObj.GetComponent<Button>();
            button.onClick.AddListener(action);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            var txt = txtObj.GetComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 18;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }

        private void ToggleDebugPanel()
        {
            if (_debugPanelObj != null)
            {
                bool active = !_debugPanelObj.activeSelf;
                _debugPanelObj.SetActive(active);
                if (active)
                {
                    _debugPanelObj.transform.SetAsLastSibling();
                }
            }
        }

        // ---- デバッグアクション ----

        private void OnClearStageClicked()
        {
            var gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager != null)
            {
                gridManager.ForceClearStage();
                ToggleDebugPanel();
                Debug.Log("[DebugMenu] ForceClearStage executed.");
            }
            else
            {
                Debug.LogWarning("[DebugMenu] GridManager not found in scene.");
            }
        }

        private void OnUnlockAllClicked()
        {
            // 全100ステージ（1〜100）をクリア済みに設定
            for (int i = 1; i <= 100; i++)
            {
                PlayerPrefs.SetInt($"StageCleared_{i}", 1);
            }
            PlayerPrefs.Save();

            if (GameBootstrap.Instance != null)
            {
                GameBootstrap.Instance.ShowStageSelect();
            }
            
            ToggleDebugPanel();
            Debug.Log("[DebugMenu] All stages unlocked.");
        }

        private void OnResetClicked()
        {
            // 1〜100ステージのクリアデータを削除
            for (int i = 1; i <= 100; i++)
            {
                PlayerPrefs.DeleteKey($"StageCleared_{i}");
            }
            PlayerPrefs.Save();

            if (GameBootstrap.Instance != null)
            {
                GameBootstrap.Instance.ShowStageSelect();
            }

            ToggleDebugPanel();
            Debug.Log("[DebugMenu] Progress reset. Stage 1 is unlocked.");
        }
    }
}
