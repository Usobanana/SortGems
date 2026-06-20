using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;
using System.Collections.Generic;
using SortGems.Core;
using SortGems.UI;

public class SetupUIToolkit : EditorWindow
{
    [MenuItem("Tools/SortGems/Setup UI Toolkit")]
    public static void Setup()
    {
        // 1. PanelSettings アセットを作成
        string panelSettingsPath = "Assets/UI/DefaultPanelSettings.asset";
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
        if (panelSettings == null)
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            var so = new SerializedObject(panelSettings);
            so.FindProperty("m_ScaleMode").enumValueIndex = 2; // ScaleWithScreenSize
            so.FindProperty("m_ScreenMatchMode").enumValueIndex = 0; // MatchWidthOrHeight
            so.FindProperty("m_Match").floatValue = 0f; // match width
            so.FindProperty("m_ReferenceResolution").vector2IntValue = new Vector2Int(1080, 1920);
            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(panelSettings, panelSettingsPath);
        }

        // 2. UXML アセット参照を取得
        var titleUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/TitleScreen.uxml");
        var stageSelectUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/StageSelectScreen.uxml");
        var gamePlayUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/GamePlayHUD.uxml");

        if (titleUxml == null || stageSelectUxml == null || gamePlayUxml == null)
        {
            Debug.LogError("[SetupUIToolkit] UXML files not found. Make sure they exist under Assets/UI/Screens/");
            return;
        }

        // 3. シーン内に UIDocument + ScreenManager を配置
        var existingDoc = Object.FindFirstObjectByType<UIDocument>();
        GameObject uiDocObj;
        UIDocument uiDoc;

        if (existingDoc != null)
        {
            uiDocObj = existingDoc.gameObject;
            uiDoc = existingDoc;
            Debug.Log("[SetupUIToolkit] Reusing existing UIDocument object.");
        }
        else
        {
            uiDocObj = new GameObject("[UIToolkit]");
            uiDoc = uiDocObj.AddComponent<UIDocument>();
        }

        uiDoc.panelSettings = panelSettings;

        // 4. ScreenManager をアタッチ
        var screenMgr = uiDocObj.GetComponent<ScreenManager>();
        if (screenMgr == null)
            screenMgr = uiDocObj.AddComponent<ScreenManager>();

        var smSo = new SerializedObject(screenMgr);
        smSo.FindProperty("_titleScreen").objectReferenceValue = titleUxml;
        smSo.FindProperty("_stageSelectScreen").objectReferenceValue = stageSelectUxml;
        smSo.FindProperty("_gamePlayHUD").objectReferenceValue = gamePlayUxml;

        // GameManager, GridView, GridManager の参照を探す
        var gameManager = Object.FindFirstObjectByType<GameManager>();
        var gridView = Object.FindFirstObjectByType<GridView>();
        var gridManager = Object.FindFirstObjectByType<GridManager>();

        if (gameManager != null)
            smSo.FindProperty("_gameManager").objectReferenceValue = gameManager;
        if (gridView != null)
            smSo.FindProperty("_gridView").objectReferenceValue = gridView;
        if (gridManager != null)
            smSo.FindProperty("_gridManager").objectReferenceValue = gridManager;

        // ステージリストのコピー（GameBootstrap から取得）
        var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
        if (bootstrap != null)
        {
            var bsSo = new SerializedObject(bootstrap);
            var srcStages = bsSo.FindProperty("_stages");
            var dstStages = smSo.FindProperty("_stages");
            dstStages.ClearArray();
            for (int i = 0; i < srcStages.arraySize; i++)
            {
                dstStages.InsertArrayElementAtIndex(i);
                dstStages.GetArrayElementAtIndex(i).objectReferenceValue =
                    srcStages.GetArrayElementAtIndex(i).objectReferenceValue;
            }
        }

        smSo.ApplyModifiedProperties();

        EditorUtility.SetDirty(uiDocObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SetupUIToolkit] UI Toolkit setup complete. UIDocument + ScreenManager configured.");
        Debug.Log("[SetupUIToolkit] Next steps: Disable old uGUI Canvas and GameBootstrap, then enter Play mode to test.");
    }
}
