using UnityEngine;
using UnityEditor;
using SortGems.Core;
using SortGems.UI;

public class DisableOldUI
{
    [MenuItem("Tools/SortGems/Disable Old uGUI (GameBootstrap + Canvas)")]
    public static void DisableOldUIObjects()
    {
        var bootstrap = Object.FindFirstObjectByType<GameBootstrap>(FindObjectsInactive.Include);
        if (bootstrap != null)
        {
            bootstrap.gameObject.SetActive(false);
            EditorUtility.SetDirty(bootstrap.gameObject);
            Debug.Log("[DisableOldUI] GameBootstrap disabled.");
        }

        // Canvas自体は有効のまま、旧UIパネル（Title/StageSelect）のみ無効化
        // GamePlayPanel内のグリッドはuGUIで描画するため残す
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name == "[Canvas]")
            {
                go.SetActive(true);
                EditorUtility.SetDirty(go);
                Debug.Log("[DisableOldUI] [Canvas] re-enabled (grid needs uGUI).");

                // 旧UIパネルを無効化
                var titlePanel = go.transform.Find("TitlePanel");
                if (titlePanel != null) { titlePanel.gameObject.SetActive(false); Debug.Log("[DisableOldUI] TitlePanel disabled."); }

                var stageSelectPanel = go.transform.Find("StageSelectPanel");
                if (stageSelectPanel != null) { stageSelectPanel.gameObject.SetActive(false); Debug.Log("[DisableOldUI] StageSelectPanel disabled."); }

                // ClearedPanel, FailedPanel も無効化（UI Toolkit側で表示する）
                var gamePlayPanel = go.transform.Find("GamePlayPanel");
                if (gamePlayPanel != null)
                {
                    gamePlayPanel.gameObject.SetActive(false);
                    Debug.Log("[DisableOldUI] GamePlayPanel disabled (ScreenManager will enable grid parts).");

                    // Header, ButtonBar, ClearedPanel, FailedPanel を無効化（UI Toolkit HUDが担当）
                    var header = gamePlayPanel.Find("Header");
                    if (header != null) header.gameObject.SetActive(false);
                    var buttonBar = gamePlayPanel.Find("ButtonBar");
                    if (buttonBar != null) buttonBar.gameObject.SetActive(false);
                    var cleared = gamePlayPanel.Find("ClearedPanel");
                    if (cleared != null) cleared.gameObject.SetActive(false);
                    var failed = gamePlayPanel.Find("FailedPanel");
                    if (failed != null) failed.gameObject.SetActive(false);
                }

                break;
            }
        }

        // GameUI コンポーネントも無効化（旧UIロジックと競合しないように）
        var gameUI = Object.FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
        if (gameUI != null)
        {
            gameUI.enabled = false;
            EditorUtility.SetDirty(gameUI);
            Debug.Log("[DisableOldUI] GameUI component disabled.");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[DisableOldUI] Done. Save the scene to persist changes.");
    }

    [MenuItem("Tools/SortGems/Fix ScreenManager References")]
    public static void FixScreenManagerReferences()
    {
        var screenMgr = Object.FindFirstObjectByType<ScreenManager>(FindObjectsInactive.Include);
        if (screenMgr == null)
        {
            Debug.LogError("[FixRefs] ScreenManager not found.");
            return;
        }

        var so = new SerializedObject(screenMgr);

        var gridView = Object.FindFirstObjectByType<GridView>(FindObjectsInactive.Include);
        if (gridView != null)
        {
            so.FindProperty("_gridView").objectReferenceValue = gridView;
            Debug.Log($"[FixRefs] GridView set: {gridView.name}");
        }

        var gridManager = Object.FindFirstObjectByType<GridManager>(FindObjectsInactive.Include);
        if (gridManager != null)
        {
            so.FindProperty("_gridManager").objectReferenceValue = gridManager;
            Debug.Log($"[FixRefs] GridManager set: {gridManager.name}");
        }

        // uGUI GamePlayPanel の参照を設定
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name == "GamePlayPanel")
            {
                so.FindProperty("_uguiGamePlayPanel").objectReferenceValue = go;
                Debug.Log($"[FixRefs] uguiGamePlayPanel set: {go.name}");
                break;
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(screenMgr);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[FixRefs] Done. Save the scene.");
    }

    [MenuItem("Tools/SortGems/Reset PlayerPrefs (Clear Progress)")]
    public static void ResetPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[SortGems] PlayerPrefs cleared. All stage progress reset.");
    }
}
