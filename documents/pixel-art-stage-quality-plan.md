# Pixel Art Stage Quality Plan

This document is a handoff note for improving SortGems stage art and puzzle quality.

## Current Goal

SortGems stages are currently generated from hardcoded pixel-art strings in `Assets/Editor/CreateGameScene.cs` and saved as `StageData` assets. The existing art often has weak silhouettes, awkward proportions, noisy colors, and uneven color-cell counts.

The next work should create a repeatable pipeline for evaluating and improving stage pixel art, rather than editing stages by feel.

## Important Product Constraint

Pixel art is not just decoration. It is the puzzle target layout.

Good stage art must satisfy both:

- Art quality: recognizable, balanced, attractive at mobile size.
- Puzzle quality: fair color counts, good region structure, satisfying group movement.

The puzzle grid width and height may be changed per stage. Do not assume all stages must remain 16x16. Reasonable target sizes:

- Easy: 12x12, 14x14, 15x15, or 16x16
- Normal: 15x15, 16x16, 17x17, or 18x18
- Hard: 19x19, 20x20, 21x21, or 24x24

Odd widths are especially useful for horizontally symmetric subjects because they provide a true center column. Use 15, 17, 19, or 21 when the subject benefits from a central spine, face line, body axis, mast, stem, or similar feature.

Palette dimensions may also need tuning to match color counts and total gem counts.

## Recommended Approach

Build a Unity Editor diagnostic tool first.

Suggested menu:

`Tools > SortGems > Analyze Stage Art`

The tool should scan `Assets/ScriptableObjects/Stages/*.asset`, inspect each `StageData.goalLayout`, and output a report.

Minimum report fields:

- Stage number and name
- Grid size
- Filled cell count
- Fill ratio
- Used color count
- Cell count per color
- Smallest/largest color count
- Connected component count per color, using 8-direction adjacency
- Number of single-cell islands
- Bounding-box width/height of filled art
- Empty border margins
- Horizontal symmetry score
- Vertical symmetry score
- Overall verdict: Good / Review / Poor

Nice-to-have:

- Render a preview contact sheet PNG for all stages.
- Show flagged cells in an editor window.
- Export CSV or Markdown report.

## Suggested Quality Heuristics

These should be tunable, not hardcoded forever.

Art heuristics:

- Filled art should occupy roughly 35% to 80% of the grid.
- Empty border should usually be at least 1 cell on each side, unless intentionally full-bleed.
- Simple objects should prefer high horizontal symmetry or deliberate asymmetry.
- Avoid one-cell details unless they are high-value accents, such as eyes.
- The silhouette should remain readable after converting all colors to one filled shape.

Puzzle heuristics:

- Easy stages should use 1 to 3 colors.
- Normal stages should use 3 to 5 colors.
- Hard stages should use 5 to 7 colors.
- Avoid colors with fewer than 3 cells, except deliberate accent colors.
- Warn when one color has more than 55% of all filled cells.
- Warn when a color has many disconnected components.
- Warn when total gem count is too high for the target difficulty/time.

## Grid Size Guidance

Because grid size is flexible, choose dimensions by subject:

- Icons and simple shapes: 12x12 or 14x14
- Symmetric icons with a center axis: 15x15 or 17x17
- Cute objects and animals: 16x16, 17x17, or 18x18
- Detailed symmetric objects: 19x19 or 21x21
- Detailed objects needing more room: 20x20 or 24x24

Avoid forcing a detailed subject into 16x16 if it creates noise. Prefer a larger grid or a simpler subject.

## Stage Creation Pipeline

1. Pick a subject with a strong silhouette.
2. Draw or generate a monochrome silhouette first.
3. Validate silhouette readability at the actual in-game preview size.
4. Add 2 to 6 colors, depending on difficulty.
5. Run the diagnostic tool.
6. Fix flagged issues:
   - Merge tiny accent colors.
   - Remove or expand isolated cells.
   - Smooth asymmetric edges.
   - Adjust grid size if the subject is cramped.
7. Generate `StageData`.
8. Test the stage in Play Mode.

## Possible Auto-Fix Tools

After diagnostics work, add optional cleanup actions:

- Remove isolated single-cell pixels.
- Mirror left half to right half for symmetric subjects.
- Normalize color counts by replacing tiny accents with neighboring colors.
- Crop filled bounds and re-center art.
- Expand canvas from 16x16 to 17x17, 18x18, 19x19, 20x20, or 21x21 while preserving art position.

These should be previewed before applying.

## Relevant Files

- `SortGemsUnity/Assets/Editor/CreateGameScene.cs`
- `SortGemsUnity/Assets/Scripts/Core/StageData.cs`
- `SortGemsUnity/Assets/Scripts/Core/GemColor.cs`
- `SortGemsUnity/Assets/Scripts/Core/GridManager.cs`
- `SortGemsUnity/Assets/Scripts/UI/ScreenManager.cs`
- `SortGemsUnity/Assets/ScriptableObjects/Stages/`

## Current UI Direction

The current scene flow should be `ScreenManager` on `[UIToolkit]`.

`GameBootstrap` and `GameUI` are legacy uGUI screen-flow classes. They were marked obsolete and should not be used for new flow work.

The uGUI `[Canvas]` is still used for puzzle grid rendering. Do not disable it globally.

## First Implementation Task

Create `Assets/Editor/StageArtAnalyzer.cs`.

Start with a console/Markdown report before building a visual editor window. Keep it simple:

- Load all `StageData` assets.
- Compute metrics.
- Write `documents/stage-art-analysis.md` or log a compact report.
- Flag obviously weak stages.

Once this report exists, use it to decide which stages need redrawing or grid resizing.
