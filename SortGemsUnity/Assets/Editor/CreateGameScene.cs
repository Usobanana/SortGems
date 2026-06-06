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
/// レイアウト構成:
///   TitlePanel        — タイトル画面
///   StageSelectPanel  — ステージ選択（動的スクロールリスト）
///   GamePlayPanel     — パズル画面
/// </summary>
public class CreateGameScene : EditorWindow
{
    // ---- レイアウト定数 ----
    const int REF_W         = 1080;
    const int REF_H         = 1920;
    const int HEADER_H      = 120;
    const float CELL_SIZE   = 41.5f;
    const float CELL_SPACING = 0f;
    const float PALETTE_GAP  = 24f;   // グリッドとパレットの隙間
    const int BUTTON_H      = 80;

    // ---- ステージ定数 ----
    const int MAIN_ROWS    = 16;
    const int MAIN_COLS    = 16;
    const int PAL_ROWS     = 4;
    const int PAL_COLS     = 8;

    // ---- 16x16 ピクセルアート定義 ----
    private static readonly string[] StarArt = new string[]
    {
        "................",
        ".......Y........",
        "......YOY.......",
        ".....YOOOY......",
        "..YYYOOOOOYYY...",
        "...YOOOOOOOY....",
        "....YOOCOOY.....",
        "...YOOOOOOOY....",
        "..YYYOOOOOYYY...",
        ".....YOOOY......",
        "......YOY.......",
        ".......Y........",
        "................",
        "................",
        "................",
        "................"
    };

    private static readonly string[] HeartArt = new string[]
    {
        "................",
        "....KK....KK....",
        "...KKKK..KKKK...",
        "..KKRRRRRRRRKK..",
        "..KRRRRRRRRRRK..",
        "...RRRBBBBRRR...",
        "....RRBBBRR.....",
        ".....RBRR.......",
        "......RR........",
        ".......R........",
        "................",
        "................",
        "................",
        "................",
        "................",
        "................"
    };

    private static readonly string[] TreeArt = new string[]
    {
        "................",
        "......YYYY......",
        "....GGGGGGGG....",
        "...GGGGGGGGGG...",
        "..GGGGGGGGGGGG..",
        "...GGGGGGGGGG...",
        "....GGGGGGGG....",
        "......GGGG......",
        ".......OO.......",
        ".......OO.......",
        ".......OO.......",
        "................",
        "................",
        "................",
        "................",
        "................"
    };

    private static readonly string[] FlowerArt = new string[]
    {
        "................",
        "......KKKK......",
        "....KKKKKKKK....",
        "...KKKYYYYKKK...",
        "..KKCYYYYYYCKK..",
        "..KKCYYYYYYCKK..",
        "...KKKYYYYKKK...",
        "....KKKKKKKK....",
        "......KKKK......",
        "................",
        "................",
        "................",
        "................",
        "................",
        "................",
        "................"
    };

    static Texture2D CreateArtTexture(string[] art, string path)
    {
        int width = 16;
        int height = 16;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color[] colors = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            string line = art[15 - y];
            for (int x = 0; x < width; x++)
            {
                char c = (x < line.Length) ? line[x] : '.';
                GemColor gc = c switch
                {
                    'R' => GemColor.Red,
                    'B' => GemColor.Blue,
                    'G' => GemColor.Green,
                    'Y' => GemColor.Yellow,
                    'P' => GemColor.Purple,
                    'O' => GemColor.Orange,
                    'C' => GemColor.Cyan,
                    'K' => GemColor.Pink,
                    _ => GemColor.None
                };

                colors[y * width + x] = gc != GemColor.None 
                    ? GemColorPalette.GetColor(gc) 
                    : Color.clear;
            }
        }

        tex.SetPixels(colors);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static StageData CreateStageFromArt(int number, string name, string[] art, float timeLimit)
    {
        string dir = "Assets/Textures/Stages";
        Directory.CreateDirectory(dir);
        string texPath = $"{dir}/Stage_{number:000}_Art.png";
        Texture2D tex = CreateArtTexture(art, texPath);

        string stagePath = $"Assets/ScriptableObjects/Stages/Stage_{number:000}.asset";
        Directory.CreateDirectory(Path.GetDirectoryName(stagePath)!);

        if (AssetDatabase.LoadAssetAtPath<StageData>(stagePath) != null)
            AssetDatabase.DeleteAsset(stagePath);

        var stageData = ScriptableObject.CreateInstance<StageData>();
        stageData.stageNumber = number;
        stageData.stageName = name;
        stageData.mainRows = 16;
        stageData.mainCols = 16;
        stageData.paletteRows = 4;
        stageData.paletteCols = 8;
        stageData.timeLimitSeconds = timeLimit;
        stageData.pixelArtTexture = tex;

        stageData.goalLayout = new List<StageData.CellColorDef>();
        stageData.initialMainCells = new List<StageData.CellColorDef>();

        List<StageData.CellColorDef> nonVoidCells = new List<StageData.CellColorDef>();

        for (int y = 0; y < 16; y++)
        {
            string line = art[y];
            for (int x = 0; x < 16; x++)
            {
                char c = (x < line.Length) ? line[x] : '.';
                GemColor gc = c switch
                {
                    'R' => GemColor.Red,
                    'B' => GemColor.Blue,
                    'G' => GemColor.Green,
                    'Y' => GemColor.Yellow,
                    'P' => GemColor.Purple,
                    'O' => GemColor.Orange,
                    'C' => GemColor.Cyan,
                    'K' => GemColor.Pink,
                    _ => GemColor.None
                };

                if (gc != GemColor.None)
                {
                    int row = y;
                    int col = x;
                    stageData.goalLayout.Add(new StageData.CellColorDef { row = row, col = col, color = gc });
                    nonVoidCells.Add(new StageData.CellColorDef { row = row, col = col, color = gc });
                }
            }
        }

        List<GemColor> colors = new List<GemColor>();
        foreach (var cell in nonVoidCells)
        {
            colors.Add(cell.color);
        }

        var rng = new System.Random(number * 100);
        int n = colors.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            GemColor val = colors[k];
            colors[k] = colors[n];
            colors[n] = val;
        }

        for (int i = 0; i < nonVoidCells.Count; i++)
        {
            stageData.initialMainCells.Add(new StageData.CellColorDef
            {
                row = nonVoidCells[i].row,
                col = nonVoidCells[i].col,
                color = colors[i]
            });
        }

        stageData.initialPaletteCells = new List<StageData.CellColorDef>();

        AssetDatabase.CreateAsset(stageData, stagePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return stageData;
    }

    // 16x16の幾何学的ピクセルアートをステージ番号に応じて自動生成する
    private static string[] GenerateProceduralArt(int stageNumber, out string name)
    {
        int pattern = stageNumber % 8;
        string[] art = new string[16];
        char[] colors = { 'Y', 'R', 'B', 'G', 'O', 'C', 'K', 'P' };
        
        char c1 = colors[(stageNumber) % colors.Length];
        char c2 = colors[(stageNumber + 2) % colors.Length];
        char c3 = colors[(stageNumber + 5) % colors.Length];

        switch (pattern)
        {
            case 0:
                name = "Circle";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        float dist = Vector2.Distance(new Vector2(r, c), new Vector2(7.5f, 7.5f));
                        if (dist < 7.5f)
                        {
                            if (dist < 3f) sb.Append(c1);
                            else if (dist < 5.5f) sb.Append(c2);
                            else sb.Append(c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
            case 1:
                name = "Diamond";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        int dist = Mathf.Abs(r - 8) + Mathf.Abs(c - 8);
                        if (dist <= 7)
                        {
                            if (dist <= 2) sb.Append(c1);
                            else if (dist <= 5) sb.Append(c2);
                            else sb.Append(c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
            case 2:
                name = "Square";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        if (r >= 2 && r < 14 && c >= 2 && c < 14)
                        {
                            int dist = Mathf.Max(Mathf.Abs(r - 8), Mathf.Abs(c - 8));
                            if (dist <= 2) sb.Append(c1);
                            else if (dist <= 4) sb.Append(c2);
                            else sb.Append(c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
            case 3:
                name = "Cross";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        bool isMainCross = Mathf.Abs(r - 8) <= 1 || Mathf.Abs(c - 8) <= 1;
                        bool isDiagCross = Mathf.Abs(r - c) <= 1 || Mathf.Abs(r + c - 15) <= 1;
                        
                        if ((isMainCross || isDiagCross) && (r >= 1 && r < 15 && c >= 1 && c < 15))
                        {
                            if (isMainCross && isDiagCross) sb.Append(c1);
                            else if (isMainCross) sb.Append(c2);
                            else sb.Append(c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
            case 4:
                name = "Spiral";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        float dist = Vector2.Distance(new Vector2(r, c), new Vector2(7.5f, 7.5f));
                        if (dist < 7.5f)
                        {
                            int angle = Mathf.FloorToInt(Mathf.Atan2(r - 7.5f, c - 7.5f) * Mathf.Rad2Deg + 180f);
                            int zone = (angle / 60) % 3;
                            if (zone == 0) sb.Append(c1);
                            else if (zone == 1) sb.Append(c2);
                            else sb.Append(c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
            case 5:
                name = "Stripe";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        float dist = Vector2.Distance(new Vector2(r, c), new Vector2(7.5f, 7.5f));
                        if (dist < 7.5f)
                        {
                            int val = (r + c) % 4;
                            if (val == 0) sb.Append(c1);
                            else if (val == 2) sb.Append(c2);
                            else sb.Append(c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
            case 6:
                name = "StarShape";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        float dx = Mathf.Abs(c - 7.5f);
                        float dy = Mathf.Abs(r - 7.5f);
                        if (dx + dy < 7.5f || (dx < 1.5f && dy < 7.5f) || (dy < 1.5f && dx < 7.5f))
                        {
                            if (dx + dy < 3.5f) sb.Append(c1);
                            else if (dx + dy < 6f) sb.Append(c2);
                            else sb.Append(c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
            default:
                name = "GridPattern";
                for (int r = 0; r < 16; r++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int c = 0; c < 16; c++)
                    {
                        float dist = Vector2.Distance(new Vector2(r, c), new Vector2(7.5f, 7.5f));
                        if (dist < 7.5f)
                        {
                            bool isEven = (r / 2 + c / 2) % 2 == 0;
                            if (dist < 4f) sb.Append(isEven ? c1 : c2);
                            else sb.Append(isEven ? c2 : c3);
                        }
                        else sb.Append('.');
                    }
                    art[r] = sb.ToString();
                }
                break;
        }

        return art;
    }

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

        // カメラ
        var cam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (cam) { cam.orthographic = true; cam.orthographicSize = 5f; }

        // ---- 2. Managers ----
        var mgr        = new GameObject("[Managers]");
        var gmObj      = Child(mgr, "GameManager");  var gm  = gmObj.AddComponent<GameManager>();
        var gridMgrObj = Child(mgr, "GridManager");  var grd = gridMgrObj.AddComponent<GridManager>();
        var adObj      = Child(mgr, "AdManager");    adObj.AddComponent<AdManager>();
        var sndObj     = Child(mgr, "SoundManager"); var snd = sndObj.AddComponent<SoundManager>();

        // Assets/Sound/BGM 以下の mp3 ファイルを自動ロード
        var bgmClipsList = new List<AudioClip>();
        string bgmDir = Path.Combine(Application.dataPath, "Sound/BGM");
        if (Directory.Exists(bgmDir))
        {
            string[] bgmFiles = Directory.GetFiles(bgmDir, "*.mp3", SearchOption.AllDirectories);
            foreach (var bgmFile in bgmFiles)
            {
                string relativePath = "Assets" + bgmFile.Substring(Application.dataPath.Length).Replace('\\', '/');
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);
                if (clip != null) bgmClipsList.Add(clip);
            }
        }

        // SoundManager の _bgmClips にシリアライズしてアサイン
        var sndSo = new SerializedObject(snd);
        var bgmClipsProp = sndSo.FindProperty("_bgmClips");
        bgmClipsProp.ClearArray();
        for (int i = 0; i < bgmClipsList.Count; i++)
        {
            bgmClipsProp.InsertArrayElementAtIndex(i);
            bgmClipsProp.GetArrayElementAtIndex(i).objectReferenceValue = bgmClipsList[i];
        }
        sndSo.ApplyModifiedProperties();

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
        scaler.matchWidthOrHeight  = 0f;

        canvasObj.AddComponent<GraphicRaycaster>();
        var gameUI = canvasObj.AddComponent<GameUI>();

        // ---- 5. Title Panel ----
        var titlePanel = MakeFullPanel("TitlePanel", canvasObj.transform, new Color(0.1f, 0.1f, 0.12f, 1f));
        titlePanel.SetActive(true);

        MakeText("TitleText", titlePanel.transform, "SORT GEMS", 64,
            new Vector2(0, 0.7f), new Vector2(1, 0.9f), Vector2.zero, Vector2.zero)
            .color = Color.white;

        var playBtn = CreateButton("PlayButton", "PLAY", titlePanel.transform, new Vector2(0, -100), new Vector2(300, 100));

        // ---- 6. Stage Select Panel ----
        var stageSelectPanel = MakeFullPanel("StageSelectPanel", canvasObj.transform, new Color(0.1f, 0.1f, 0.12f, 1f));
        stageSelectPanel.SetActive(false);

        MakeText("SelectTitle", stageSelectPanel.transform, "SELECT STAGE", 48,
            new Vector2(0, 0.85f), new Vector2(1, 0.95f), Vector2.zero, Vector2.zero)
            .color = Color.white;

        var selectBackBtn = CreateButton("BackButton", "Back", stageSelectPanel.transform, Vector2.zero, new Vector2(120, 60));
        var selectBackRt = selectBackBtn.GetComponent<RectTransform>();
        selectBackRt.anchorMin = new Vector2(0f, 1f);
        selectBackRt.anchorMax = new Vector2(0f, 1f);
        selectBackRt.pivot = new Vector2(0f, 1f);
        selectBackRt.anchoredPosition = new Vector2(40f, -40f);

        // スクロールコンテナ
        var scrollRectObj = MakeRect("StageListScroll", stageSelectPanel.transform,
            new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var scrollRect = scrollRectObj.gameObject.AddComponent<ScrollRect>();
        
        var viewport = MakeRect("Viewport", scrollRectObj, Vector2.zero, Vector2.one, new Vector2(0, 1), Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.2f);
        viewport.gameObject.AddComponent<Mask>();
        
        var scrollContent = MakeRect("Content", viewport, Vector2.zero, new Vector2(1, 0), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 800));
        var contentLayout = scrollContent.gameObject.AddComponent<GridLayoutGroup>();
        contentLayout.cellSize = new Vector2(180, 180);
        contentLayout.spacing = new Vector2(24, 24);
        contentLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        contentLayout.constraintCount = 4;
        contentLayout.padding = new RectOffset(20, 20, 20, 20);

        scrollRect.viewport = viewport;
        scrollRect.content = scrollContent;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        var stageBtnTemplate = CreateButton("StageButtonTemplate", "", scrollContent, Vector2.zero, new Vector2(180, 180));
        stageBtnTemplate.gameObject.SetActive(false);

        // 既存の Text の RectTransform を下部に縮小調整
        var templateText = stageBtnTemplate.transform.Find("Text")?.GetComponent<Text>();
        if (templateText != null)
        {
            templateText.fontSize = 16;
            templateText.text = "1\nStageName";
            var textRt = templateText.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 0.25f);
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = Vector2.zero;
        }

        // プレビューImage の追加 (中央より少し上に配置、120x120)
        var previewImg = MakeImageChild("PreviewImage", stageBtnTemplate.transform, Color.clear, 
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var previewRt = previewImg.GetComponent<RectTransform>();
        previewRt.anchoredPosition = new Vector2(0f, 15f); // 少し上にずらす
        previewRt.sizeDelta = new Vector2(120f, 120f);

        // ---- 7. Game Play Panel ----
        var gamePlayPanel = MakeFullPanel("GamePlayPanel", canvasObj.transform, new Color(0.15f, 0.15f, 0.18f, 1f));
        gamePlayPanel.SetActive(false);

        // ---- 8. Header（パズル用）----
        var headerObj  = MakeRect("Header", gamePlayPanel.transform,
            ancMin: new Vector2(0,1), ancMax: new Vector2(1,1),
            pivot: new Vector2(0.5f,1), pos: Vector2.zero, size: new Vector2(0, HEADER_H));

        var gamePlayBackBtn = CreateButton("BackButton", "Back", headerObj, Vector2.zero, new Vector2(120, 60));
        var gamePlayBackRt = gamePlayBackBtn.GetComponent<RectTransform>();
        gamePlayBackRt.anchorMin = new Vector2(0f, 0.5f);
        gamePlayBackRt.anchorMax = new Vector2(0f, 0.5f);
        gamePlayBackRt.pivot = new Vector2(0f, 0.5f);
        gamePlayBackRt.anchoredPosition = new Vector2(20f, 0f);

        var stageNameText = MakeText("StageNameText", headerObj,
            "Stage Name", 32,
            ancMin: new Vector2(0.2f,0), ancMax: new Vector2(0.6f,1),
            pos: Vector2.zero, size: Vector2.zero);

        var timerArea = MakeRect("TimerArea", headerObj,
            ancMin: new Vector2(0.6f,0), ancMax: new Vector2(1,1),
            pivot: new Vector2(0.5f,0.5f), pos: Vector2.zero, size: Vector2.zero);

        var (slider, fillImage) = MakeSlider("TimerSlider", timerArea.transform);

        var timerText = MakeText("TimerText", timerArea.transform,
            "05:00", 28,
            ancMin: new Vector2(0,0), ancMax: new Vector2(1,0.45f),
            pos: Vector2.zero, size: Vector2.zero);

        // ---- 9. Main Grid ----
        float gridPx  = MAIN_COLS * (CELL_SIZE + CELL_SPACING);
        float gridH   = MAIN_ROWS * (CELL_SIZE + CELL_SPACING);

        float palH = PAL_ROWS * (CELL_SIZE + CELL_SPACING);
        float contentH = gridH + PALETTE_GAP + palH;
        float remainingH = (REF_H - HEADER_H - BUTTON_H) - contentH;
        float topMargin = remainingH / 2f;
        float gridTop = HEADER_H + topMargin;

        var gridContainerObj = MakeRect("GridContainer", gamePlayPanel.transform,
            ancMin: new Vector2(0.5f,1), ancMax: new Vector2(0.5f,1),
            pivot: new Vector2(0.5f,1),
            pos: new Vector2(0, -gridTop),
            size: new Vector2(gridPx, gridH));

        var gridView   = gridContainerObj.gameObject.AddComponent<GridView>();
        var mainLayout = gridContainerObj.gameObject.AddComponent<GridLayoutGroup>();
        ConfigureLayout(mainLayout, MAIN_COLS, CELL_SIZE, CELL_SPACING);

        // ---- 10. Palette ----
        float palW = PAL_COLS * (CELL_SIZE + CELL_SPACING);
        float palTop = gridTop + gridH + PALETTE_GAP;

        var paletteObj = MakeRect("PaletteContainer", gamePlayPanel.transform,
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

        // ---- 11. ButtonBar ----
        var buttonBarObj = MakeRect("ButtonBar", gamePlayPanel.transform,
            ancMin: new Vector2(0,0), ancMax: new Vector2(1,0),
            pivot: new Vector2(0.5f,0), pos: Vector2.zero, size: new Vector2(0, BUTTON_H));

        var undoBtn  = CreateButton("UndoButton",  "Undo",  buttonBarObj.transform, new Vector2(-160,0), new Vector2(120,56));
        var hintBtn  = CreateButton("HintButton",  "Hint",  buttonBarObj.transform, new Vector2(0,0),    new Vector2(120,56));
        var resetBtn = CreateButton("ResetButton", "Reset", buttonBarObj.transform, new Vector2(160,0),  new Vector2(120,56));

        // ---- 12. Cleared / Failed パネル ----
        var clearedPanel    = MakeFullPanel("ClearedPanel", gamePlayPanel.transform, new Color(0f, 0f, 0f, 0.65f));
        MakeText("ClearedText", clearedPanel.transform, "STAGE CLEAR!", 52,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0, 700f), new Vector2(800, 100))
            .color = Color.yellow;
        var nextBtn         = CreateButton("NextStageButton", "Next Stage", clearedPanel.transform, new Vector2(0, -340f), new Vector2(280, 72));
        var replayBtnC      = CreateButton("ReplayButton",    "Replay",     clearedPanel.transform, new Vector2(0, -460f), new Vector2(240, 64));
        var backBtnC        = CreateButton("BackButton",      "Back",       clearedPanel.transform, new Vector2(0, -580f), new Vector2(240, 64));

        var failedPanel     = MakeFullPanel("FailedPanel", gamePlayPanel.transform, new Color(0f, 0f, 0f, 0.65f));
        MakeText("FailedText", failedPanel.transform, "TIME'S UP!", 52,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0, 700f), new Vector2(800, 100))
            .color = Color.red;
        var addTimeBtn      = CreateButton("AddTimeButton", "Watch Ad +1:00", failedPanel.transform, new Vector2(0, -340f), new Vector2(300, 72));
        var replayBtnF      = CreateButton("ReplayButton",  "Retry",          failedPanel.transform, new Vector2(0, -460f), new Vector2(240, 64));
        var backBtnF        = CreateButton("BackButton",    "Back",           failedPanel.transform, new Vector2(0, -580f), new Vector2(240, 64));
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
        SetRef(gameUI, "_clearedBackButton",    backBtnC);
        SetRef(gameUI, "_failedPanel",          failedPanel);
        SetRef(gameUI, "_addTimeButton",        addTimeBtn);
        SetRef(gameUI, "_replayButton_Failed",  replayBtnF);
        SetRef(gameUI, "_failedBackButton",     backBtnF);
        SetRef(gameUI, "_addTimeLabel",         addTimeLabel);
        SetRef(gameUI, "_gridView",             gridView);
        SetRef(gameUI, "_gridManager",          grd);
        
        SetRef(gameUI, "_playButton",           playBtn);
        SetRef(gameUI, "_stageSelectBackButton",selectBackBtn);
        SetRef(gameUI, "_gamePlayBackButton",   gamePlayBackBtn);
        new SerializedObject(gameUI).ApplyModifiedProperties();

        // ピクセルアート（GridContainer）がクリア/失敗時の黒マスクより手前に描画されるように最前面に移動
        gridContainerObj.SetAsLastSibling();

        // ---- 13. GemCell Prefab ----
        var prefabPath = "Assets/Prefabs/GemCell.prefab";
        Directory.CreateDirectory(Path.GetDirectoryName(prefabPath)!);

        var cellRoot = new GameObject("GemCell");
        var cellView = cellRoot.AddComponent<GemCellView>();
        cellRoot.AddComponent<RectTransform>().sizeDelta = new Vector2(CELL_SIZE, CELL_SIZE);

        var aspect = cellRoot.AddComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
        aspect.aspectRatio = 1.0f;

        var bgImg  = MakeImageChild("BackgroundImage", cellRoot.transform,
                        new Color(0.18f, 0.18f, 0.22f, 1f), Vector2.zero, Vector2.one);
        var socketImg = MakeImageChild("SocketImage", cellRoot.transform,
                        new Color(0f, 0f, 0f, 0.55f), Vector2.zero, Vector2.one);
        var gemImg = MakeImageChild("GemImage", cellRoot.transform,
                        Color.white, Vector2.zero, Vector2.one);
        
        var bevelShadow = MakeImageChild("BevelShadow", gemImg.transform,
                            Color.white, Vector2.zero, Vector2.one);
        bevelShadow.sprite = GemColorPalette.BevelShadowSprite;

        var shadowEff = gemImg.gameObject.AddComponent<Shadow>();
        shadowEff.effectColor = new Color(0f, 0f, 0f, 0.48f);
        shadowEff.effectDistance = new Vector2(2f, -2f);

        var mark   = MakeImageChild("CompletedMark", cellRoot.transform,
                        new Color(1f, 1f, 1f, 0.4f), new Vector2(0.15f, 0.55f), new Vector2(0.85f, 0.85f));
        mark.sprite = GemColorPalette.RoundedRectSprite;
        mark.gameObject.SetActive(false);

        SetRef(cellView, "_gemImage",       gemImg);
        SetRef(cellView, "_backgroundImage",bgImg);
        SetRef(cellView, "_socketImage",    socketImg);
        SetRef(cellView, "_completedMark",  mark.gameObject);

        var trigger = cellRoot.AddComponent<EventTrigger>();
        var entry   = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(entry.callback, cellView.OnPointerClick);
        trigger.triggers.Add(entry);

        var gemCellPrefab = PrefabUtility.SaveAsPrefabAsset(cellRoot, prefabPath);
        Object.DestroyImmediate(cellRoot);

        SetRef(gridView, "_cellPrefab", gemCellPrefab.GetComponent<GemCellView>());
        new SerializedObject(gridView).ApplyModifiedProperties();

        // ---- 14. ステージアセット生成 ----
        List<StageData> stages = new List<StageData>();
        stages.Add(CreateStageFromArt(1, "Star", StarArt, 300f));
        stages.Add(CreateStageFromArt(2, "Heart", HeartArt, 300f));
        stages.Add(CreateStageFromArt(3, "Tree", TreeArt, 300f));
        stages.Add(CreateStageFromArt(4, "Flower", FlowerArt, 300f));

        for (int i = 5; i <= 100; i++)
        {
            string stageName;
            string[] art = GenerateProceduralArt(i, out stageName);
            stages.Add(CreateStageFromArt(i, stageName, art, 300f));
        }

        // ---- 15. GameBootstrap ----
        var bootstrap = new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
        SetRef(bootstrap, "_gameManager", gm);
        SetRef(bootstrap, "_gridView",    gridView);
        SetRef(bootstrap, "_gameUI",      gameUI);

        SetRef(bootstrap, "_titlePanel",       titlePanel);
        SetRef(bootstrap, "_stageSelectPanel", stageSelectPanel);
        SetRef(bootstrap, "_gamePlayPanel",    gamePlayPanel);
        SetRef(bootstrap, "_stageButtonContent",scrollContent.transform);
        SetRef(bootstrap, "_stageButtonPrefab", stageBtnTemplate.gameObject);

        // ステージリスト（_stages）のシリアライズ
        var bootstrapSo = new SerializedObject(bootstrap);
        var stagesProp = bootstrapSo.FindProperty("_stages");
        stagesProp.ClearArray();
        for (int i = 0; i < stages.Count; i++)
        {
            stagesProp.InsertArrayElementAtIndex(i);
            stagesProp.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
        }
        bootstrapSo.ApplyModifiedProperties();

        // ---- 16. Audio Visualizer シングルトンシステムの自動生成 ----
        // 画面最下部に固定され、シーン切り替え時も常駐する Canvas
        var visualizerCanvasObj = new GameObject("[DontDestroyVisualizer]");
        var vizCanvas = visualizerCanvasObj.AddComponent<Canvas>();
        vizCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        vizCanvas.sortingOrder = 999; // 他の全UIの最前面に描画

        var vizScaler = visualizerCanvasObj.AddComponent<CanvasScaler>();
        vizScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        vizScaler.referenceResolution = new Vector2(REF_W, REF_H);
        vizScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        vizScaler.matchWidthOrHeight = 0f;

        visualizerCanvasObj.AddComponent<GraphicRaycaster>(); // UIイベント遮断防止のため、子要素のRaycastTargetはOFFになります

        // バーを配置する親コンテナ (RectTransform)
        // 画面の最下部（Bottom）にアンカーを固定
        var containerRt = MakeRect("VisualizerContainer", visualizerCanvasObj.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), 
            new Vector2(0f, 0f), new Vector2(0f, 220f)); // 画面幅いっぱいにストレッチ、高さ220f

        // マネージャーの追加
        var vizManager = visualizerCanvasObj.AddComponent<VisualizerManager>();

        // インスペクター値を SerializedObject からセット
        var vizSo = new SerializedObject(vizManager);
        vizSo.FindProperty("_container").objectReferenceValue = containerRt;
        vizSo.FindProperty("_barCount").intValue = 32; // 32本のバー
        vizSo.FindProperty("_spacing").floatValue = 2f; // 間隔2px
        vizSo.FindProperty("_sensitivity").floatValue = 2500f; // 感度
        vizSo.FindProperty("_lerpSpeed").floatValue = 12f; // スムーズさ
        vizSo.FindProperty("_barColor").colorValue = new Color(1f, 1f, 1f, 0.45f); // 白色、透明度 0.45f
        vizSo.FindProperty("_minHeight").floatValue = 10f;
        vizSo.FindProperty("_maxHeight").floatValue = 200f;
        vizSo.ApplyModifiedProperties();

        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SortGems] GameScene 構築完了 — Title, StageSelect, GamePlay の3パネル構成 + 16x16ピクセルアート4ステージ + BGM自動割当 + ビジュアライザー生成");
    }

    // ===== ヘルパー =====

    static void ConfigureLayout(GridLayoutGroup layout, int cols, float cellSize, float spacing)
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
        var prop = so.FindProperty(propName);
        if (prop == null)
        {
            Debug.LogError($"[CreateGameScene] Property '{propName}' not found on {target.name}");
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }
}
