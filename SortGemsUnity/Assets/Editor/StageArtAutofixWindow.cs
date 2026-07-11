using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SortGems.Core;

namespace SortGems.Editor
{
    public class StageArtAutofixWindow : EditorWindow
    {
        private StageData targetStage;
        private bool applyToAll = false;

        private bool removeIsolated = true;
        private bool mirrorLeftToRight = false;
        private bool normalizeColors = true;
        private bool cropAndCenter = false;

        private Vector2 scrollPos;
        private string previewText = "";
        private List<AutofixResult> previewResults = new List<AutofixResult>();

        private class AutofixResult
        {
            public string stageName;
            public int stageNumber;
            public string beforeVerdict;
            public List<string> beforeWarnings = new List<string>();
            public string afterVerdict;
            public List<string> afterWarnings = new List<string>();
            public bool isChanged;
            public StageData stageAsset;
            public List<StageData.CellColorDef> updatedGoal;
            public List<StageData.CellColorDef> updatedInitial;
        }

        [MenuItem("Tools/SortGems/Stage Art Autofixer")]
        public static void ShowWindow()
        {
            GetWindow<StageArtAutofixWindow>("Stage Art Autofixer");
        }

        [MenuItem("Tools/SortGems/Autofix All Poor and Review Stages")]
        public static void AutofixAllPoorAndReview()
        {
            Debug.Log("[StageArtAutofixer] 一括自動修正を開始します（対象: Poor / Review の全ステージ）...");

            string[] guids = AssetDatabase.FindAssets("t:StageData", new[] { "Assets/ScriptableObjects/Stages" });
            int fixedCount = 0;

            // 一時的なウィンドウ/インスタンスのオプションをシミュレート
            StageArtAutofixWindow tempInstance = CreateInstance<StageArtAutofixWindow>();
            tempInstance.removeIsolated = true;
            tempInstance.normalizeColors = true;
            tempInstance.cropAndCenter = true; // センタリングも適用して余白ゼロのPoorを解消
            tempInstance.mirrorLeftToRight = false; // デザインが大きく崩れるのを防ぐため、ミラーはデフォルトOFF

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (stage != null)
                {
                    // 修正前の評価を行う
                    string beforeVerdict;
                    List<string> beforeWarnings;
                    
                    // grid展開
                    int rows = stage.mainRows;
                    int cols = stage.mainCols;
                    GemColor[,] originalGrid = new GemColor[rows, cols];
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                            originalGrid[r, c] = GemColor.None;

                    foreach (var cell in stage.goalLayout)
                    {
                        if (cell.row >= 0 && cell.row < rows && cell.col >= 0 && cell.col < cols)
                            originalGrid[cell.row, cell.col] = cell.color;
                    }

                    tempInstance.EvaluateGridQuality(originalGrid, rows, cols, out beforeVerdict, out beforeWarnings);

                    // Poor または Review の場合のみ自動修正を適用
                    if (beforeVerdict == "Poor" || beforeVerdict == "Review")
                    {
                        var result = tempInstance.ProcessStage(stage, false);
                        if (result.isChanged)
                        {
                            Undo.RecordObject(stage, "Batch Apply Stage Art Autofix");
                            stage.goalLayout = result.updatedGoal;
                            stage.initialMainCells = result.updatedInitial;
                            EditorUtility.SetDirty(stage);
                            fixedCount++;
                            Debug.Log($"[StageArtAutofixer] Stage {stage.stageNumber:D3} ({stage.stageName}) を自動修正しました。 ({beforeVerdict} -> {result.afterVerdict})");
                        }
                    }
                }
            }

            if (fixedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[StageArtAutofixer] 一括自動修正が完了しました。修正件数: {fixedCount} 件");
            }
            else
            {
                Debug.Log("[StageArtAutofixer] 自動修正の対象となる Poor / Review ステージはありませんでした。");
            }

            DestroyImmediate(tempInstance);
        }

        private void OnGUI()
        {
            GUILayout.Label("Stage Art Autofixer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            applyToAll = EditorGUILayout.Toggle("Apply to All Stages", applyToAll);

            if (!applyToAll)
            {
                targetStage = (StageData)EditorGUILayout.ObjectField("Target Stage", targetStage, typeof(StageData), false);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Autofix Options", EditorStyles.boldLabel);

            removeIsolated = EditorGUILayout.Toggle("Remove Isolated Cells", removeIsolated);
            mirrorLeftToRight = EditorGUILayout.Toggle("Mirror Left to Right", mirrorLeftToRight);
            normalizeColors = EditorGUILayout.Toggle("Normalize Tiny Colors (< 3 cells)", normalizeColors);
            cropAndCenter = EditorGUILayout.Toggle("Crop & Center Art", cropAndCenter);

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Changes", GUILayout.Height(30)))
            {
                GeneratePreview();
            }
            
            GUI.enabled = previewResults.Count > 0 && previewResults.Any(r => r.isChanged);
            if (GUILayout.Button("Apply Autofix", GUILayout.Height(30)))
            {
                ApplyAutofix();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            GUILayout.Label("Preview:", EditorStyles.boldLabel);

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
            if (!string.IsNullOrEmpty(previewText))
            {
                EditorGUILayout.TextArea(previewText, GUILayout.ExpandHeight(true));
            }
            else
            {
                GUILayout.Label("No preview generated. Click 'Preview Changes'.");
            }
            GUILayout.EndScrollView();
        }

        private void GeneratePreview()
        {
            previewResults.Clear();
            previewText = "";

            List<StageData> stages = GetStagesToProcess();
            if (stages.Count == 0)
            {
                previewText = "Error: No stage selected or found.";
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"--- Autofix Preview (Target Count: {stages.Count}) ---");
            sb.AppendLine();

            int changedCount = 0;

            foreach (var stage in stages)
            {
                var result = ProcessStage(stage, false);
                previewResults.Add(result);

                if (result.isChanged)
                {
                    changedCount++;
                    sb.AppendLine($"Stage {result.stageNumber:D3}: {result.stageName}");
                    sb.AppendLine($"  [Before] Verdict: {result.beforeVerdict}");
                    if (result.beforeWarnings.Count > 0)
                    {
                        foreach (var w in result.beforeWarnings) sb.AppendLine($"    - {w}");
                    }
                    else
                    {
                        sb.AppendLine("    (No warnings)");
                    }

                    sb.AppendLine($"  [After] Verdict: {result.afterVerdict}");
                    if (result.afterWarnings.Count > 0)
                    {
                        foreach (var w in result.afterWarnings) sb.AppendLine($"    - {w}");
                    }
                    else
                    {
                        sb.AppendLine("    (No warnings)");
                    }
                    sb.AppendLine();
                }
            }

            sb.AppendLine($"Summary: {changedCount} / {stages.Count} stages will be modified.");
            previewText = sb.ToString();
        }

        private void ApplyAutofix()
        {
            if (previewResults.Count == 0) return;

            int successCount = 0;
            foreach (var result in previewResults)
            {
                if (result.isChanged && result.stageAsset != null)
                {
                    Undo.RecordObject(result.stageAsset, "Apply Stage Art Autofix");
                    
                    // アセットのgoalLayout と initialMainCells を更新
                    result.stageAsset.goalLayout = result.updatedGoal;
                    result.stageAsset.initialMainCells = result.updatedInitial;
                    
                    EditorUtility.SetDirty(result.stageAsset);
                    successCount++;
                }
            }

            if (successCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[StageArtAutofixer] {successCount} 件のステージデータを修正・保存しました。");
                EditorUtility.DisplayDialog("Autofix Success", $"{successCount} stages successfully autofixed and saved.", "OK");
            }
            
            // プレビューをクリア
            previewResults.Clear();
            previewText = "Autofix applied successfully. Run preview again to check results.";
        }

        private List<StageData> GetStagesToProcess()
        {
            List<StageData> list = new List<StageData>();
            if (applyToAll)
            {
                string[] guids = AssetDatabase.FindAssets("t:StageData", new[] { "Assets/ScriptableObjects/Stages" });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
                    if (stage != null) list.Add(stage);
                }
                list = list.OrderBy(s => s.stageNumber).ThenBy(s => s.name).ToList();
            }
            else
            {
                if (targetStage != null)
                {
                    list.Add(targetStage);
                }
            }
            return list;
        }

        private AutofixResult ProcessStage(StageData stage, bool apply)
        {
            AutofixResult res = new AutofixResult();
            res.stageAsset = stage;
            res.stageName = stage.stageName;
            res.stageNumber = stage.stageNumber;

            int rows = stage.mainRows;
            int cols = stage.mainCols;

            // 1. グリッドデータの展開
            GemColor[,] originalGrid = new GemColor[rows, cols];
            GemColor[,] workingGrid = new GemColor[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    originalGrid[r, c] = GemColor.None;
                    workingGrid[r, c] = GemColor.None;
                }
            }

            foreach (var cell in stage.goalLayout)
            {
                if (cell.row >= 0 && cell.row < rows && cell.col >= 0 && cell.col < cols)
                {
                    originalGrid[cell.row, cell.col] = cell.color;
                    workingGrid[cell.row, cell.col] = cell.color;
                }
            }

            // Before の評価
            EvaluateGridQuality(originalGrid, rows, cols, out res.beforeVerdict, out res.beforeWarnings);

            // 2. 自動修正オプションの適用
            bool isChanged = false;

            // A. 左右対称化 (Mirror)
            if (mirrorLeftToRight)
            {
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols / 2; c++)
                    {
                        int targetC = cols - 1 - c;
                        if (workingGrid[r, targetC] != workingGrid[r, c])
                        {
                            workingGrid[r, targetC] = workingGrid[r, c];
                            isChanged = true;
                        }
                    }
                }
            }

            // B. 孤立マスの除去
            if (removeIsolated)
            {
                int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
                int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

                List<(int r, int c, GemColor newColor)> changes = new List<(int, int, GemColor)>();

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        GemColor color = workingGrid[r, c];
                        if (color != GemColor.None)
                        {
                            bool hasSameNeighbor = false;
                            for (int i = 0; i < 8; i++)
                            {
                                int nr = r + dr[i];
                                int nc = c + dc[i];
                                if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
                                {
                                    if (workingGrid[nr, nc] == color)
                                    {
                                        hasSameNeighbor = true;
                                        break;
                                    }
                                }
                            }

                            if (!hasSameNeighbor)
                            {
                                // 孤立している
                                // 周囲の最多色を見つける
                                Dictionary<GemColor, int> neighborColors = new Dictionary<GemColor, int>();
                                for (int i = 0; i < 8; i++)
                                {
                                    int nr = r + dr[i];
                                    int nc = c + dc[i];
                                    if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
                                    {
                                        GemColor ncColor = workingGrid[nr, nc];
                                        if (ncColor != GemColor.None)
                                        {
                                            if (!neighborColors.ContainsKey(ncColor)) neighborColors[ncColor] = 0;
                                            neighborColors[ncColor]++;
                                        }
                                    }
                                }

                                GemColor bestColor = GemColor.None;
                                if (neighborColors.Count > 0)
                                {
                                    bestColor = neighborColors.OrderByDescending(x => x.Value).First().Key;
                                }

                                changes.Add((r, c, bestColor));
                            }
                        }
                    }
                }

                if (changes.Count > 0)
                {
                    isChanged = true;
                    foreach (var change in changes)
                    {
                        workingGrid[change.r, change.c] = change.newColor;
                    }
                }
            }

            // C. 極小色 (3マス未満) の自動統合
            if (normalizeColors)
            {
                int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
                int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

                // 色ごとのピクセル数をカウント
                Dictionary<GemColor, int> colorCounts = new Dictionary<GemColor, int>();
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        GemColor color = workingGrid[r, c];
                        if (color != GemColor.None)
                        {
                            if (!colorCounts.ContainsKey(color)) colorCounts[color] = 0;
                            colorCounts[color]++;
                        }
                    }
                }

                List<GemColor> tinyColors = colorCounts.Where(x => x.Value < 3).Select(x => x.Key).ToList();

                if (tinyColors.Count > 0)
                {
                    List<(int r, int c, GemColor newColor)> changes = new List<(int, int, GemColor)>();

                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            GemColor color = workingGrid[r, c];
                            if (tinyColors.Contains(color))
                            {
                                // このセルを最も近い、Noneおよび極小色以外の隣接する最多色に書き換える
                                Dictionary<GemColor, int> neighborColors = new Dictionary<GemColor, int>();
                                for (int i = 0; i < 8; i++)
                                {
                                    int nr = r + dr[i];
                                    int nc = c + dc[i];
                                    if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
                                    {
                                        GemColor ncColor = workingGrid[nr, nc];
                                        if (ncColor != GemColor.None && !tinyColors.Contains(ncColor))
                                        {
                                            if (!neighborColors.ContainsKey(ncColor)) neighborColors[ncColor] = 0;
                                            neighborColors[ncColor]++;
                                        }
                                    }
                                }

                                GemColor bestColor = GemColor.None;
                                if (neighborColors.Count > 0)
                                {
                                    bestColor = neighborColors.OrderByDescending(x => x.Value).First().Key;
                                }
                                else
                                {
                                    // 周囲にまともな色がなければ、単にNoneにする
                                    bestColor = GemColor.None;
                                }

                                changes.Add((r, c, bestColor));
                            }
                        }
                    }

                    if (changes.Count > 0)
                    {
                        isChanged = true;
                        foreach (var change in changes)
                        {
                            workingGrid[change.r, change.c] = change.newColor;
                        }
                    }
                }
            }

            // D. センタリング (Crop and Center)
            if (cropAndCenter)
            {
                int minRow = rows, maxRow = -1, minCol = cols, maxCol = -1;
                int filledCount = 0;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (workingGrid[r, c] != GemColor.None)
                        {
                            filledCount++;
                            if (r < minRow) minRow = r;
                            if (r > maxRow) maxRow = r;
                            if (c < minCol) minCol = c;
                            if (c > maxCol) maxCol = c;
                        }
                    }
                }

                if (filledCount > 0)
                {
                    int boundsW = maxCol - minCol + 1;
                    int boundsH = maxRow - minRow + 1;

                    int newMinRow = (rows - boundsH) / 2;
                    int newMinCol = (cols - boundsW) / 2;

                    int offsetRow = newMinRow - minRow;
                    int offsetCol = newMinCol - minCol;

                    if (offsetRow != 0 || offsetCol != 0)
                    {
                        isChanged = true;
                        GemColor[,] centeredGrid = new GemColor[rows, cols];
                        for (int r = 0; r < rows; r++)
                        {
                            for (int c = 0; c < cols; c++)
                            {
                                centeredGrid[r, c] = GemColor.None;
                            }
                        }

                        for (int r = minRow; r <= maxRow; r++)
                        {
                            for (int c = minCol; c <= maxCol; c++)
                            {
                                int nr = r + offsetRow;
                                int nc = c + offsetCol;
                                if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
                                {
                                    centeredGrid[nr, nc] = workingGrid[r, c];
                                }
                            }
                        }
                        workingGrid = centeredGrid;
                    }
                }
            }

            res.isChanged = isChanged;

            // 新しい goalLayout の構築
            List<StageData.CellColorDef> newGoal = new List<StageData.CellColorDef>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (workingGrid[r, c] != GemColor.None)
                    {
                        newGoal.Add(new StageData.CellColorDef
                        {
                            row = r,
                            col = c,
                            color = workingGrid[r, c]
                        });
                    }
                }
            }
            res.updatedGoal = newGoal;

            // 新しい initialMainCells の構築 (色のランダムシャッフル再配置)
            List<StageData.CellColorDef> newInitial = new List<StageData.CellColorDef>();
            if (newGoal.Count > 0)
            {
                List<Vector2Int> positions = newGoal.Select(x => new Vector2Int(x.row, x.col)).ToList();
                List<GemColor> colors = newGoal.Select(x => x.color).ToList();

                // 撹乱順列（すべての要素が元の目標色と異なる状態）を作るシャッフル
                System.Random rand = new System.Random(stage.stageNumber); // シード値を固定して毎度同じシャッフルにする
                List<GemColor> bestColors = new List<GemColor>(colors);
                int minMatches = colors.Count;

                for (int attempt = 0; attempt < 1000; attempt++)
                {
                    // Fisher-Yates シャッフル
                    for (int i = colors.Count - 1; i > 0; i--)
                    {
                        int j = rand.Next(i + 1);
                        GemColor temp = colors[i];
                        colors[i] = colors[j];
                        colors[j] = temp;
                    }

                    // 一致数をカウント
                    int matches = 0;
                    for (int i = 0; i < newGoal.Count; i++)
                    {
                        if (colors[i] == newGoal[i].color)
                        {
                            matches++;
                        }
                    }

                    // 完全順列（一致数0）が見つかったら即座に終了
                    if (matches == 0)
                    {
                        bestColors = new List<GemColor>(colors);
                        break;
                    }

                    if (matches < minMatches)
                    {
                        minMatches = matches;
                        bestColors = new List<GemColor>(colors);
                    }
                }

                for (int i = 0; i < positions.Count; i++)
                {
                    newInitial.Add(new StageData.CellColorDef
                    {
                        row = positions[i].x,
                        col = positions[i].y,
                        color = bestColors[i]
                    });
                }
            }
            res.updatedInitial = newInitial;

            // After の評価
            EvaluateGridQuality(workingGrid, rows, cols, out res.afterVerdict, out res.afterWarnings);

            return res;
        }

        private void EvaluateGridQuality(GemColor[,] grid, int rows, int cols, out string verdict, out List<string> warnings)
        {
            verdict = "Good";
            warnings = new List<string>();

            int filledCellCount = 0;
            Dictionary<GemColor, int> colorCounts = new Dictionary<GemColor, int>();

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    GemColor color = grid[r, c];
                    if (color != GemColor.None)
                    {
                        filledCellCount++;
                        if (!colorCounts.ContainsKey(color)) colorCounts[color] = 0;
                        colorCounts[color]++;
                    }
                }
            }

            if (filledCellCount == 0)
            {
                verdict = "Poor";
                warnings.Add("Empty grid");
                return;
            }

            double fillRatio = (double)filledCellCount / (rows * cols);

            // 1. 孤立マス
            int isolatedCount = 0;
            int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    GemColor color = grid[r, c];
                    if (color != GemColor.None)
                    {
                        bool hasSameNeighbor = false;
                        for (int i = 0; i < 8; i++)
                        {
                            int nr = r + dr[i];
                            int nc = c + dc[i];
                            if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
                            {
                                if (grid[nr, nc] == color)
                                {
                                    hasSameNeighbor = true;
                                    break;
                                }
                            }
                        }
                        if (!hasSameNeighbor) isolatedCount++;
                    }
                }
            }

            if (isolatedCount >= 3)
            {
                verdict = "Poor";
                warnings.Add($"Isolated cells: {isolatedCount} (threshold: < 3)");
            }
            else if (isolatedCount > 0)
            {
                if (verdict != "Poor") verdict = "Review";
                warnings.Add($"Isolated cells: {isolatedCount} (threshold: 0)");
            }

            // 2. 最少マス
            int minCells = colorCounts.Values.Min();
            if (minCells == 1)
            {
                verdict = "Poor";
                warnings.Add("Has color with 1 cell");
            }
            else if (minCells == 2)
            {
                if (verdict != "Poor") verdict = "Review";
                warnings.Add("Has color with 2 cells");
            }

            // 3. 充填率
            if (fillRatio < 0.20 || fillRatio > 0.90)
            {
                verdict = "Poor";
                warnings.Add($"Fill ratio out of bounds: {fillRatio:P1}");
            }
            else if (fillRatio < 0.35 || fillRatio > 0.80)
            {
                if (verdict != "Poor") verdict = "Review";
                warnings.Add($"Fill ratio sub-optimal: {fillRatio:P1}");
            }

            // 4. 特定色の占有率
            foreach (var kvp in colorCounts)
            {
                double colorRatio = (double)kvp.Value / filledCellCount;
                if (colorRatio > 0.70)
                {
                    verdict = "Poor";
                    warnings.Add($"Color {kvp.Key} dominates {colorRatio:P1}");
                }
                else if (colorRatio > 0.55)
                {
                    if (verdict != "Poor") verdict = "Review";
                    warnings.Add($"Color {kvp.Key} dominates {colorRatio:P1}");
                }
            }

            // 5. 連結コンポーネント
            bool[,] visited = new bool[rows, cols];
            foreach (var color in colorCounts.Keys)
            {
                int components = 0;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (grid[r, c] == color && !visited[r, c])
                        {
                            components++;
                            Queue<(int, int)> queue = new Queue<(int, int)>();
                            queue.Enqueue((r, c));
                            visited[r, c] = true;

                            while (queue.Count > 0)
                            {
                                var (currR, currC) = queue.Dequeue();
                                for (int i = 0; i < 8; i++)
                                {
                                    int nr = currR + dr[i];
                                    int nc = currC + dc[i];
                                    if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
                                    {
                                        if (grid[nr, nc] == color && !visited[nr, nc])
                                        {
                                            visited[nr, nc] = true;
                                            queue.Enqueue((nr, nc));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (components >= 3)
                {
                    if (verdict != "Poor") verdict = "Review";
                    warnings.Add($"Color {color} has {components} disconnected components");
                }
            }
        }
    }
}
