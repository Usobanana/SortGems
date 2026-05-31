using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections.Generic;
using SortGems.Core;
using SortGems.UI;
using SortGems.Ads;

/// <summary>
/// モバイル縦画面（1080×1920）レイアウトでゲームシーンを自動生成する。
///
/// レイアウト構成（上から）:
///   Header     120px  — ステージ名 / タイマー
///   MainGrid  1056px  — 24×24グリッド (44px/cell, spacing 0)
///   Palette    176px  — 16×4パレット (44px/cell, spacing 0)
///   ButtonBar   80px  — Undo / Hint / Reset
///   ─────────────────
///   合計       1432px  (1920px 内に収まる)
/// </summary>
public class CreateGameScene : EditorWindow
{
    // ---- レイアウト定数 ----
    const int REF_W         = 1080;
    const int REF_H         = 1920;
    const int HEADER_H      = 120;
    const int CELL_SIZE     = 44;
    const int CELL_SPACING  = 0;
    const int PALETTE_GAP   = 24;   // グリッドとパレットの隙間
    const int BUTTON_H      = 80;

    // ---- ステージ定数 ----
    const int MAIN_ROWS    = 24;
    const int MAIN_COLS    = 24;
    const int PAL_ROWS     = 4;
    const int PAL_COLS     = 16;

    [MenuItem("Tools/SortGems/Create Game Scene")]
    public static void CreateScene()
    {
        // GoogleMobileAdsSettings.asset が存在しない場合は、ダミーのAppIDを設定したアセットを自動生成してパッケージのエラーを防ぐ
        string settingsDir = Path.Combine(Application.dataPath, "GoogleMobileAds/Resources");
        string settingsPath = Path.Combine(settingsDir, "GoogleMobileAdsSettings.asset");
        if (!File.Exists(settingsPath))
        {
            Directory.CreateDirectory(settingsDir);
            string yaml = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: a187246822bbb47529482707f3e0eff8, type: 3}
  m_Name: GoogleMobileAdsSettings
  m_EditorClassIdentifier: 
  adMobAndroidAppId: ca-app-pub-3940256099942544~3347511713
  adMobIOSAppId: ca-app-pub-3940256099942544~1458002511
  enableKotlinXCoroutinesPackagingOption: 1
  optimizeInitialization: 0
  optimizeAdLoading: 0
  userTrackingUsageDescription: 
  validateGradleDependencies: 1
";
            File.WriteAllText(settingsPath, yaml);
            AssetDatabase.Refresh();
        }

        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
        }

        // ---- 1. シーン ----
        var scenePath = "Assets/Scenes/GameScene.unity";
        Directory.CreateDirectory(Path.GetDirectoryName(scenePath)!);
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
            UnityEditor.SceneManagement.NewSceneMode.Single);

        // カメラ（念のため Orthographic に）
        var cam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (cam) { cam.orthographic = true; cam.orthographicSize = 5f; }

        // ---- 2. Managers ----
        var mgr        = new GameObject("[Managers]");
        var gmObj      = Child(mgr, "GameManager");  var gm  = gmObj.AddComponent<GameManager>();
        var gridMgrObj = Child(mgr, "GridManager");  var grd = gridMgrObj.AddComponent<GridManager>();
        var adObj      = Child(mgr, "AdManager");    adObj.AddComponent<AdManager>();
        var sndObj     = Child(mgr, "SoundManager"); sndObj.AddComponent<SoundManager>();

        SetRef(gm, "_gridManager", grd);

        // ---- 3. EventSystem ----
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // ---- 4. Canvas ----
        var canvasObj = new GameObject("[Canvas]");
        var canvas    = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(REF_W, REF_H);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0f;   // 幅基準でスケール

        canvasObj.AddComponent<GraphicRaycaster>();
        var gameUI = canvasObj.AddComponent<GameUI>();

        // ---- 5. Header（上部 120px）----
        //  ┌─ StageNameText (左半分)  ─┬─ TimerArea (右半分) ─┐
        var headerObj  = MakeRect("Header", canvasObj.transform,
            ancMin: new Vector2(0,1), ancMax: new Vector2(1,1),
            pivot: new Vector2(0.5f,1), pos: Vector2.zero, size: new Vector2(0, HEADER_H));

        var stageNameText = MakeText("StageNameText", headerObj.transform,
            "Stage Name", 32,
            ancMin: new Vector2(0,0), ancMax: new Vector2(0.5f,1),
            pos: Vector2.zero, size: Vector2.zero);

        var timerArea = MakeRect("TimerArea", headerObj.transform,
            ancMin: new Vector2(0.5f,0), ancMax: Vector2.one,
            pivot: new Vector2(0.5f,0.5f), pos: Vector2.zero, size: Vector2.zero);

        var (slider, fillImage) = MakeSlider("TimerSlider", timerArea.transform);

        var timerText = MakeText("TimerText", timerArea.transform,
            "05:00", 28,
            ancMin: new Vector2(0,0), ancMax: new Vector2(1,0.45f),
            pos: Vector2.zero, size: Vector2.zero);

        // ---- 6. Main Grid ----
        int gridPx  = MAIN_COLS * (CELL_SIZE + CELL_SPACING);  // 24×44 = 1056
        int gridH   = MAIN_ROWS * (CELL_SIZE + CELL_SPACING);  // 1056

        var gridContainerObj = MakeRect("GridContainer", canvasObj.transform,
            ancMin: new Vector2(0.5f,1), ancMax: new Vector2(0.5f,1),
            pivot: new Vector2(0.5f,1),
            pos: new Vector2(0, -HEADER_H),
            size: new Vector2(gridPx, gridH));

        var gridView   = gridContainerObj.gameObject.AddComponent<GridView>();
        var mainLayout = gridContainerObj.gameObject.AddComponent<GridLayoutGroup>();
        ConfigureLayout(mainLayout, MAIN_COLS, CELL_SIZE, CELL_SPACING);

        // ---- 7. Palette（グリッドの直下）----
        int palW = PAL_COLS * (CELL_SIZE + CELL_SPACING);  // 16×44 = 704
        int palH = PAL_ROWS * (CELL_SIZE + CELL_SPACING);  // 4×44  = 176
        int palTop = HEADER_H + gridH + PALETTE_GAP;       // 120+1056+24 = 1200

        var paletteObj = MakeRect("PaletteContainer", canvasObj.transform,
            ancMin: new Vector2(0.5f,1), ancMax: new Vector2(0.5f,1),
            pivot: new Vector2(0.5f,1),
            pos: new Vector2(0, -palTop),
            size: new Vector2(palW, palH));

        var paletteLayout = paletteObj.gameObject.AddComponent<GridLayoutGroup>();
        ConfigureLayout(paletteLayout, PAL_COLS, CELL_SIZE, CELL_SPACING);

        // GridView 参照
        SetRef(gridView, "_gridManager",   grd);
        SetRef(gridView, "_mainLayout",    mainLayout);
        SetRef(gridView, "_paletteLayout", paletteLayout);

        // ---- 8. ButtonBar（下部 80px）----
        var buttonBarObj = MakeRect("ButtonBar", canvasObj.transform,
            ancMin: new Vector2(0,0), ancMax: new Vector2(1,0),
            pivot: new Vector2(0.5f,0), pos: Vector2.zero, size: new Vector2(0, BUTTON_H));

        var undoBtn  = CreateButton("UndoButton",  "Undo",  buttonBarObj.transform, new Vector2(-160,0), new Vector2(120,56));
        var hintBtn  = CreateButton("HintButton",  "Hint",  buttonBarObj.transform, new Vector2(0,0),    new Vector2(120,56));
        var resetBtn = CreateButton("ResetButton", "Reset", buttonBarObj.transform, new Vector2(160,0),  new Vector2(120,56));

        // ---- 9. Cleared / Failed パネル ----
        var clearedPanel    = MakeFullPanel("ClearedPanel", canvasObj.transform, new Color(0,0,0,0.82f));
        MakeText("ClearedText", clearedPanel.transform, "STAGE CLEAR!", 52,
            new Vector2(0,0), Vector2.one, new Vector2(0,80), Vector2.zero)
            .color = Color.yellow;
        var nextBtn         = CreateButton("NextStageButton", "Next Stage", clearedPanel.transform, new Vector2(0,-40),  new Vector2(220,64));
        var replayBtnC      = CreateButton("ReplayButton",    "Replay",     clearedPanel.transform, new Vector2(0,-120), new Vector2(220,64));

        var failedPanel     = MakeFullPanel("FailedPanel", canvasObj.transform, new Color(0,0,0,0.82f));
        MakeText("FailedText", failedPanel.transform, "TIME'S UP!", 52,
            new Vector2(0,0), Vector2.one, new Vector2(0,80), Vector2.zero)
            .color = Color.red;
        var addTimeBtn      = CreateButton("AddTimeButton", "Watch Ad +1:00", failedPanel.transform, new Vector2(0,-40),  new Vector2(260,64));
        var replayBtnF      = CreateButton("ReplayButton",  "Retry",          failedPanel.transform, new Vector2(0,-120), new Vector2(220,64));
        var addTimeLabel    = addTimeBtn.transform.Find("Text").GetComponent<Text>();

        // GameUI 参照
        SetRef(gameUI, "_timerSlider",         slider);
        SetRef(gameUI, "_timerText",            timerText);
        SetRef(gameUI, "_timerFill",            fillImage);
        SetRef(gameUI, "_stageNameText",        stageNameText);
        SetRef(gameUI, "_undoButton",           undoBtn);
        SetRef(gameUI, "_hintButton",           hintBtn);
        SetRef(gameUI, "_resetButton",          resetBtn);
        SetRef(gameUI, "_clearedPanel",         clearedPanel);
        SetRef(gameUI, "_nextStageButton",      nextBtn);
        SetRef(gameUI, "_replayButton_Cleared", replayBtnC);
        SetRef(gameUI, "_failedPanel",          failedPanel);
        SetRef(gameUI, "_addTimeButton",        addTimeBtn);
        SetRef(gameUI, "_replayButton_Failed",  replayBtnF);
        SetRef(gameUI, "_addTimeLabel",         addTimeLabel);
        SetRef(gameUI, "_gridView",             gridView);
        SetRef(gameUI, "_gridManager",          grd);
        new SerializedObject(gameUI).ApplyModifiedProperties();

        // ---- 10. GemCell Prefab ----
        var prefabPath = "Assets/Prefabs/GemCell.prefab";
        Directory.CreateDirectory(Path.GetDirectoryName(prefabPath)!);

        var cellRoot = new GameObject("GemCell");
        var cellView = cellRoot.AddComponent<GemCellView>();
        cellRoot.AddComponent<RectTransform>().sizeDelta = new Vector2(CELL_SIZE, CELL_SIZE);

        var bgImg  = MakeImageChild("BackgroundImage", cellRoot.transform,
                        new Color(0.18f, 0.18f, 0.22f, 1f), Vector2.zero, Vector2.one);
        var gemImg = MakeImageChild("GemImage", cellRoot.transform,
                        Color.white, new Vector2(0.1f,0.1f), new Vector2(0.9f,0.9f));
        var mark   = MakeImageChild("CompletedMark", cellRoot.transform,
                        new Color(0.5f,1f,0.5f,0.8f), new Vector2(0.65f,0.65f), Vector2.one);
        mark.gameObject.SetActive(false);

        SetRef(cellView, "_gemImage",       gemImg);
        SetRef(cellView, "_backgroundImage",bgImg);
        SetRef(cellView, "_completedMark",  mark.gameObject);

        var trigger = cellRoot.AddComponent<EventTrigger>();
        var entry   = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(entry.callback, cellView.OnPointerClick);
        trigger.triggers.Add(entry);

        var gemCellPrefab = PrefabUtility.SaveAsPrefabAsset(cellRoot, prefabPath);
        Object.DestroyImmediate(cellRoot);

        SetRef(gridView, "_cellPrefab", gemCellPrefab.GetComponent<GemCellView>());
        new SerializedObject(gridView).ApplyModifiedProperties();

        // ---- 11. Stage_001（24×24, palette 16×4）----
        var stagePath = "Assets/ScriptableObjects/Stages/Stage_001.asset";
        Directory.CreateDirectory(Path.GetDirectoryName(stagePath)!);
        if (AssetDatabase.LoadAssetAtPath<StageData>(stagePath) != null)
            AssetDatabase.DeleteAsset(stagePath);

        var stageData = ScriptableObject.CreateInstance<StageData>();
        stageData.stageNumber     = 1;
        stageData.stageName       = "Tutorial 1";
        stageData.mainRows        = MAIN_ROWS;   // 24
        stageData.mainCols        = MAIN_COLS;   // 24
        stageData.paletteRows     = PAL_ROWS;    // 4
        stageData.paletteCols     = PAL_COLS;    // 16
        stageData.timeLimitSeconds = 480f;       // 8分

        // ゴール配置: 4色を6×4のブロックで分割
        //   Red:    row  0-11, col  0-11  (左上 12×12)
        //   Blue:   row  0-11, col 12-23  (右上 12×12)
        //   Green:  row 12-23, col  0-11  (左下 12×12)
        //   Yellow: row 12-23, col 12-23  (右下 12×12)
        stageData.goalLayout = new List<StageData.CellColorDef>();
        FillBlock(stageData.goalLayout,  0, 12,  0, 12, GemColor.Red);
        FillBlock(stageData.goalLayout,  0, 12, 12, 24, GemColor.Blue);
        FillBlock(stageData.goalLayout, 12, 24,  0, 12, GemColor.Green);
        FillBlock(stageData.goalLayout, 12, 24, 12, 24, GemColor.Yellow);

        // 初期配置: 同じジェムを市松模様に混在（=毎行 R B G Y R B G Y...）
        stageData.initialMainCells = new List<StageData.CellColorDef>();
        var colorCycle = new[] { GemColor.Red, GemColor.Blue, GemColor.Green, GemColor.Yellow };
        // メイングリッドの 24×24 すべてのマスにジェムをぎっしり配置
        for (int r = 0; r < MAIN_ROWS; r++)
            for (int c = 0; c < MAIN_COLS; c++)
            {
                int colorIdx = (r + c) % 4;
                stageData.initialMainCells.Add(new StageData.CellColorDef
                    { row = r, col = c, color = colorCycle[colorIdx] });
            }

        stageData.initialPaletteCells = new List<StageData.CellColorDef>();

        AssetDatabase.CreateAsset(stageData, stagePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ---- 12. GameBootstrap ----
        var bootstrap = new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
        SetRef(bootstrap, "_gameManager", gm);
        SetRef(bootstrap, "_gridView",    gridView);
        SetRef(bootstrap, "_gameUI",      gameUI);
        SetRef(bootstrap, "_testStage",   stageData);
        new SerializedObject(bootstrap).ApplyModifiedProperties();

        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[SortGems] GameScene 生成完了 — 24×24グリッド / 16×4パレット / モバイル縦レイアウト");
    }

    // ===== ヘルパー =====

    static void FillBlock(List<StageData.CellColorDef> list,
                          int rMin, int rMax, int cMin, int cMax, GemColor color)
    {
        for (int r = rMin; r < rMax; r++)
            for (int c = cMin; c < cMax; c++)
                list.Add(new StageData.CellColorDef { row = r, col = c, color = color });
    }

    static void ConfigureLayout(GridLayoutGroup layout, int cols, int cellSize, int spacing)
    {
        layout.constraint       = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount  = cols;
        layout.cellSize         = new Vector2(cellSize, cellSize);
        layout.spacing          = new Vector2(spacing, spacing);
        layout.padding          = new RectOffset(0, 0, 0, 0);
    }

    static GameObject Child(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
    }

    static RectTransform MakeRect(string name, Transform parent,
        Vector2 ancMin, Vector2 ancMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = ancMin;
        rect.anchorMax        = ancMax;
        rect.pivot            = pivot;
        rect.anchoredPosition = pos;
        rect.sizeDelta        = size;
        return rect;
    }

    static Text MakeText(string name, Transform parent, string content, int fontSize,
        Vector2 ancMin, Vector2 ancMax, Vector2 pos, Vector2 size)
    {
        var rt   = MakeRect(name, parent, ancMin, ancMax, new Vector2(0.5f,0.5f), pos, size);
        var text = rt.gameObject.AddComponent<Text>();
        text.text      = content;
        text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize  = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color     = Color.white;
        return text;
    }

    static (Slider slider, Image fill) MakeSlider(string name, Transform parent)
    {
        var rt   = MakeRect(name, parent,
            new Vector2(0,0.5f), new Vector2(1,0.5f),
            new Vector2(0.5f,0.5f), new Vector2(0,10), new Vector2(-20,24));
        var slider = rt.gameObject.AddComponent<Slider>();

        var bg    = MakeImageChild("Background", rt, new Color(0.25f,0.25f,0.25f,1), Vector2.zero, Vector2.one);
        var area  = MakeRect("Fill Area", rt, Vector2.zero, Vector2.one, new Vector2(0,0.5f), Vector2.zero, Vector2.zero);
        var fill  = MakeImageChild("Fill", area, Color.green, Vector2.zero, Vector2.one);

        slider.fillRect     = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fill;
        slider.value         = 1f;
        return (slider, fill);
    }

    static Image MakeImageChild(string name, Transform parent,
        Color color, Vector2 ancMin, Vector2 ancMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var r   = img.GetComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.sizeDelta = Vector2.zero;
        return img;
    }

    static GameObject MakeFullPanel(string name, Transform parent, Color bg)
    {
        var rt  = MakeRect(name, parent, Vector2.zero, Vector2.one,
                           new Vector2(0.5f,0.5f), Vector2.zero, Vector2.zero);
        rt.gameObject.AddComponent<Image>().color = bg;
        rt.gameObject.SetActive(false);
        return rt.gameObject;
    }

    static Button CreateButton(string name, string label, Transform parent, Vector2 pos, Vector2 size)
    {
        var rt  = MakeRect(name, parent,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(0.5f,0.5f), pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0.28f, 0.28f, 0.38f, 1f);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        MakeText("Text", rt, label, 22,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return btn;
    }

    // SerializedObject への参照セット（型を自動判別）
    static void SetRef<T>(Object target, string propName, T value) where T : Object
    {
        var so = new SerializedObject(target);
        so.FindProperty(propName).objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }
}
