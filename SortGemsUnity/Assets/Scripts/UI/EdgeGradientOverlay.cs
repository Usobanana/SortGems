using UnityEngine;
using UnityEngine.UIElements;

namespace SortGems.UI
{
    public enum ScreenEdge
    {
        Top,
        Bottom
    }

    /// <summary>
    /// 画面端(セーフエリア基準)を単色で塗りつぶし、内側へ向かって透過させるデコレーション用オーバーレイ。
    /// </summary>
    [UxmlElement]
    public partial class EdgeGradientOverlay : VisualElement
    {
        // Horizontal resolution of the arch curve and vertical resolution of the fade easing.
        private const int ArchSegments = 24;
        private const int FadeSteps = 6;
        private const int RowsPerColumn = FadeSteps + 2;

        [UxmlAttribute] public ScreenEdge Edge { get; set; } = ScreenEdge.Top;
        [UxmlAttribute] public float SolidSize { get; set; } = 80f;
        [UxmlAttribute] public float FadeSize { get; set; } = 80f;
        [UxmlAttribute] public float ArchAmount { get; set; } = 0f;
        [UxmlAttribute] public Color Tint { get; set; } = Color.black;

        public EdgeGradientOverlay()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.right = 0;

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<AttachToPanelEvent>(_ => ApplyLayout());
            RegisterCallback<GeometryChangedEvent>(_ => ApplyLayout());
        }

        private void ApplyLayout()
        {
            style.height = SolidSize + FadeSize + ArchAmount;
            if (Edge == ScreenEdge.Top)
            {
                style.top = GetSafeAreaTopInset();
                style.bottom = StyleKeyword.Auto;
            }
            else
            {
                style.bottom = GetSafeAreaBottomInset();
                style.top = StyleKeyword.Auto;
            }
        }

        // Smoothstep: eases the alpha ramp so the fade reads as gradual rather than linear.
        private static float Ease(float t) => t * t * (3f - 2f * t);

        // 0 at the horizontal center, ArchAmount at the left/right edges (elliptical arc).
        private float ArchOffset(float nx)
        {
            if (ArchAmount <= 0f) return 0f;
            float clamped = Mathf.Clamp(nx, -1f, 1f);
            return ArchAmount * (1f - Mathf.Sqrt(Mathf.Max(0f, 1f - clamped * clamped)));
        }

        private static float GetSafeAreaTopInset()
        {
            var safeArea = Screen.safeArea;
            return Screen.height - (safeArea.y + safeArea.height);
        }

        private static float GetSafeAreaBottomInset()
        {
            return Screen.safeArea.y;
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float width = contentRect.width;
            float height = contentRect.height;
            if (width <= 0f || height <= 0f) return;

            Color32 opaque = Tint;
            Color32 clear = new Color32(opaque.r, opaque.g, opaque.b, 0);

            int columnCount = ArchSegments + 1;
            var vertices = new Vertex[columnCount * RowsPerColumn];

            for (int col = 0; col < columnCount; col++)
            {
                float x = width * col / ArchSegments;
                float nx = (x / width) * 2f - 1f;
                float arch = ArchOffset(nx);

                for (int row = 0; row < RowsPerColumn; row++)
                {
                    float y;
                    Color32 tint;

                    if (Edge == ScreenEdge.Top)
                    {
                        if (row == 0)
                        {
                            y = 0f;
                            tint = opaque;
                        }
                        else
                        {
                            float t = (row - 1) / (float)FadeSteps;
                            y = SolidSize + arch + t * FadeSize;
                            tint = Color32.Lerp(opaque, clear, Ease(t));
                        }
                    }
                    else
                    {
                        if (row == RowsPerColumn - 1)
                        {
                            y = height;
                            tint = opaque;
                        }
                        else
                        {
                            float t = row / (float)FadeSteps;
                            float fadeStart = height - SolidSize - arch - FadeSize;
                            y = fadeStart + t * FadeSize;
                            tint = Color32.Lerp(clear, opaque, Ease(t));
                        }
                    }

                    vertices[col * RowsPerColumn + row] = new Vertex
                    {
                        position = new Vector3(x, y, Vertex.nearZ),
                        tint = tint
                    };
                }
            }

            var indices = new ushort[ArchSegments * (RowsPerColumn - 1) * 6];
            int i = 0;
            for (int col = 0; col < ArchSegments; col++)
            {
                for (int row = 0; row < RowsPerColumn - 1; row++)
                {
                    ushort a = (ushort)(col * RowsPerColumn + row);
                    ushort b = (ushort)((col + 1) * RowsPerColumn + row);
                    ushort c = (ushort)(col * RowsPerColumn + row + 1);
                    ushort d = (ushort)((col + 1) * RowsPerColumn + row + 1);

                    indices[i++] = a; indices[i++] = b; indices[i++] = c;
                    indices[i++] = c; indices[i++] = b; indices[i++] = d;
                }
            }

            var mwd = mgc.Allocate(vertices.Length, indices.Length);
            mwd.SetAllVertices(vertices);
            mwd.SetAllIndices(indices);
        }
    }
}
