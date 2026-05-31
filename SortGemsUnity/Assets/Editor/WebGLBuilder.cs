using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class WebGLBuilder
{
    [MenuItem("Tools/SortGems/Build WebGL")]
    public static void Build()
    {
        // GoogleMobileAdsSettings.asset を事前に自動生成して、ビルド時のAppIDチェックエラーおよびパッケージ内のPrefab上書き保存エラーを回避する
        string settingsDir = Path.Combine(Application.dataPath, "GoogleMobileAds/Resources");
        string settingsPath = Path.Combine(settingsDir, "GoogleMobileAdsSettings.asset");
        if (!File.Exists(settingsPath))
        {
            Debug.Log("[WebGLBuilder] GoogleMobileAdsSettings.asset が存在しないため、ダミーのAppIDを設定したアセットを自動生成します。");
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

        string projectDir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        // リポジトリのルートにある docs フォルダを出力先にする
        string buildPath = Path.GetFullPath(Path.Combine(projectDir, "../docs"));

        // ディレクトリをクリーンアップ
        if (Directory.Exists(buildPath))
        {
            try
            {
                Directory.Delete(buildPath, true);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[WebGLBuilder] 既存のdocsフォルダ削除中に例外が発生しました（無視して続行します）: {ex.Message}");
            }
        }
        Directory.CreateDirectory(buildPath);

        // ビルドターゲットを WebGL に切り替える
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        // GitHub Pages向けに圧縮を無効化（解凍ヘッダー不整合によるWebGL起動エラーを防止）
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        BuildPlayerOptions options = new BuildPlayerOptions();
        // シーンリスト（GameScene のみ）
        options.scenes = new[] { "Assets/Scenes/GameScene.unity" };
        options.locationPathName = buildPath;
        options.target = BuildTarget.WebGL;
        options.options = BuildOptions.None;

        Debug.Log($"[WebGLBuilder] WebGLビルドを開始します。出力先: {buildPath}");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[WebGLBuilder] WebGLビルド成功！サイズ: {summary.totalSize} bytes");
        }
        else
        {
            Debug.LogError($"[WebGLBuilder] WebGLビルド失敗！エラー数: {summary.totalErrors}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
