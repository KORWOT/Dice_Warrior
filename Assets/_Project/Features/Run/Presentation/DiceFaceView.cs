using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    // A resolution-independent die face. It displays values, never generates game results.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DiceFaceView : MaskableGraphic
    {
        public Color faceColor = new Color(.08f, .12f, .18f);
        public Color edgeColor = new Color(.65f, .52f, .30f);
        public Color pipColor = new Color(.96f, .91f, .79f);
        public int Value { get; private set; }

        public void Render(int value)
        {
            Value = Mathf.Clamp(value, 0, 6);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            var size = Mathf.Min(rect.width, rect.height);
            RoundedRect(mesh, rect.center, new Vector2(size, size) * .48f, size * .09f, edgeColor);
            RoundedRect(mesh, rect.center, new Vector2(size, size) * .455f, size * .075f, faceColor);
            var spacing = size * .235f;
            var radius = size * .052f;
            if (Value == 0)
            {
                Circle(mesh, rect.center, radius, new Color(pipColor.r, pipColor.g, pipColor.b, .3f));
                return;
            }
            if ((Value & 1) == 1) Circle(mesh, rect.center, radius, pipColor);
            if (Value >= 2)
            {
                Circle(mesh, rect.center + new Vector2(-spacing, spacing), radius, pipColor);
                Circle(mesh, rect.center + new Vector2(spacing, -spacing), radius, pipColor);
            }
            if (Value >= 4)
            {
                Circle(mesh, rect.center + new Vector2(spacing, spacing), radius, pipColor);
                Circle(mesh, rect.center + new Vector2(-spacing, -spacing), radius, pipColor);
            }
            if (Value == 6)
            {
                Circle(mesh, rect.center + new Vector2(-spacing, 0), radius, pipColor);
                Circle(mesh, rect.center + new Vector2(spacing, 0), radius, pipColor);
            }
        }

        static void Circle(VertexHelper mesh, Vector2 center, float radius, Color tint)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(center, tint, Vector2.zero);
            const int segments = 20;
            for (int i = 0; i <= segments; i++)
            {
                var angle = i * Mathf.PI * 2 / segments;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
                if (i > 0) mesh.AddTriangle(first, first + i, first + i + 1);
            }
        }

        static void RoundedRect(VertexHelper mesh, Vector2 center, Vector2 half, float radius, Color tint)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(center, tint, Vector2.zero);
            for (int corner = 0; corner < 4; corner++)
            {
                var pivot = center + new Vector2(corner == 0 || corner == 3 ? half.x - radius : -half.x + radius,
                    corner < 2 ? half.y - radius : -half.y + radius);
                for (int step = 0; step <= 6; step++)
                {
                    float angle = (corner * 90 + step * 15) * Mathf.Deg2Rad;
                    mesh.AddVert(pivot + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
                }
            }
            int count = mesh.currentVertCount - first - 1;
            for (int i = 0; i < count; i++) mesh.AddTriangle(first, first + i + 1, first + (i + 1) % count + 1);
        }
    }
}
