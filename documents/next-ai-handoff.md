# Next AI Handoff

## User Intent

The user wants to improve SortGems stage pixel art. Current stage art is not attractive enough: shapes are distorted, color counts feel wrong, and per-color cell counts may hurt both art and puzzle quality.

They approved moving forward with a diagnostic-first approach.

Important extra note from user:

The puzzle grid width and height may be changed. Do not treat 16x16 as fixed.

Odd grid widths are allowed and encouraged for horizontally symmetric subjects. Accept 15, 17, 19, and 21 when a true center column improves the art.

## Recent Work Done

- Confirmed Unity MCP can connect, though larger or parallel requests often time out.
- Confirmed current `GameScene` uses `[UIToolkit]` with `UIDocument` + `ScreenManager`.
- Confirmed `[Canvas]` remains needed for the uGUI puzzle grid.
- Marked `GameBootstrap` and `GameUI` as legacy with `Obsolete` attributes.
- Added `documents/pixel-art-stage-quality-plan.md`.

## Recommended Next Step

Implement a Unity Editor analyzer:

`SortGemsUnity/Assets/Editor/StageArtAnalyzer.cs`

Menu item:

`Tools > SortGems > Analyze Stage Art`

Output:

`documents/stage-art-analysis.md`

The analyzer should load all `StageData` assets and compute:

- Grid size
- Filled cell count and ratio
- Used color count
- Per-color cell counts
- Smallest/largest color count
- Connected components per color using 8-direction adjacency
- Single-cell islands
- Empty margins
- Horizontal and vertical symmetry score
- Verdict and warning list

## Suggested Verdict Rules

Good:

- No single-cell islands
- No color below 3 cells
- Fill ratio between 35% and 80%
- No color dominates more than 55%

Review:

- Minor isolated accents
- Very high or low fill ratio
- One very small color
- Many components in one color

Poor:

- Multiple one-cell islands
- Several tiny colors
- Shape touches too many borders accidentally
- Extremely dominant color

## Implementation Notes

Use `AssetDatabase.FindAssets("t:StageData", new[] { "Assets/ScriptableObjects/Stages" })`.

Build a `GemColor[,]` from `stage.goalLayout`.

Use `stage.mainRows` and `stage.mainCols`; do not infer a fixed grid size.

When reporting symmetry, handle odd widths naturally by treating the center column as self-mirrored.

For connected components, use 8-neighbor flood fill.

Write the Markdown report under the repository `documents` folder. From Unity, use:

`Path.GetFullPath(Path.Combine(Application.dataPath, "../../documents/stage-art-analysis.md"))`

After writing, call `AssetDatabase.Refresh()` if needed.

## Keep In Mind

Do not remove `[Canvas]`; ScreenManager still uses it for puzzle rendering.

Avoid regenerating `GameScene` unless explicitly requested. `CreateGameScene.cs` is large and may overwrite scene assets.
