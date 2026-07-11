using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SortGems.Core;

namespace SortGems.Editor
{
    public class StageArtAnalyzer
    {
        [MenuItem("Tools/SortGems/Analyze Stage Art")]
        public static void Analyze()
        {
            Debug.Log("[StageArtAnalyzer] ステージアートの解析を開始します...");

            // 1. StageData アセットをロード
            string[] guids = AssetDatabase.FindAssets("t:StageData", new[] { "Assets/ScriptableObjects/Stages" });
            List<StageData> stages = new List<StageData>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (stage != null)
                {
                    stages.Add(stage);
                }
            }

            // ステージ番号順（またはファイル名順）にソート
            stages = stages.OrderBy(s => s.stageNumber).ThenBy(s => s.name).ToList();

            if (stages.Count == 0)
            {
                Debug.LogWarning("[StageArtAnalyzer] 解析対象の StageData アセットが見つかりませんでした。");
                return;
            }

            // 2. レポートの出力を準備
            string projectDir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string reportPath = Path.GetFullPath(Path.Combine(projectDir, "../documents/stage-art-analysis.md"));
            string reportDir = Path.GetDirectoryName(reportPath);

            if (!Directory.Exists(reportDir))
            {
                Directory.CreateDirectory(reportDir);
            }

            using (StreamWriter writer = new StreamWriter(reportPath, false, System.Text.Encoding.UTF8))
            {
                // レポートヘッダー
                writer.WriteLine("# Stage Art Analysis Report");
                writer.WriteLine();
                writer.WriteLine($"**Generated At:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"**Total Stages Analyzed:** {stages.Count}");
                writer.WriteLine();
                writer.WriteLine("## Summary");
                writer.WriteLine();

                // サマリー用の集計
                int goodCount = 0;
                int reviewCount = 0;
                int poorCount = 0;

                List<string> poorStages = new List<string>();
                List<string> reviewStages = new List<string>();

                // 詳細結果を一時保存するバッファ
                List<string> detailLines = new List<string>();

                foreach (var stage in stages)
                {
                    int rows = stage.mainRows;
                    int cols = stage.mainCols;

                    // グリッド構築
                    GemColor[,] grid = new GemColor[rows, cols];
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            grid[r, c] = GemColor.None;
                        }
                    }

                    // goalLayout を配置
                    if (stage.goalLayout != null)
                    {
                        foreach (var cell in stage.goalLayout)
                        {
                            if (cell.row >= 0 && cell.row < rows && cell.col >= 0 && cell.col < cols)
                            {
                                grid[cell.row, cell.col] = cell.color;
                            }
                        }
                    }

                    // --- 指標の計算 ---

                    // 1. 充填マス数
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
                                if (!colorCounts.ContainsKey(color))
                                {
                                    colorCounts[color] = 0;
                                }
                                colorCounts[color]++;
                            }
                        }
                    }

                    double fillRatio = (double)filledCellCount / (rows * cols);
                    int usedColorCount = colorCounts.Count;

                    // 2. 境界枠（Bounding Box）と余白（Margins）
                    int minRow = rows, maxRow = -1, minCol = cols, maxCol = -1;
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            if (grid[r, c] != GemColor.None)
                            {
                                if (r < minRow) minRow = r;
                                if (r > maxRow) maxRow = r;
                                if (c < minCol) minCol = c;
                                if (c > maxCol) maxCol = c;
                            }
                        }
                    }

                    int boundsWidth = 0;
                    int boundsHeight = 0;
                    int marginTop = 0, marginBottom = 0, marginLeft = 0, marginRight = 0;

                    if (filledCellCount > 0)
                    {
                        boundsWidth = maxCol - minCol + 1;
                        boundsHeight = maxRow - minRow + 1;
                        marginTop = minRow;
                        marginBottom = rows - 1 - maxRow;
                        marginLeft = minCol;
                        marginRight = cols - 1 - maxCol;
                    }

                    // 3. 8方向連結コンポーネント数と孤立マスのカウント
                    int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
                    int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

                    int isolatedIslandsCount = 0;
                    Dictionary<GemColor, int> colorComponentCounts = new Dictionary<GemColor, int>();

                    // 孤立マス判定
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
                                if (!hasSameNeighbor)
                                {
                                    isolatedIslandsCount++;
                                }
                            }
                        }
                    }

                    // 連結コンポーネント数判定
                    bool[,] visited = new bool[rows, cols];
                    foreach (var color in colorCounts.Keys)
                    {
                        int components = 0;
                        // visited をリセット（この色専用で訪問管理するか、全体で管理するか。ここでは全体で管理）
                        for (int r = 0; r < rows; r++)
                        {
                            for (int c = 0; c < cols; c++)
                            {
                                if (grid[r, c] == color && !visited[r, c])
                                {
                                    components++;
                                    // BFS で 8方向 Flood Fill
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
                        colorComponentCounts[color] = components;
                    }

                    // 4. 对称性スコアの計算
                    // 左右対称
                    int matchH = 0;
                    int totalH = 0;
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < (cols + 1) / 2; c++)
                        {
                            int targetC = cols - 1 - c;
                            totalH++;
                            if (grid[r, c] == grid[r, targetC])
                            {
                                matchH++;
                            }
                        }
                    }
                    double symmetryH = totalH > 0 ? (double)matchH / totalH : 1.0;

                    // 上下対称
                    int matchV = 0;
                    int totalV = 0;
                    for (int c = 0; c < cols; c++)
                    {
                        for (int r = 0; r < (rows + 1) / 2; r++)
                        {
                            int targetR = rows - 1 - r;
                            totalV++;
                            if (grid[r, c] == grid[targetR, c])
                            {
                                matchV++;
                            }
                        }
                    }
                    double symmetryV = totalV > 0 ? (double)matchV / totalV : 1.0;

                    // 5. 判定 (Verdict) のヒューリスティクス評価
                    string verdict = "Good";
                    List<string> warnings = new List<string>();

                    // 警告：孤立マス
                    if (isolatedIslandsCount >= 3)
                    {
                        verdict = "Poor";
                        warnings.Add($"孤立マス（1セル）が {isolatedIslandsCount} 個存在します (推奨: 0)");
                    }
                    else if (isolatedIslandsCount > 0)
                    {
                        if (verdict != "Poor") verdict = "Review";
                        warnings.Add($"孤立マス（1セル）が {isolatedIslandsCount} 個存在します (推奨: 0)");
                    }

                    // 警告：最少セル数
                    int minCells = colorCounts.Count > 0 ? colorCounts.Values.Min() : 0;
                    if (colorCounts.Count > 0)
                    {
                        if (minCells == 1)
                        {
                            verdict = "Poor";
                            var colorName = colorCounts.FirstOrDefault(x => x.Value == 1).Key;
                            warnings.Add($"極端にマス数の少ない色 ({colorName}: 1マス) があります (推奨: 各色3マス以上)");
                        }
                        else if (minCells == 2)
                        {
                            if (verdict != "Poor") verdict = "Review";
                            var colorName = colorCounts.FirstOrDefault(x => x.Value == 2).Key;
                            warnings.Add($"マス数の少ない色 ({colorName}: 2マス) があります (推奨: 各色3マス以上)");
                        }
                    }

                    // 警告：充填率
                    if (fillRatio < 0.20 || fillRatio > 0.90)
                    {
                        verdict = "Poor";
                        warnings.Add($"充填率が適正範囲外です: {fillRatio:P1} (推奨: 35%〜80%, 限界限界: 20%〜90%)");
                    }
                    else if (fillRatio < 0.35 || fillRatio > 0.80)
                    {
                        if (verdict != "Poor") verdict = "Review";
                        warnings.Add($"充填率がやや偏っています: {fillRatio:P1} (推奨: 35%〜80%)");
                    }

                    // 警告：特定色の過剰占有
                    if (filledCellCount > 0)
                    {
                        foreach (var kvp in colorCounts)
                        {
                            double colorRatio = (double)kvp.Value / filledCellCount;
                            if (colorRatio > 0.70)
                            {
                                verdict = "Poor";
                                warnings.Add($"特定の色の占有率が極端に高いです: {kvp.Key} ({colorRatio:P1}) (推奨: 55%以下)");
                            }
                            else if (colorRatio > 0.55)
                            {
                                if (verdict != "Poor") verdict = "Review";
                                warnings.Add($"特定の色の占有率がやや高いです: {kvp.Key} ({colorRatio:P1}) (推奨: 55%以下)");
                            }
                        }
                    }

                    // 警告：連結コンポーネント数（同色の分裂）
                    foreach (var kvp in colorComponentCounts)
                    {
                        if (kvp.Value >= 3)
                        {
                            if (verdict != "Poor") verdict = "Review";
                            warnings.Add($"色 {kvp.Key} の連結コンポーネントが {kvp.Value} 個に分裂しています (推奨: 2以下)");
                        }
                    }

                    // 警告：余白ゼロ
                    if (filledCellCount > 0 && marginTop == 0 && marginBottom == 0 && marginLeft == 0 && marginRight == 0)
                    {
                        verdict = "Poor";
                        warnings.Add("アートが上下左右すべての外枠に接しています（余白がありません）");
                    }

                    // カウント更新
                    if (verdict == "Good") goodCount++;
                    else if (verdict == "Review")
                    {
                        reviewCount++;
                        reviewStages.Add($"Stage {stage.stageNumber:D3}: {stage.stageName}");
                    }
                    else if (verdict == "Poor")
                    {
                        poorCount++;
                        poorStages.Add($"Stage {stage.stageNumber:D3}: {stage.stageName}");
                    }

                    // 詳細行の作成
                    detailLines.Add($"### Stage {stage.stageNumber:D3}: {stage.stageName}");
                    detailLines.Add($"* **Asset Path:** `Assets/ScriptableObjects/Stages/{stage.name}.asset`");
                    detailLines.Add($"* **Grid Size:** {rows}x{cols}");
                    detailLines.Add($"* **Filled Cells:** {filledCellCount} / {rows * cols} ({fillRatio:P1})");
                    detailLines.Add($"* **Used Colors ({usedColorCount}):** " + string.Join(", ", colorCounts.Select(c => $"{c.Key} ({c.Value} cells, {colorComponentCounts[c.Key]} comps)")));
                    detailLines.Add($"* **Symmetry:** Horizontal: {symmetryH:P1}, Vertical: {symmetryV:P1}");
                    detailLines.Add($"* **Bounding Box:** {boundsWidth}x{boundsHeight} (Margin: T:{marginTop} B:{marginBottom} L:{marginLeft} R:{marginRight})");
                    detailLines.Add($"* **Isolated Cells:** {isolatedIslandsCount}");

                    string badge = verdict == "Good" ? "🟢 **Good**" : (verdict == "Review" ? "🟡 **Review**" : "🔴 **Poor**");
                    detailLines.Add($"* **Verdict:** {badge}");

                    if (warnings.Count > 0)
                    {
                        detailLines.Add("* **Warnings:**");
                        foreach (var warn in warnings)
                        {
                            detailLines.Add($"  - {warn}");
                        }
                    }
                    detailLines.Add("");
                }

                // サマリーの書き出し
                writer.WriteLine("| Status | Count | Ratio |");
                writer.WriteLine("|---|---|---|");
                writer.WriteLine($"| 🟢 Good | {goodCount} | {(double)goodCount / stages.Count:P1} |");
                writer.WriteLine($"| 🟡 Review | {reviewCount} | {(double)reviewCount / stages.Count:P1} |");
                writer.WriteLine($"| 🔴 Poor | {poorCount} | {(double)poorCount / stages.Count:P1} |");
                writer.WriteLine();

                if (poorStages.Count > 0)
                {
                    writer.WriteLine("### 🔴 Poor Stages");
                    foreach (var s in poorStages)
                    {
                        writer.WriteLine($"- {s}");
                    }
                    writer.WriteLine();
                }

                if (reviewStages.Count > 0)
                {
                    writer.WriteLine("### 🟡 Review Stages");
                    foreach (var s in reviewStages)
                    {
                        writer.WriteLine($"- {s}");
                    }
                    writer.WriteLine();
                }

                writer.WriteLine("---");
                writer.WriteLine();
                writer.WriteLine("## Detailed Stage Report");
                writer.WriteLine();

                foreach (var line in detailLines)
                {
                    writer.WriteLine(line);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[StageArtAnalyzer] 解析が完了しました。レポートを出力しました: {reportPath}");
        }
    }
}
